using System;
using System.Buffers;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Parses RFC 9651 Structured Field Values (Lists, Dictionaries, and Items) from their ASCII string representation,
    /// following the parsing algorithms of <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2">RFC 9651 section 4.2</see>.
    /// </summary>
    public static class StructuredFieldParser
    {
        /// <summary>
        /// Provides a UTF-8 encoding instance that does not emit a byte order mark (BOM) and throws on invalid byte sequences.
        /// </summary>
        [DebuggerBrowsable(DebuggerBrowsableState.Never)]
        private static readonly UTF8Encoding StrictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

        /// <summary>
        /// Parses a string as an RFC 9651 top-level <see cref="SfDictionary"/>.
        /// </summary>
        /// <param name="value">The string representation of a structured field dictionary.</param>
        /// <returns>An <see cref="SfDictionary"/> instance parsed from the input.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">The <paramref name="value"/> is not a valid string representation of a structured field dictionary or contains unexpected trailing data after optional whitespace.</exception>
        public static SfDictionary ParseDictionary(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            StringCursor cursor = new(value);
            SkipSpace(ref cursor);
            SfDictionary result = ParseDictionaryImpl(ref cursor);
            return result;

            static SfDictionary ParseDictionaryImpl(ref StringCursor cursor)
            {
                List<KeyValuePair<string, SfMember>> members = [];
                while (!cursor.AtEnd)
                {
                    SfMember member;
                    string key = ParseKey(ref cursor);
                    if (!cursor.AtEnd && cursor.Current == '=')
                    {
                        cursor.Advance();
                        member = ParseItemOrInnerList(ref cursor);
                    }
                    else
                    {
                        SfParameters parameters = ParseParameters(ref cursor);
                        member = new SfItem(new SfBareBoolean(true), parameters);
                    }
                    members.Add(new KeyValuePair<string, SfMember>(key, member));
                    SkipSpaceAndHTab(ref cursor);
                    if (cursor.AtEnd)
                        break;
                    if (cursor.Current != ',')
                        throw new FormatException($"Expected ',' at position {cursor.Position}");
                    cursor.Advance();
                    SkipSpaceAndHTab(ref cursor);
                    if (cursor.AtEnd)
                        throw new FormatException("Trailing comma with no following member");
                }
                return new SfDictionary(members);
            }
        }
        /// <summary>
        /// Parses a string as an RFC 9651 top-level <see cref="SfItem"/>.
        /// </summary>
        /// <param name="value">The string representation of a structured field item.</param>
        /// <returns>An <see cref="SfItem"/> instance parsed from the input.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">The <paramref name="value"/> is not a valid string representation of a structured field dictionary or contains unexpected trailing data after optional whitespace.</exception>
        public static SfItem ParseItem(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            StringCursor cursor = new(value);
            SkipSpace(ref cursor);
            SfItem result = ParseItemInner(ref cursor);
            SkipSpace(ref cursor);
            if (!cursor.AtEnd)
                throw new FormatException($"Unexpected trailing data at position {cursor.Position}");
            return result;
        }
        /// <summary>
        /// Parses a string as an RFC 9651 top-level <see cref="SfList"/>.
        /// </summary>
        /// <param name="value">The string representation of a structured field list.</param>
        /// <returns>An <see cref="SfList"/> instance parsed from the input.</returns>
        /// <exception cref="ArgumentNullException">The <paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="FormatException">The <paramref name="value"/> is not a valid string representation of a structured field dictionary or contains unexpected trailing data after optional whitespace.</exception>
        public static SfList ParseList(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            StringCursor cursor = new(value);
            SkipSpace(ref cursor);
            SfList result = ParseListImpl(ref cursor);
            return result;

            static SfList ParseListImpl(ref StringCursor cursor)
            {
                List<SfMember> members = [];
                while (!cursor.AtEnd)
                {
                    members.Add(ParseItemOrInnerList(ref cursor));
                    SkipSpaceAndHTab(ref cursor);
                    if (cursor.AtEnd)
                        break;
                    if (cursor.Current != ',')
                        throw new FormatException($"Expected ',' at position {cursor.Position}");
                    cursor.Advance();
                    SkipSpaceAndHTab(ref cursor);
                    if (cursor.AtEnd)
                        throw new FormatException("Trailing comma with no following member");
                }
                return new SfList(members);
            }
        }

        /// <summary>
        /// Parses a single <see cref="SfBareItem"/> from the input cursor according to RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.3.1">4.2.3.1</see>.
        /// </summary>
        /// <param name="cursor">The cursor positioned at the start of a bare item; advanced past the parsed item.</param>
        /// <returns>An <see cref="SfBareItem"/> representing the parsed value.</returns>
        /// <exception cref="FormatException">The input ends unexpectedly before a bare item is complete or if an unexpected character is encountered at the cursor position.</exception>
        private static SfBareItem ParseBareItem(ref StringCursor cursor)
        {
            if (cursor.AtEnd)
                throw new FormatException("Unexpected end of input while parsing a bare item");

            char c = cursor.Current;
            if (c == '-' || char.IsAsciiDigit(c))
                return ParseNumber(ref cursor);
            if (c == '"')
                return ParseString(ref cursor);
            if (char.IsAsciiLetter(c) || c == '*')
                return ParseToken(ref cursor);
            if (c == ':')
                return ParseByteSequence(ref cursor);
            if (c == '?')
                return ParseBoolean(ref cursor);
            if (c == '@')
                return ParseDate(ref cursor);
            if (c == '%')
                return ParseDisplayString(ref cursor);

            throw new FormatException($"Unexpected character '{c}' at position {cursor.Position} while parsing a bare item");
        }
        /// <summary>
        /// Parses a structured field boolean token from the current cursor position,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.8">4.2.8</see>.
        /// </summary>
        /// <remarks>Consumes exactly two characters: '?' followed by '0' or '1'.</remarks>
        /// <param name="cursor">Reference to the cursor positioned at the leading '?' character; advanced past the parsed token.</param>
        /// <returns>An <see cref="SfBareBoolean"/> containing the parsed boolean value.</returns>
        /// <exception cref="FormatException">The current input is not a valid boolean token: the expected '?' is missing, the input ends after '?', or the following character is not '0' or '1'.</exception>
        private static SfBareBoolean ParseBoolean(ref StringCursor cursor)
        {
            cursor.Expect('?');
            if (cursor.AtEnd || (cursor.Current != '0' && cursor.Current != '1'))
                throw new FormatException($"Expected '0' or '1' after '?' at position {cursor.Position}");
            bool value = cursor.Current == '1';
            cursor.Advance();
            return new SfBareBoolean(value);
        }
        /// <summary>
        /// Parses an sf-binary token delimited by colons and decodes its Base64 payload into a byte-sequence item,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.7">4.2.7</see>.
        /// </summary>
        /// <param name="cursor">Cursor positioned at the opening ':' of the sf-binary token; advances past the closing ':'.</param>
        /// <returns>An <see cref="SfBareByteSequence"/> created from the decoded binary content.</returns>
        /// <exception cref="FormatException">The binary token is unterminated (missing a closing ':') or the enclosed content is not valid Base64.</exception>
        private static SfBareByteSequence ParseByteSequence(ref StringCursor cursor)
        {
            cursor.Expect(':');
            int start = cursor.Position;
            while (!cursor.AtEnd && cursor.Current != ':')
            {
                cursor.Advance();
            }
            if (cursor.AtEnd)
            {
                throw new FormatException("Unterminated sf-binary (missing closing ':')");
            }
            string base64 = cursor.Slice(start, cursor.Position - start).ToString();
            cursor.Advance();
            foreach (char c in base64)
            {
                if (!(char.IsAsciiLetterOrDigit(c) || c == '+' || c == '/' || c == '='))
                {
                    throw new FormatException("sf-binary content contains a character outside the base64 alphabet");
                }
            }
            int padding = base64.Length % 4;
            if (padding == 1)
            {
                throw new FormatException("sf-binary content is not valid base64");
            }
            if (padding != 0)
            {
                base64 = base64.PadRight(base64.Length + 4 - padding, '=');
            }
            try
            {
                return new SfBareByteSequence(Convert.FromBase64String(base64));
            }
            catch (FormatException ex)
            {
                throw new FormatException("sf-binary content is not valid base64", ex);
            }
        }
        /// <summary>
        /// Parses a date item from the current cursor position,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.9">4.2.9</see>.
        /// </summary>
        /// <param name="cursor">A reference to the cursor positioned at the beginning of the date token; advanced past the parsed value.</param>
        /// <returns>An <see cref="SfBareDate"/> created from the parsed integer date value.</returns>
        /// <exception cref="FormatException">The value is not a valid date token in the form '@' followed by an integer.</exception>
        private static SfBareDate ParseDate(ref StringCursor cursor)
        {
            cursor.Expect('@');
            if (ParseNumber(ref cursor) is not SfBareInteger number)
                throw new FormatException("sf-date requires an integer value after '@'");
            return new SfBareDate(number.Value);
        }
        /// <summary>
        /// Parses an sf-displaystring token from the current cursor position and decodes its UTF-8 content,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.10">4.2.10</see>.
        /// </summary>
        /// <remarks>Expects the sequence <c>%"</c> at the start and stops after the terminating
        /// <c>"</c>.</remarks>
        /// <param name="cursor">Cursor positioned at the start of an sf-displaystring; advanced as characters are consumed.</param>
        /// <returns>An <see cref="SfBareDisplayString"/> created from the decoded display string value.</returns>
        /// <exception cref="FormatException">The input is not a valid sf-displaystring, including missing delimiters, incomplete or invalid percent-encoding, or disallowed non-printable/non-ASCII characters.</exception>
        private static SfBareDisplayString ParseDisplayString(ref StringCursor cursor)
        {
            cursor.Expect('%');
            cursor.Expect('"');
            int maxLength = 0;
            StringCursor probe = cursor;
            while (!probe.AtEnd && probe.Current != '"')
            {
                probe.Advance();
                maxLength++;
            }
            byte[] buffer = ArrayPool<byte>.Shared.Rent(maxLength);
            try
            {
                int byteCount = 0;
                while (true)
                {
                    if (cursor.AtEnd)
                    {
                        throw new FormatException("Unterminated sf-displaystring");
                    }
                    char c = cursor.Current;
                    cursor.Advance();
                    if (c == '"')
                    {
                        break;
                    }
                    if (c == '%')
                    {
                        if (cursor.RemainingLength < 2)
                            throw new FormatException("Unterminated percent-encoding in sf-displaystring");
                        int hi = HexValue(cursor.Current);
                        cursor.Advance();
                        int lo = HexValue(cursor.Current);
                        cursor.Advance();
                        if (hi < 0 || lo < 0)
                            throw new FormatException("Invalid percent-encoding in sf-displaystring");
                        buffer[byteCount++] = (byte)((hi << 4) | lo);
                    }
                    else if (c < 0x20 || c > 0x7E)
                    {
                        throw new FormatException("sf-displaystring may only contain printable ASCII characters and percent-encoded bytes");
                    }
                    else
                    {
                        buffer[byteCount++] = (byte)c;
                    }
                }
                try
                {
                    return new SfBareDisplayString(StrictUtf8.GetString(buffer, 0, byteCount));
                }
                catch (DecoderFallbackException ex)
                {
                    throw new FormatException("sf-displaystring contains an invalid UTF-8 byte sequence", ex);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }

            // Helper method to convert a hexadecimal character to its integer value, returning -1 for invalid characters
            static int HexValue(char c) => c switch
            {
                >= '0' and <= '9' => c - '0',
                >= 'a' and <= 'f' => c - 'a' + 10,
                _ => -1,
            };
        }
        /// <summary>
        /// Parses an inner list enclosed in parentheses, including its items and trailing parameters,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.1.2">4.2.1.2</see>.
        /// </summary>
        /// <param name="cursor">A cursor over the input to read from. The current position must be at the opening parenthesis of an inner list and is advanced past the parsed inner list and parameters.</param>
        /// <returns>An <see cref="SfInnerList"/> containing the parsed items and parameters.</returns>
        /// <exception cref="FormatException">A closing parenthesis is not followed by a valid separator, or item separators are not a space or a closing parenthesis.</exception>
        private static SfInnerList ParseInnerList(ref StringCursor cursor)
        {
            cursor.Expect('(');
            List<SfItem> items = [];
            while (true)
            {
                SkipSpace(ref cursor);
                if (cursor.AtEnd)
                {
                    throw new FormatException("Unterminated inner list (missing closing ')').");
                }
                if (cursor.Current == ')')
                {
                    cursor.Advance();
                    SfParameters parameters = ParseParameters(ref cursor);
                    return new SfInnerList(items, parameters);
                }
                SfItem item = ParseItemInner(ref cursor);
                items.Add(item);
                if (!cursor.AtEnd && cursor.Current != ' ' && cursor.Current != ')')
                    throw new FormatException($"Expected ' ' or ')' at position {cursor.Position}");
            }
        }
        /// <summary>
        /// Parses an <see cref="SfItem"/> from the input <paramref name="cursor"/> by reading a bare item followed by its parameters,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.3">4.2.3</see>.
        /// </summary>
        /// <param name="cursor">A reference to the input cursor; advanced to the position after the parsed bare item and its parameters.</param>
        /// <returns>An <see cref="SfItem"/> that combines the parsed bare item and its parameters.</returns>
        /// <exception cref="FormatException">The input ends unexpectedly before a bare item is complete or if an unexpected character is encountered at the cursor position.</exception>
        private static SfItem ParseItemInner(ref StringCursor cursor)
        {
            SfBareItem value = ParseBareItem(ref cursor); // FormatException
            SfParameters parameters = ParseParameters(ref cursor);
            return new SfItem(value, parameters);
        }
        /// <summary>
        /// Parses either an inner list or a single item from the current cursor position based on whether the current character is '(',
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.1.1">4.2.1.1</see>.
        /// </summary>
        /// <param name="cursor">The string cursor positioned at the next member to parse; advanced past the parsed content.</param>
        /// <returns>An <see cref="SfMember"/> representing the parsed <see cref="SfInnerList"/> or <see cref="SfItem"/>.</returns>
        private static SfMember ParseItemOrInnerList(ref StringCursor cursor)
        {
            if (!cursor.AtEnd && cursor.Current == '(')
                return ParseInnerList(ref cursor);
            return ParseItemInner(ref cursor);
        }
        /// <summary>
        /// Parses a key token from the current cursor position,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.3.3">4.2.3.3</see>.
        /// </summary>
        /// <remarks>A key starts with a lowercase ASCII letter or '*', followed by zero or more lowercase ASCII letters, ASCII digits, '_', '-', '.', or '*'.</remarks>
        /// <param name="cursor">The cursor to read from; advanced past the parsed key.</param>
        /// <returns>The parsed key string.</returns>
        /// <exception cref="FormatException">Thrown when the current position is at the end of input or the first character is neither a lowercase ASCII letter nor '*'.</exception>
        private static string ParseKey(ref StringCursor cursor)
        {
            int start = cursor.Position;
            if (cursor.AtEnd || (!char.IsAsciiLetterLower(cursor.Current) && cursor.Current != '*'))
                throw new FormatException($"Expected a lowercase ASCII letter or '*' at position {cursor.Position}");
            cursor.Advance();
            while (!cursor.AtEnd && (char.IsAsciiLetterLower(cursor.Current) || char.IsAsciiDigit(cursor.Current) || cursor.Current is '_' or '-' or '.' or '*'))
                cursor.Advance();
            return cursor.Slice(start, cursor.Position - start).ToString();
        }
        /// <summary>
        /// Parse a numeric token from the cursor and return an <see cref="SfBareDecimal"/> for decimals or an <see cref="SfBareInteger"/> for integers,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.4">4.2.4</see>.
        /// </summary>
        /// <remarks>Accepts an optional leading '-' sign. Integer component limited to 15 digits
        /// (maximum 12 digits for the integer component when a fractional part is present). Fractional component requires 1–3 digits.
        /// Parsing uses <see cref="CultureInfo.InvariantCulture"/> with <see cref="NumberStyles.AllowDecimalPoint"/> and <see cref="NumberStyles.AllowLeadingSign"/>.</remarks>
        /// <param name="cursor">Cursor positioned at the start of the numeric token; advances past consumed characters during parsing.</param>
        /// <returns>An <see cref="SfBareDecimal"/> when a decimal point is present; otherwise an <see cref="SfBareInteger"/> representing the parsed value.</returns>
        /// <exception cref="FormatException">
        /// Passed an invalid or out-of-range numeric text, including: missing digit at the current position;
        /// numeric component exceeding 15 digits; decimal integer component with more than 12 digits;
        /// decimal fractional component with more than 3 digits or missing fractional digits after '.'.
        /// </exception>
        private static SfBareItem ParseNumber(ref StringCursor cursor)
        {
            int start = cursor.Position;
            // Handle optional leading '-' sign for negative numbers
            if (!cursor.AtEnd && cursor.Current == '-')
            {
                cursor.Advance();
            }
            if (cursor.AtEnd || !char.IsAsciiDigit(cursor.Current))
            {
                throw new FormatException($"Expected a digit at position {cursor.Position}");
            }
            // Count the number of digits in the integer component, enforcing the maximum allowed digits for integers and decimals
            int digits = 0;
            while (!cursor.AtEnd && char.IsAsciiDigit(cursor.Current))
            {
                ++digits;
                if (digits > SfBareInteger.MaxIntegerDigits)
                    throw new FormatException($"Numeric component has too many digits (max {SfBareInteger.MaxIntegerDigits} for integer)");
                cursor.Advance();
            }
            // Check for a decimal point to determine if this is a decimal number
            bool isDecimal = false;
            if (!cursor.AtEnd && cursor.Current == '.')
            {
                isDecimal = true;
                if (digits > SfBareDecimal.MaxDecimalIntegerDigits)
                    throw new FormatException($"Decimal's integer component must have at most {SfBareDecimal.MaxDecimalIntegerDigits} digits");

                cursor.Advance();
                int fractionalDigits = 0;
                while (!cursor.AtEnd && char.IsAsciiDigit(cursor.Current))
                {
                    ++fractionalDigits;
                    if (fractionalDigits > SfBareDecimal.MaxDecimalFractionalDigits)
                        throw new FormatException($"Decimal's fractional component must have at most {SfBareDecimal.MaxDecimalFractionalDigits} digits");
                    cursor.Advance();
                }
                if (fractionalDigits == 0)
                    throw new FormatException("Decimal requires at least one fractional digit after '.'");
            }
            // Slice the span representing the parsed number
            ReadOnlySpan<char> text = cursor.Slice(start, cursor.Position - start);
            if (isDecimal)
            {
                decimal parsed = decimal.Parse(text, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                return new SfBareDecimal(parsed);
            }
            else
            {
                long parsed = long.Parse(text, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture);
                return new SfBareInteger(parsed);
            }
        }
        /// <summary>
        /// Parses a sequence of structured field parameters from the current cursor position,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.3.2">4.2.3.2</see>.
        /// </summary>
        /// <remarks>Parses consecutive parameters prefixed with <c>;</c> and stops when the next character is not a parameter delimiter or the end is reached.</remarks>
        /// <param name="cursor">Cursor positioned at the start of a parameter section; advanced as parameters are consumed.</param>
        /// <returns>An <see cref="SfParameters"/> instance containing the parsed key/value pairs. Parameters without an explicit value are stored as boolean <see langword="true"/>.</returns>
        /// <exception cref="FormatException">
        /// The input ends unexpectedly before a bare item is complete, the current position is at the end of input, or an unexpected character is encountered,
        /// including a first character that is neither a lowercase ASCII letter nor '*'.
        /// </exception>
        private static SfParameters ParseParameters(ref StringCursor cursor)
        {
            List<KeyValuePair<string, SfBareItem>> parameters = [];
            while (!cursor.AtEnd && cursor.Current == ';')
            {
                cursor.Advance();
                SkipSpace(ref cursor);
                string key = ParseKey(ref cursor); // FormatException
                SfBareItem value = new SfBareBoolean(true);
                if (!cursor.AtEnd && cursor.Current == '=')
                {
                    cursor.Advance();
                    value = ParseBareItem(ref cursor); // // FormatException
                }
                parameters.Add(new KeyValuePair<string, SfBareItem>(key, value));
            }
            return new SfParameters(parameters);
        }
        /// <summary>
        /// Parses an <see cref="SfBareString"/> value from the current cursor position, handling quoted content and supported escape sequences,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.5">4.2.5</see>.
        /// </summary>
        /// <param name="cursor">A cursor positioned at the opening double quote of the sf-string; advanced past the closing double quote when parsing succeeds.</param>
        /// <returns>An <see cref="SfBareString"/> containing the parsed string content.</returns>
        /// <exception cref="FormatException">
        /// The string is unterminated, an escape sequence is unterminated, an unsupported escape sequence is encountered, or a non-printable ASCII character is found in the string.
        /// </exception>
        private static SfBareString ParseString(ref StringCursor cursor)
        {
            cursor.Expect('"');
            StringBuilder builder = new();
            while (true)
            {
                if (cursor.AtEnd)
                {
                    throw new FormatException("Unterminated sf-string");
                }
                if (cursor.Current == '"')
                {
                    cursor.Advance();
                    return new SfBareString(builder.ToString());
                }
                if (cursor.Current == '\\')
                {
                    cursor.Advance();
                    if (cursor.AtEnd)
                        throw new FormatException("Unterminated escape sequence in sf-string");
                    if (cursor.Current != '"' && cursor.Current != '\\')
                        throw new FormatException($"Invalid escape sequence '\\{cursor.Current}' in sf-string");
                    builder.Append(cursor.Current);
                }
                else if (!SfBareString.IsValidChar(cursor.Current))
                {
                    throw new FormatException($"sf-string contains a non-printable-ASCII character at position {cursor.Position}");
                }
                else
                {
                    builder.Append(cursor.Current);
                }
                cursor.Advance();
            }
        }
        /// <summary>
        /// Parses a token from the current cursor position and creates an <see cref="SfBareToken"/> from the consumed text,
        /// per RFC 9651 section <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.2.6">4.2.6</see>.
        /// </summary>
        /// <param name="cursor">Cursor over the source text; advanced past the parsed token.</param>
        /// <returns>An <see cref="SfBareToken"/> containing the parsed token.</returns>
        /// <exception cref="FormatException">The current position does not start with a valid token character.</exception>
        private static SfBareToken ParseToken(ref StringCursor cursor)
        {
            int start = cursor.Position;
            cursor.Advance();
            while (!cursor.AtEnd && StructuredFieldSyntax.IsTokenChar(cursor.Current))
                cursor.Advance();
            return new SfBareToken(cursor.Slice(start, cursor.Position - start).ToString());
        }
        /// <summary>
        /// Advances the cursor while the current character is a space, stopping at the first non-space character or at the end.
        /// </summary>
        /// <remarks>Only the ASCII space character (' ') is skipped; tabs, line breaks, and other whitespace characters are not skipped.</remarks>
        /// <param name="cursor">Reference to the <see cref="StringCursor"/> whose position is advanced past consecutive space characters; passed by reference so the caller observes the updated position.</param>
        private static void SkipSpace(ref StringCursor cursor)
        {
            while (!cursor.AtEnd && cursor.Current == ' ')
                cursor.Advance();
        }
        /// <summary>
        /// Advances the cursor past consecutive space and tab characters.
        /// </summary>
        /// <remarks>Stops when a non-space, non-tab character is reached or when the end of the input is reached.</remarks>
        /// <param name="cursor">A reference to the cursor to advance.</param>
        private static void SkipSpaceAndHTab(ref StringCursor cursor)
        {
            while (!cursor.AtEnd && (cursor.Current == ' ' || cursor.Current == '\t'))
                cursor.Advance();
        }

        /// <summary>
        /// Stack-only cursor over a <see cref="ReadOnlySpan{T}"/> that tracks the current position and provides low-allocation
        /// helpers for advancing, slicing, and validating characters during parsing.
        /// </summary>
        /// <remarks>As a ref struct, Cursor is stack-only: it cannot be boxed, stored on the heap, or captured by closures.
        /// It is intended for high-performance, low-allocation parsing of character spans.</remarks>
        /// <param name="value">The input <see cref="ReadOnlySpan{T}"/> that backs the cursor and supplies the characters to navigate.</param>
        private ref struct StringCursor(ReadOnlySpan<char> value)
        {
            /// <summary>
            /// The underlying read-only span of characters representing the instance value.
            /// </summary>
            [DebuggerBrowsable(DebuggerBrowsableState.Never)]
            private readonly ReadOnlySpan<char> _value = value;

            /// <summary>
            /// Gets the zero-based position index of the item.
            /// </summary>
            public int Position { get; private set; }
            /// <summary>
            /// Indicates whether the current position is at or beyond the end of the underlying value.
            /// </summary>
            public readonly bool AtEnd => Position >= _value.Length;
            /// <summary>
            /// Gets the character at the current <see cref="Position"/> within the underlying value.
            /// </summary>
            /// <exception cref="IndexOutOfRangeException">The <see cref="Position"/> is beyond the end of the underlying value.</exception>"
            public readonly char Current => _value[Position];
            /// <summary>
            /// Gets the number of elements remaining from the current <see cref="Position"/> to the end of the underlying value.
            /// </summary>
            public readonly int RemainingLength => _value.Length - Position;

            /// <summary>
            /// Increments the <see cref="Position"/> property by one.
            /// </summary>
            public void Advance() => ++Position;
            /// <summary>
            /// Returns a slice of the underlying read-only character span that begins at the specified index and has the specified length.
            /// </summary>
            /// <param name="start">The zero-based starting index of the slice within the span.</param>
            /// <param name="length">The number of characters in the returned slice.</param>
            /// <returns>A <see cref="ReadOnlySpan{T}"/> that represents the specified substring of the underlying span.</returns>
            /// <exception cref="ArgumentOutOfRangeException">The <paramref name="start"/> or <paramref name="length"/> specify a range outside the bounds of the span.</exception>
            public readonly ReadOnlySpan<char> Slice(int start, int length) => _value.Slice(start, length);
            /// <summary>
            /// Verify that the current character equals the specified character and advance the position.
            /// </summary>
            /// <param name="c">The character expected at the current position.</param>
            /// <exception cref="FormatException">The end of input is reached or the current character does not match the specified character.</exception>
            public void Expect(char c)
            {
                if (AtEnd || Current != c)
                    throw new FormatException($"Expected '{c}' at position {Position}");
                Advance();
            }
        }
    }
}