using System;
using System.Text.RegularExpressions;

namespace R8.RedisHashMap
{
    public static class TextExtensions
    {
        private static readonly Regex CamelCaseRegex = new Regex("(?:^|_| +)(.)", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        /// <summary>
        /// Returns a Camel Case <see cref="string"/> from a given <see cref="string"/>.
        /// </summary>
        /// <returns>A <see cref="string"/> value in camel case.</returns>
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

        public static string ToSnakeCase(this string s)
        {
            if (string.IsNullOrEmpty(s))
                throw new ArgumentNullException(nameof(s));

            var lastIndex = 0;
            Span<char> span = stackalloc char[s.Length * 2];
            for (var i = 0; i < s.Length; i++)
            {
                if (char.IsUpper(s[i]))
                {
                    if (i > 0 && char.IsLower(s[i - 1]))
                    {
                        span[lastIndex++] = '_';
                    }

                    span[lastIndex++] = char.ToLowerInvariant(s[i]);
                }
                else
                {
                    span[lastIndex++] = s[i];
                }
            }
            
            return new string(span.Slice(0, lastIndex));
        }
    }
}