using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace R8.RedisHashMap.Test.Map;

public static class TextExtensions
{
    private static readonly Regex CamelCaseRegex = new("(?:^|_| +)(.)", RegexOptions.Compiled);

    /// <summary>
    ///     Removes all non-alphanumeric characters from a string.
    /// </summary>
    [return: NotNullIfNotNull("str")]
    public static string Unaccent(this string str)
    {
        if (string.IsNullOrEmpty(str))
            return str;

        return string.Concat(str.Normalize(NormalizationForm.FormD).Where(ch => CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark));
    }

    private static ReadOnlySpan<char> Initialize(this Span<char> span, string source)
    {
        for (var i = 0; i < source.Length; i++) span[i] = source[i];

        return span;
    }

    public static string RemoveRlmChar(this string str)
    {
        Span<char> c = stackalloc char[str.Length];
        var lastIndex = -1;
        for (var index = 0; index < str.Length; index++)
        {
            var ch = str[index];
            if (ch == '\u200F')
                continue;

            c[++lastIndex] = ch;
        }

        c = c[..(lastIndex + 1)];
        return new string(c);
    }

    /// <summary>
    ///     Determines whether the given text is right-to-left.
    /// </summary>
    public static bool IsRightToLeft(this string text, double ratio = 0.3)
    {
        if (string.IsNullOrWhiteSpace(text))
            return false;

        if (text.Any(rlmChar => rlmChar == '\u200F'))
            return true;

        // Check if the first character is RTL
        if (IsRtlCharacter(text[0]))
            return true;

        var rtlCharacters = 0;
        var totalCharacters = 0;

        // Use stackalloc to create a ReadOnlySpan<char> from the input text
        var length = text.Length;
        var span = text.Length <= 128 ? stackalloc char[length].Initialize(text) : text;

        foreach (var c in span)
        {
            if (IsRtlCharacter(c)) rtlCharacters++;

            totalCharacters++;
        }

        if (totalCharacters == 0)
            return false;

        var rtlRatio = (double)rtlCharacters / totalCharacters;

        return rtlRatio > ratio;
    }

    private static bool IsRtlCharacter(char c)
    {
        var category = CharUnicodeInfo.GetUnicodeCategory(c);

        return ((c >= 0x0591 && c <= 0x07FF) || // Hebrew and Arabic blocks
                (c >= 0xFB1D && c <= 0xFB4F) || // Hebrew presentation forms
                (c >= 0xFB50 && c <= 0xFD3D) || // Arabic presentation forms-A
                (c >= 0xFD50 && c <= 0xFDFF) || // Arabic presentation forms-B
                (c >= 0xFE70 && c <= 0xFEFC)) && // Arabic presentation forms-C
               (category == UnicodeCategory.UppercaseLetter ||
                category == UnicodeCategory.LowercaseLetter ||
                category == UnicodeCategory.TitlecaseLetter ||
                category == UnicodeCategory.ModifierLetter ||
                category == UnicodeCategory.OtherLetter ||
                category == UnicodeCategory.NonSpacingMark ||
                category == UnicodeCategory.SpacingCombiningMark ||
                category == UnicodeCategory.EnclosingMark ||
                category == UnicodeCategory.DecimalDigitNumber ||
                category == UnicodeCategory.LetterNumber ||
                category == UnicodeCategory.OtherNumber);
    }

    /// <summary>
    ///     Converts " " to "%20".
    /// </summary>
    /// <returns></returns>
    public static string Urlify(this string s)
    {
        return !string.IsNullOrEmpty(s)
            ? s.Replace(" ", "%20")
            : null;
    }

    /// <summary>
    ///     Converts a string to a slug.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    public static string Slugify(this string s)
    {
        if (string.IsNullOrEmpty(s))
            throw new ArgumentNullException(nameof(s));

        // Remove Accent
        var bytes = Encoding.UTF8.GetBytes(s);
        var str = Encoding.ASCII.GetString(bytes);

        str = str.ToLower();

        // invalid chars
        str = Regex.Replace(str, @"[^a-z0-9\s-]", "");

        // convert multiple spaces into one space
        str = Regex.Replace(str, @"\s+", " ").Trim();

        // cut and trim
        str = str[..(str.Length <= 45 ? str.Length : 45)].Trim();
        str = Regex.Replace(str, @"\s", "-"); // hyphens

        return str;
    }

    /// <summary>
    ///     Returns a <see cref="string" /> between to given characters.
    /// </summary>
    /// <param name="str">A <see cref="string" /> value to check in</param>
    /// <param name="start">Starting <see cref="char" /></param>
    /// <param name="end">Ending <see cref="char" /></param>
    /// <exception cref="ArgumentNullException">Thrown when the string is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start or end characters are less than or equal to 0.</exception>
    /// <returns>A <see cref="string" /> between two characters.</returns>
    public static string GetStringBetween(this string str, char start, char end)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (start <= 0)
            throw new ArgumentOutOfRangeException(nameof(start));
        if (end <= 0)
            throw new ArgumentOutOfRangeException(nameof(end));

        return new string(str.SkipWhile(c => c != start)
            .Skip(1)
            .TakeWhile(c => c != end)
            .ToArray()).Trim();
    }

    /// <summary>
    ///     Returns a <see cref="string" /> between to given strings.
    /// </summary>
    /// <param name="str">A <see cref="string" /> value to check in</param>
    /// <param name="start">Starting <see cref="string" /></param>
    /// <param name="end">Ending <see cref="string" /></param>
    /// <exception cref="ArgumentNullException">Thrown when the string is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the start or end strings are null or empty.</exception>
    /// <returns>A <see cref="string" /> between two strings.</returns>
    public static string GetStringBetween(this string str, string start, string end)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (string.IsNullOrEmpty(start))
            throw new ArgumentOutOfRangeException(nameof(start));
        if (string.IsNullOrEmpty(end))
            throw new ArgumentOutOfRangeException(nameof(end));

        var startIndex = str.IndexOf(start, StringComparison.Ordinal);
        if (startIndex == -1)
            return null;

        startIndex += start.Length;
        var endIndex = str.IndexOf(end, startIndex, StringComparison.Ordinal);
        if (endIndex == -1)
            return null;

        return str.Substring(startIndex, endIndex - startIndex);
    }

    /// <summary>
    ///     Returns a Camel Case <see cref="string" /> from a given <see cref="string" />.
    /// </summary>
    /// <returns>A <see cref="string" /> value in camel case.</returns>
    /// <example>SomePropertyData => somePropertyData</example>
    /// <exception cref="ArgumentNullException">Thrown when the string is null or empty.</exception>
    public static string ToCamelCase(this string s)
    {
        if (string.IsNullOrEmpty(s))
            throw new ArgumentNullException(nameof(s));

        var key = CamelCaseRegex.Replace(s, match => match.Groups[1].Value.ToUpper());
        if (key.Length == 0)
            return key;

        var upperCaps = true;
        for (var i = 1; i < key.Length; i++)
        {
            if (!char.IsLetter(key[i]))
                continue;

            if (!char.IsUpper(key[i]))
            {
                upperCaps = false;
                break;
            }
        }

        if (upperCaps)
            return key; // If all letters are upper case, return the original string

        Span<char> span = stackalloc char[key.Length];
        span[0] = char.ToLowerInvariant(key[0]);
        for (var i = 1; i < key.Length; i++)
            span[i] = key[i];

        return new string(span);
    }

    /// <summary>
    ///     Returns specific index of given value in string.
    /// </summary>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    public static int IndexOfNth(this string str, char value, int nth, int? startIndex = null)
    {
        if (str == null)
            throw new ArgumentNullException(nameof(str));
        if (startIndex < 0)
            throw new ArgumentOutOfRangeException(nameof(startIndex), "param cannot be less than 0");
        if (value <= 0)
            throw new ArgumentOutOfRangeException(nameof(value));

        var nSign = Math.Sign(nth);
        var inputLength = str?.Length ?? 0;
        int index;
        int count;

        switch (nSign)
        {
            case 1:
                index = startIndex ?? 0;
                count = inputLength - index;
                break;

            case -1:
                index = startIndex ?? inputLength - 1;
                count = index + 1;
                break;

            default:
                throw new ArgumentOutOfRangeException(message: "param cannot be equal to 0", paramName: nameof(nth));
        }

        while (count-- > 0)
        {
            if (str[index] == value && (nth -= nSign) == 0)
                return index;

            index += nSign;
        }

        return -1;
    }

    /// <summary>
    ///     Truncates a string to a given number of words.
    /// </summary>
    public static string TruncateWords(this string value, int wordLimit)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        var lines = value.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        var truncated = new StringBuilder();
        var wordCount = 0;

        var totalWords = value.Split(' ').Length;
        if (totalWords <= wordLimit)
            return value;

        static int FindValidDot(string input, int startIndex)
        {
            if (startIndex == -1)
                return -1;

            var index = startIndex;
            while (index < input.Length)
            {
                index = input.IndexOf('.', index);
                if (index == -1)
                    break;

                if (index == input.Length - 1 || char.IsWhiteSpace(input[index + 1]) || input[index + 1] == '\n')
                    return index;

                index++;
            }

            return input.Length - 1;
        }

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                truncated.AppendLine();
                continue;
            }

            foreach (var word in line.Split(' '))
            {
                truncated.Append(word);
                wordCount++;

                if (wordCount < wordLimit)
                {
                    truncated.Append(' ');
                }
                else
                {
                    var lastIndex = truncated.Length;
                    var nextDotIndex = -1;
                    var restOfValue = string.Empty;
                    var endOfValue = false;
                    foreach (var p in new[] { '.', '\n' })
                    {
                        var ind = value.IndexOf(p, lastIndex);
                        if (ind != -1)
                        {
                            nextDotIndex = ind;
                            restOfValue = value.Substring(lastIndex, nextDotIndex - lastIndex + 1);
                            break;
                        }
                    }

                    if (nextDotIndex == -1)
                    {
                        nextDotIndex = value.Length - 1;
                        restOfValue = value[lastIndex..nextDotIndex];
                        endOfValue = true;
                    }

                    if (!string.IsNullOrWhiteSpace(restOfValue))
                        truncated.Append(' ').Append(restOfValue.Trim());

                    if (!endOfValue)
                        truncated.Append(truncated[^1] == '.' ? ".." : "...");

                    break;
                }
            }

            if (wordCount >= wordLimit)
                break;

            if (truncated.Length > 0 && truncated[^1] != '\n')
                truncated.AppendLine();
        }

        return truncated.ToString();
    }
}