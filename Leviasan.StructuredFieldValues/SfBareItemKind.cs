namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents the kind of an RFC 9651 <c>bare-item</c> value.
    /// </summary>
    public enum SfBareItemKind
    {
        /// <summary>
        /// Integers have a range of -999,999,999,999,999 to 999,999,999,999,999 inclusive for IEEE 754 compatibility.
        /// </summary>
        Integer,
        /// <summary>
        /// Decimals are numbers with an integer and a fractional component.
        /// The integer component has at most 12 digits; the fractional component has at most three digits.
        /// </summary>
        Decimal,
        /// <summary>
        /// Strings are zero or more printable ASCII [RFC0020] characters.
        /// </summary>
        String,
        /// <summary>
        /// Tokens are short textual words that begin with an alphabetic character or "*", followed by zero to many token characters,
        /// which are the same as those allowed by the "token" ABNF rule defined in [HTTP] plus the ":" and "/" characters.
        /// </summary>
        Token,
        /// <summary>
        /// Byte Sequence is delimited with colons and encoded using base64.
        /// </summary>
        ByteSequence,
        /// <summary>
        /// Boolean is indicated with a leading "?" character followed by a "1" for a true value or "0" for false.
        /// </summary>
        Boolean,
        /// <summary>
        /// Dates have a data model that is similar to Integers, representing a (possibly negative) delta in seconds from 1970-01-01T00:00:00Z, excluding leap seconds.
        /// Accordingly, their serialization in textual HTTP fields is similar to that of Integers, distinguished from them with a leading "@".
        /// </summary>
        Date,
        /// <summary>
        /// Display Strings are similar to <see cref="String"/>, in that they consist of zero or more characters,
        /// but they allow Unicode scalar values (i.e., all Unicode code points except for surrogates), unlike Strings.
        /// But except that non-ASCII characters are percent-encoded; there is a leading "%" to distinguish them from Strings.
        /// </summary>
        DisplayString
    }
}