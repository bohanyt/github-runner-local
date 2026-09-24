namespace Grl.Core;

public sealed record WindowsEntryMetadata(
    bool Exists, bool IsDirectory, bool IsReparsePoint = false,
    bool IsRedirected = false, bool IsSynced = false);

// The adapter supplies Windows metadata. The policy never probes the host filesystem.
public interface IFileSystemView
{
    WindowsEntryMetadata? GetMetadata(string canonicalPath);
    IReadOnlyList<string> GetChildren(string canonicalDirectory);
}

public enum WindowsLocationKind
{
    Local, Network, Redirected, Synced, TooLong, ReparsePoint, Invalid
}

public sealed record WindowsPathBudget(
    string Candidate, int CharacterCount, int RemainingCharacters, bool FitsLegacyMaxPath);

public sealed record WindowsDeletionPlan(
    string OwnedRoot, IReadOnlyList<string> PathsInDeletionOrder);

public sealed class WindowsPathPolicy(IFileSystemView view)
{
    public const int LegacyMaxPathWithTerminator = 260;

    public static string NormalizeDriveAbsolute(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ContractException("INVALID_PATH", "A path is required.");

        var normalized = path.Replace('/', '\\');
        if (normalized.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            normalized.StartsWith(@"\\.\", StringComparison.Ordinal) ||
            normalized.StartsWith(@"\??\", StringComparison.Ordinal))
            throw new ContractException("DEVICE_PATH", "Windows device paths are forbidden.");
        if (normalized.StartsWith(@"\\", StringComparison.Ordinal))
            throw new ContractException("UNC_PATH", "UNC paths are not local owned roots.");
        if (normalized.Length < 3 || !IsAsciiLetter(normalized[0]) ||
            normalized[1] != ':' || normalized[2] != '\\')
            throw new ContractException("NOT_DRIVE_ABSOLUTE", "An absolute drive path is required.");

        var drive = char.ToUpperInvariant(normalized[0]);
        var rest = normalized[3..];
        if (rest.Length == 0) return $"{drive}:\\";
        var parts = rest.Split('\\', StringSplitOptions.None);
        if (parts.Length > 0 && parts[^1].Length == 0)
            parts = parts[..^1]; // one terminal separator is harmless
        if (parts.Length == 0) return $"{drive}:\\";
        foreach (var part in parts) ValidateSegment(part);
        return $"{drive}:\\{string.Join('\\', parts)}";
    }

    public static bool IsWithinOwnedRoot(string ownedRoot, string candidate)
    {
        try
        {
            var root = NormalizeDriveAbsolute(ownedRoot).TrimEnd('\\');
            var child = NormalizeDriveAbsolute(candidate).TrimEnd('\\');
            return child.Equals(root, StringComparison.OrdinalIgnoreCase) ||
                child.StartsWith(root + "\\", StringComparison.OrdinalIgnoreCase);
        }
        catch (ContractException)
        {
            return false;
        }
    }

    public WindowsLocationKind ClassifyLocation(string path, string worstCaseRelativePath = "r\\w\\repo\\repo")
    {
        if (path is null) return WindowsLocationKind.Invalid;
        var slash = path.Replace('/', '\\');
        if (slash.StartsWith(@"\\?\", StringComparison.Ordinal) ||
            slash.StartsWith(@"\\.\", StringComparison.Ordinal) ||
            slash.StartsWith(@"\??\", StringComparison.Ordinal))
            return WindowsLocationKind.Invalid;
        if (slash.StartsWith(@"\\", StringComparison.Ordinal))
            return WindowsLocationKind.Network;

        string canonical;
        try { canonical = NormalizeDriveAbsolute(path); }
        catch (ContractException) { return WindowsLocationKind.Invalid; }
        if (canonical.Length == 3) return WindowsLocationKind.Invalid; // never own a drive root

        foreach (var ancestor in Ancestors(canonical))
        {
            var metadata = view.GetMetadata(ancestor);
            if (metadata is null || !metadata.Exists) return WindowsLocationKind.Invalid;
            if (metadata.IsReparsePoint) return WindowsLocationKind.ReparsePoint;
            if (!metadata.IsDirectory) return WindowsLocationKind.Invalid;
            if (metadata.IsRedirected) return WindowsLocationKind.Redirected;
            if (metadata.IsSynced) return WindowsLocationKind.Synced;
        }
        if (canonical.Split('\\').Any(segment =>
            segment.StartsWith("OneDrive", StringComparison.OrdinalIgnoreCase)))
            return WindowsLocationKind.Synced;
        return CalculateBudget(canonical, worstCaseRelativePath).FitsLegacyMaxPath
            ? WindowsLocationKind.Local : WindowsLocationKind.TooLong;
    }

    public static WindowsPathBudget CalculateBudget(string ownedRoot, string relativePath)
    {
        var root = NormalizeDriveAbsolute(ownedRoot);
        if (root.Length == 3)
            throw new ContractException("UNSAFE_ROOT", "An owned root cannot be a drive root.");
        if (string.IsNullOrEmpty(relativePath))
            throw new ContractException("INVALID_PATH", "A relative path is required.");
        var suffix = relativePath.Replace('/', '\\');
        if (suffix.StartsWith('\\') || suffix.Contains(':'))
            throw new ContractException("INVALID_PATH", "A relative path cannot be rooted or contain a colon.");
        var candidate = NormalizeDriveAbsolute(root + "\\" + suffix);
        var remaining = LegacyMaxPathWithTerminator - 1 - candidate.Length;
        return new(candidate, candidate.Length, remaining, remaining >= 0);
    }

    public WindowsDeletionPlan PlanDeletion(string ownedRoot, string target)
    {
        var root = NormalizeDriveAbsolute(ownedRoot);
        var canonicalTarget = NormalizeDriveAbsolute(target);
        if (root.Length == 3 || !IsWithinOwnedRoot(root, canonicalTarget))
            throw new ContractException("OUTSIDE_OWNED_ROOT", "Deletion target is outside the owned root.");

        // Inspect every ancestor, including the drive root, before considering entries.
        foreach (var ancestor in Ancestors(canonicalTarget))
        {
            var metadata = RequireOrdinaryEntry(ancestor);
            if (!ancestor.Equals(canonicalTarget, StringComparison.OrdinalIgnoreCase) &&
                !metadata.IsDirectory)
                throw new ContractException("NOT_DIRECTORY", "A deletion ancestor must be a directory.");
        }

        var planned = new List<string>();
        var visiting = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Visit(canonicalTarget);
        return new(root, planned.AsReadOnly());

        void Visit(string path)
        {
            if (!visiting.Add(path))
                throw new ContractException("CYCLE", "The filesystem view contains a cycle.");
            var metadata = RequireOrdinaryEntry(path);
            if (metadata.IsDirectory)
            {
                foreach (var child in view.GetChildren(path))
                {
                    var canonicalChild = NormalizeDriveAbsolute(child);
                    var prefix = path.TrimEnd('\\') + "\\";
                    if (!IsWithinOwnedRoot(root, canonicalChild) ||
                        !canonicalChild.StartsWith(prefix, StringComparison.OrdinalIgnoreCase) ||
                        canonicalChild[prefix.Length..].Contains('\\'))
                        throw new ContractException("OUTSIDE_OWNED_ROOT", "An entry escaped its parent or owned root.");
                    Visit(canonicalChild);
                }
            }
            planned.Add(path);
            visiting.Remove(path);
        }
    }

    private WindowsEntryMetadata RequireOrdinaryEntry(string path)
    {
        var metadata = view.GetMetadata(path);
        if (metadata is null || !metadata.Exists)
            throw new ContractException("MISSING_METADATA", "Filesystem metadata is required for deletion planning.");
        if (metadata.IsReparsePoint)
            throw new ContractException("REPARSE_POINT", "Reparse points cannot be traversed or deleted.");
        return metadata;
    }

    private static IEnumerable<string> Ancestors(string canonical)
    {
        yield return canonical[..3];
        if (canonical.Length == 3) yield break;
        var current = canonical[..3];
        foreach (var segment in canonical[3..].Split('\\'))
        {
            current = current.TrimEnd('\\') + "\\" + segment;
            yield return current;
        }
    }

    private static void ValidateSegment(string part)
    {
        if (part.Length == 0 || part is "." or "..")
            throw new ContractException("TRAVERSAL", "Empty, dot or parent segments are forbidden.");
        if (part[^1] is '.' or ' ')
            throw new ContractException("TRAILING_DOT_SPACE", "Trailing dots or spaces are forbidden.");
        if (part.Any(c => c < 32 || c is ':' or '<' or '>' or '"' or '|' or '?' or '*'))
            throw new ContractException("INVALID_PATH", "A segment contains a forbidden Windows character.");
        var basename = part.Split('.')[0];
        if (basename.EndsWith(' '))
            throw new ContractException("TRAILING_DOT_SPACE", "Spaces before an extension are ambiguous.");
        if (basename.Equals("CON", StringComparison.OrdinalIgnoreCase) ||
            basename.Equals("PRN", StringComparison.OrdinalIgnoreCase) ||
            basename.Equals("AUX", StringComparison.OrdinalIgnoreCase) ||
            basename.Equals("NUL", StringComparison.OrdinalIgnoreCase) ||
            basename.Equals("CONIN$", StringComparison.OrdinalIgnoreCase) ||
            basename.Equals("CONOUT$", StringComparison.OrdinalIgnoreCase) ||
            (basename.Length == 4 &&
             (basename.StartsWith("COM", StringComparison.OrdinalIgnoreCase) ||
              basename.StartsWith("LPT", StringComparison.OrdinalIgnoreCase)) &&
             (basename[3] is >= '1' and <= '9' or '¹' or '²' or '³')))
            throw new ContractException("RESERVED_NAME", "A Windows device name is forbidden.");
    }

    private static bool IsAsciiLetter(char c) => c is >= 'A' and <= 'Z' or >= 'a' and <= 'z';
}
