using Grl.Core;

namespace Grl.Core.Tests;

public sealed class FakeFileSystemView : IFileSystemView
{
    private readonly Dictionary<string, WindowsEntryMetadata> _metadata =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _children =
        new(StringComparer.OrdinalIgnoreCase);

    public void Add(string path, bool directory = true, bool reparse = false,
        bool redirected = false, bool synced = false)
    {
        var canonical = WindowsPathPolicy.NormalizeDriveAbsolute(path);
        _metadata[canonical] = new(true, directory, reparse, redirected, synced);
    }

    public void AddAncestors(string path)
    {
        var canonical = WindowsPathPolicy.NormalizeDriveAbsolute(path);
        Add(canonical[..3]);
        var current = canonical[..3];
        foreach (var segment in canonical[3..].Split('\\'))
        {
            current = current.TrimEnd('\\') + "\\" + segment;
            Add(current);
        }
    }

    public void SetChildren(string parent, params string[] children) =>
        _children[WindowsPathPolicy.NormalizeDriveAbsolute(parent)] = children.ToList();

    public WindowsEntryMetadata? GetMetadata(string canonicalPath) =>
        _metadata.GetValueOrDefault(canonicalPath);

    public IReadOnlyList<string> GetChildren(string canonicalDirectory) =>
        _children.GetValueOrDefault(canonicalDirectory) ?? [];
}

public sealed class WindowsPathPolicyTests
{
    [Theory]
    [InlineData(@"c:\Users\owner\Documents\grl", @"C:\Users\owner\Documents\grl")]
    [InlineData(@"c:/Users\owner/Documents/grl", @"C:\Users\owner\Documents\grl")]
    [InlineData(@"C:\", @"C:\")]
    public void Drive_paths_normalize_without_host_Path(string input, string expected)
    {
        Assert.Equal(expected, WindowsPathPolicy.NormalizeDriveAbsolute(input));
    }

    [Theory]
    [InlineData(@"C:\owned\..\escape", "TRAVERSAL")]
    [InlineData(@"C:\owned\.\entry", "TRAVERSAL")]
    [InlineData(@"C:\owned\\entry", "TRAVERSAL")]
    [InlineData(@"C:\owned\file.txt:stream", "INVALID_PATH")]
    [InlineData(@"\\?\C:\owned", "DEVICE_PATH")]
    [InlineData(@"\\.\C:\owned", "DEVICE_PATH")]
    [InlineData(@"\??\C:\owned", "DEVICE_PATH")]
    [InlineData(@"\\server\share\owned", "UNC_PATH")]
    [InlineData(@"C:relative", "NOT_DRIVE_ABSOLUTE")]
    [InlineData(@"C:\owned\CON.txt", "RESERVED_NAME")]
    [InlineData(@"C:\owned\lpt9", "RESERVED_NAME")]
    [InlineData(@"C:\owned\NUL", "RESERVED_NAME")]
    [InlineData(@"C:\owned\COM¹.txt", "RESERVED_NAME")]
    [InlineData(@"C:\owned\CONIN$", "RESERVED_NAME")]
    [InlineData(@"C:\owned\CON .txt", "TRAILING_DOT_SPACE")]
    [InlineData(@"C:\owned\entry.", "TRAILING_DOT_SPACE")]
    [InlineData(@"C:\owned\entry ", "TRAILING_DOT_SPACE")]
    public void Dangerous_Windows_path_forms_are_refused(string path, string code)
    {
        Assert.Equal(code,
            Assert.Throws<ContractException>(() =>
                WindowsPathPolicy.NormalizeDriveAbsolute(path)).Code);
    }

    [Fact]
    public void Root_containment_uses_a_component_boundary_and_case_insensitive_drive()
    {
        Assert.True(WindowsPathPolicy.IsWithinOwnedRoot(
            @"C:\Owned", @"c:/owned/sub/file"));
        Assert.False(WindowsPathPolicy.IsWithinOwnedRoot(
            @"C:\Owned", @"C:\OwnedEvil\file"));
        Assert.False(WindowsPathPolicy.IsWithinOwnedRoot(
            @"C:\Owned", @"D:\Owned\file"));
        Assert.False(WindowsPathPolicy.IsWithinOwnedRoot(
            @"C:\Owned", @"C:\Owned\..\outside"));
    }

    [Fact]
    public void Redirected_and_synced_metadata_are_classified_from_fake_view()
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\Users\owner\Documents\grl");
        view.Add(@"C:\Users\owner\Documents", redirected: true);
        var policy = new WindowsPathPolicy(view);
        Assert.Equal(WindowsLocationKind.Redirected,
            policy.ClassifyLocation(@"C:\Users\owner\Documents\grl"));

        view.Add(@"C:\Users\owner\Documents");
        view.Add(@"C:\Users\owner\Documents\grl", synced: true);
        Assert.Equal(WindowsLocationKind.Synced,
            policy.ClassifyLocation(@"C:\Users\owner\Documents\grl"));
        Assert.Equal(WindowsLocationKind.Network,
            policy.ClassifyLocation(@"\\server\share\grl"));
    }

    [Fact]
    public void OneDrive_like_path_is_not_silently_classified_local()
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\Users\owner\OneDrive - Org\grl");
        Assert.Equal(WindowsLocationKind.Synced,
            new WindowsPathPolicy(view).ClassifyLocation(
                @"C:\Users\owner\OneDrive - Org\grl"));
    }

    [Fact]
    public void Legacy_path_budget_has_exact_boundary()
    {
        var root = @"C:\r";
        var fits = WindowsPathPolicy.CalculateBudget(root, new string('a', 254));
        var tooLong = WindowsPathPolicy.CalculateBudget(root, new string('a', 255));
        Assert.Equal(259, fits.CharacterCount);
        Assert.Equal(0, fits.RemainingCharacters);
        Assert.True(fits.FitsLegacyMaxPath);
        Assert.Equal(260, tooLong.CharacterCount);
        Assert.False(tooLong.FitsLegacyMaxPath);
    }

    [Fact]
    public void Location_classification_uses_budget_and_requires_directory_metadata()
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\owned");
        var policy = new WindowsPathPolicy(view);
        Assert.Equal(WindowsLocationKind.TooLong,
            policy.ClassifyLocation(@"C:\owned", new string('a', 255)));
        view.Add(@"C:\owned", directory: false);
        Assert.Equal(WindowsLocationKind.Invalid,
            policy.ClassifyLocation(@"C:\owned"));
    }

    [Fact]
    public void Reparse_ancestor_and_entry_are_both_refused()
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\owned\sub\file");
        view.Add(@"C:\owned", reparse: true);
        var policy = new WindowsPathPolicy(view);
        Assert.Equal(WindowsLocationKind.ReparsePoint,
            policy.ClassifyLocation(@"C:\owned\sub"));
        Assert.Equal("REPARSE_POINT",
            Assert.Throws<ContractException>(() =>
                policy.PlanDeletion(@"C:\owned", @"C:\owned\sub")).Code);

        view.Add(@"C:\owned");
        view.Add(@"C:\owned\sub\file", directory: false, reparse: true);
        view.SetChildren(@"C:\owned\sub", @"C:\owned\sub\file");
        Assert.Equal("REPARSE_POINT",
            Assert.Throws<ContractException>(() =>
                policy.PlanDeletion(@"C:\owned", @"C:\owned\sub")).Code);
    }

    [Fact]
    public void Deletion_planner_returns_only_postorder_paths()
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\owned\sub\file");
        view.Add(@"C:\owned\sub\file", directory: false);
        view.SetChildren(@"C:\owned\sub", @"C:\owned\sub\file");
        var plan = new WindowsPathPolicy(view).PlanDeletion(
            @"C:\owned", @"C:\owned\sub");
        Assert.Equal(
            [@"C:\owned\sub\file", @"C:\owned\sub"],
            plan.PathsInDeletionOrder);
    }

    [Theory]
    [InlineData(@"C:\outside")]
    [InlineData(@"C:\owned-evil")]
    [InlineData(@"C:\owned\..\outside")]
    public void Deletion_target_cannot_escape_owned_root(string target)
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\owned");
        Assert.Throws<ContractException>(() =>
            new WindowsPathPolicy(view).PlanDeletion(@"C:\owned", target));
    }

    [Fact]
    public void Deletion_view_cannot_inject_escape_or_unknown_metadata()
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\owned\sub");
        view.SetChildren(@"C:\owned\sub", @"C:\other\file");
        Assert.Equal("OUTSIDE_OWNED_ROOT",
            Assert.Throws<ContractException>(() =>
                new WindowsPathPolicy(view).PlanDeletion(@"C:\owned", @"C:\owned\sub")).Code);
        view.SetChildren(@"C:\owned\sub", @"C:\owned\sub\unknown");
        Assert.Equal("MISSING_METADATA",
            Assert.Throws<ContractException>(() =>
                new WindowsPathPolicy(view).PlanDeletion(@"C:\owned", @"C:\owned\sub")).Code);
    }

    [Fact]
    public void Deletion_view_cannot_skip_a_reparse_ancestor()
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\owned\sub\link\file");
        view.Add(@"C:\owned\sub\link", reparse: true);
        view.Add(@"C:\owned\sub\link\file", directory: false);
        view.SetChildren(@"C:\owned\sub", @"C:\owned\sub\link\file");
        Assert.Equal("OUTSIDE_OWNED_ROOT",
            Assert.Throws<ContractException>(() =>
                new WindowsPathPolicy(view).PlanDeletion(@"C:\owned", @"C:\owned\sub")).Code);
    }

    [Fact]
    public void Drive_root_is_never_an_owned_deletion_root()
    {
        var view = new FakeFileSystemView();
        view.AddAncestors(@"C:\owned");
        Assert.Equal("OUTSIDE_OWNED_ROOT",
            Assert.Throws<ContractException>(() =>
                new WindowsPathPolicy(view).PlanDeletion(@"C:\", @"C:\owned")).Code);
    }
}
