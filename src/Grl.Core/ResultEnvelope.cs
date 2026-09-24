using System.Text;
using System.Text.RegularExpressions;

namespace Grl.Core;

public static partial class ResultEnvelope
{
    public const string Marker = "<!-- grl-result v1 -->";
    public const int MaximumBodyBytes = 16 * 1024;

    [GeneratedRegex(@"\A<!-- grl-result v1 -->\r?\n" +
        @"\x60{3}json\r?\n(?<json>.*?)\r?\n\x60{3}" +
        @"(?:\r?\n[^\r\n]{1,200})?[ \t\r\n]*\z", RegexOptions.Singleline)]
    private static partial Regex EnvelopePattern();

    public static ResultContract Parse(
        ReadOnlySpan<byte> bodyBytes, RequestContract request,
        ProfilePolicy profilePolicy, string expectedExecutionRepository,
        long expectedRequestCommentId)
    {
        if (bodyBytes.Length > MaximumBodyBytes)
            throw new ContractException("BODY_TOO_LARGE", "A result exceeds 16 KiB.");
        if (bodyBytes.Length >= 3 && bodyBytes[0] == 0xEF &&
            bodyBytes[1] == 0xBB && bodyBytes[2] == 0xBF)
            throw new ContractException("BOM", "A UTF-8 BOM is forbidden.");
        string body;
        try { body = new UTF8Encoding(false, true).GetString(bodyBytes); }
        catch (DecoderFallbackException)
        {
            throw new ContractException("INVALID_UTF8", "Result bytes must be valid UTF-8.");
        }
        if (body.Split(new string((char)96, 3), StringSplitOptions.None).Length != 3)
            throw new ContractException("INVALID_ENVELOPE", "Exactly one JSON fence is required.");
        var envelope = EnvelopePattern().Match(body);
        if (!envelope.Success)
            throw new ContractException("INVALID_ENVELOPE", "One exact result marker and JSON fence are required.");
        return ResultValidator.Parse(envelope.Groups["json"].Value, request,
            profilePolicy, expectedExecutionRepository, expectedRequestCommentId);
    }
}
