using System;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Provides methods for validating RFC 9651 values.
    /// </summary>
    public static class StructuredFieldSyntax
    {
        /// <summary>
        /// Determines whether the <paramref name="value"/> is a valid RFC 9651 <c>key</c>.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is a valid key; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidKey(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty || !char.IsAsciiLetterLower(value[0]) && value[0] != '*')
            {
                return false;
            }
            for (int index = 1; index < value.Length; index++)
            {
                char c = value[index];
                if (!char.IsAsciiLetterLower(c) && !char.IsAsciiDigit(c) && c != '_' && c != '-' && c != '.' && c != '*')
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Determines whether the <paramref name="value"/> is a valid RFC 9651 <c>sf-token</c>.
        /// </summary>
        /// <param name="value">The value to validate.</param>
        /// <returns><see langword="true"/> if <paramref name="value"/> is a valid sf-token; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidToken(ReadOnlySpan<char> value)
        {
            if (value.IsEmpty || !char.IsAsciiLetter(value[0]) && value[0] != '*')
            {
                return false;
            }
            for (int index = 1; index < value.Length; index++)
            {
                if (!IsTokenChar(value[index]))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// Determines whether <paramref name="c"/> is allowed in an RFC 9651 <c>sf-token</c> after the first character (<c>tchar</c>, ":" or "/").
        /// </summary>
        /// <param name="c">The character to test.</param>
        /// <returns><see langword="true"/> if <paramref name="c"/> is allowed; otherwise, <see langword="false"/>.</returns>
        public static bool IsTokenChar(char c) => c switch
        {
            '!' or '#' or '$' or '%' or '&' or '\'' or '*' or '+' or '-' or '.' or '^' or '_' or '`' or '|' or '~' or ':' or '/' => true,
            _ => char.IsAsciiLetterOrDigit(c)
        };
    }
}