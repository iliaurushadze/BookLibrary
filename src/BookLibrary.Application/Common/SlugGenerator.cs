using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BookLibrary.Application.Common;

public static partial class SlugGenerator
{
    public static string Generate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return string.Empty;

        var normalized = input.Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder();

        foreach (var c in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category != UnicodeCategory.NonSpacingMark)
            {
                sb.Append(c);
            }
        }

        var cleaned = sb.ToString().Normalize(NormalizationForm.FormC).ToLowerInvariant();
        cleaned = InvalidCharsRegex().Replace(cleaned, "");
        cleaned = WhitespaceRegex().Replace(cleaned, "-");
        cleaned = DuplicateHyphenRegex().Replace(cleaned, "-");

        return cleaned.Trim('-');
    }

    [GeneratedRegex(@"[^a-z0-9\s-]")]
    private static partial Regex InvalidCharsRegex();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"-+")]
    private static partial Regex DuplicateHyphenRegex();
}
