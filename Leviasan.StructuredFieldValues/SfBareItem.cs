using System;
using System.Globalization;
using System.Text;

namespace Leviasan.StructuredFieldValues
{
    /// <summary>
    /// Represents an RFC 9651 <c>bare-item</c>: a single value of one of the supported types.
    /// </summary>
    /// <remarks>
    /// The concrete type reflects the kind of the value: <see cref="SfBareInteger"/>, <see cref="SfBareDecimal"/>, <see cref="SfBareString"/>,
    /// <see cref="SfBareToken"/>, <see cref="SfBareByteSequence"/>, <see cref="SfBareBoolean"/>, <see cref="SfBareDate"/> and <see cref="SfBareDisplayString"/>.
    /// </remarks>
    public abstract class SfBareItem : IEquatable<SfBareItem>
    {
        /// <summary>
        /// Gets the kind of the bare item, indicating which type of value it holds.
        /// </summary>
        public abstract SfBareItemKind Kind { get; }

        /// <inheritdoc/>
        public abstract bool Equals(SfBareItem? other);
        /// <inheritdoc/>
        public sealed override bool Equals(object? obj) => Equals(obj as SfBareItem);
        /// <inheritdoc/>
        public abstract override int GetHashCode();
        /// <summary>
        /// Returns the RFC 9651 serialization of this bare item.
        /// </summary>
        public abstract override string ToString();

        /// <summary>
        /// Determines whether two bare items are equal.
        /// </summary>
        public static bool operator ==(SfBareItem? left, SfBareItem? right) => left is null ? right is null : left.Equals(right);
        /// <summary>
        /// Determines whether two bare items are not equal.
        /// </summary>
        public static bool operator !=(SfBareItem? left, SfBareItem? right) => !(left == right);
    }
    /// <summary>
    /// Represents a sf-integer.
    /// </summary>
    public sealed class SfBareInteger : SfBareItem
    {
        /// <summary>
        /// The minimum value of an <see cref="SfBareItemKind.Integer"/>: -(10^15 - 1).
        /// </summary>
        public const long MinInteger = -999_999_999_999_999L;
        /// <summary>
        /// The maximum value of an <see cref="SfBareItemKind.Integer"/>: 10^15 - 1.
        /// </summary>
        public const long MaxInteger = 999_999_999_999_999L;
        /// <summary>
        /// Maximum number of digits allowed in the integer portion of a numeric value.
        /// </summary>
        public const int MaxIntegerDigits = 15;

        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareInteger"/> class with the specified integer value.
        /// </summary>
        /// <param name="value">The integer value.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="value"/> is outside [<see cref="MinInteger"/>, <see cref="MaxInteger"/>].</exception>
        public SfBareInteger(long value)
        {
            if (value < MinInteger || value > MaxInteger)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"sf-integer must be within [{MinInteger}, {MaxInteger}]");
            Value = value;
        }

        /// <summary>
        /// Gets the integer value.
        /// </summary>
        public long Value { get; }
        /// <inheritdoc/>
        public override SfBareItemKind Kind => SfBareItemKind.Integer;

        /// <inheritdoc/>
        public override bool Equals(SfBareItem? other) => other is SfBareInteger item && item.Value == Value;
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Kind, Value);
        /// <summary>
        /// Returns the serialization of this bare item per RFC 9651 <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.4">section 4.1.4</see>.
        /// </summary>
        public override string ToString() => Value.ToString(CultureInfo.InvariantCulture);
    }
    /// <summary>
    /// Represents a sf-decimal.
    /// </summary>
    public sealed class SfBareDecimal : SfBareItem
    {
        /// <summary>
        /// The maximum magnitude of an <see cref="SfBareItemKind.Decimal"/>'s integer component: 10^12 - 1.
        /// </summary>
        public const long MaxDecimalIntegerComponent = 999_999_999_999L;
        /// <summary>
        /// Maximum number of digits allowed in the integer portion of a decimal value.
        /// </summary>
        public const int MaxDecimalIntegerDigits = 12;
        /// <summary>
        /// Maximum number of fractional digits allowed for decimal values.
        /// </summary>
        public const int MaxDecimalFractionalDigits = 3;

        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareDecimal"/> class with the specified decimal value. The value is rounded to at most 3 fractional digits
        /// using round-half-to-even, per <see href="https://www.rfc-editor.org/info/rfc9651/#section-4.1.5">section 4.1.5</see>.
        /// </summary>
        /// <param name="value">The decimal value.</param>
        /// <exception cref="ArgumentOutOfRangeException">The integer component's magnitude exceeds <see cref="MaxDecimalIntegerComponent"/>.</exception>
        public SfBareDecimal(decimal value) : this(value, MidpointRounding.ToEven) { }
        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareDecimal"/> class with the specified decimal value and rounding mode.
        /// The value is rounded to at most 3 fractional digits using the specified rounding mode.
        /// </summary>
        /// <param name="value">The decimal value.</param>
        /// <param name="mode">The rounding mode to use.</param>
        /// <exception cref="ArgumentException">The <paramref name="mode"/> is not a valid value of <see cref="MidpointRounding"/>.</exception>
        /// <exception cref="ArgumentOutOfRangeException">The integer component's magnitude exceeds <see cref="MaxDecimalIntegerComponent"/>.</exception>
        public SfBareDecimal(decimal value, MidpointRounding mode)
        {
            decimal rounded = Math.Round(value, 3, mode);
            if (Math.Abs(Math.Truncate(rounded)) > MaxDecimalIntegerComponent)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"sf-decimal's integer component must have a magnitude of at most {MaxDecimalIntegerComponent}");
            Value = rounded;
        }

        /// <summary>
        /// Gets the decimal value.
        /// </summary>
        public decimal Value { get; }
        /// <inheritdoc/>
        public override SfBareItemKind Kind => SfBareItemKind.Decimal;

        /// <inheritdoc/>
        public override bool Equals(SfBareItem? other) => other is SfBareDecimal item && item.Value == Value;
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Kind, Value);
        /// <summary>
        /// Returns the serialization of this bare item per RFC 9651 <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.5">section 4.1.5</see>.
        /// </summary>
        public override string ToString() => Value.ToString("0.0##", CultureInfo.InvariantCulture);
    }
    /// <summary>
    /// Represents a sf-string.
    /// </summary>
    public sealed class SfBareString : SfBareItem
    {
        /// <summary>
        /// Determines whether the specified character is allowed in a sf-string (printable ASCII, 0x20-0x7E).
        /// </summary>
        /// <param name="c">The character to check.</param>
        /// <returns><see langword="true"/> if the character is allowed; otherwise, <see langword="false"/>.</returns>
        public static bool IsValidChar(char c) => c >= 0x20 && c <= 0x7E;

        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareString"/> class with the specified string value.
        /// </summary>
        /// <param name="value">The string value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> contains a character outside the printable ASCII range (0x20-0x7E).</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public SfBareString(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            foreach (char c in value)
            {
                if (!IsValidChar(c))
                    throw new ArgumentException("sf-string may only contain printable ASCII characters (0x20-0x7E)", nameof(value));
            }
            Value = value;
        }

        /// <summary
        /// >Gets the string value.
        /// </summary>
        public string Value { get; }
        /// <inheritdoc/>
        public override SfBareItemKind Kind => SfBareItemKind.String;

        /// <inheritdoc/>
        public override bool Equals(SfBareItem? other) => other is SfBareString item && string.Equals(item.Value, Value, StringComparison.Ordinal);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Kind, Value);
        /// <summary>
        /// Returns the serialization of this bare item per RFC 9651 <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.6">section 4.1.6</see>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder builder = new(Value.Length + 2);
            builder.Append('"');
            foreach (char c in Value)
            {
                if (c == '"' || c == '\\')
                    builder.Append('\\');
                builder.Append(c);
            }
            return builder.Append('"').ToString();
        }
    }
    /// <summary>
    /// Represents a sf-token.
    /// </summary>
    public sealed class SfBareToken : SfBareItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareToken"/> class with the specified token value.
        /// </summary>
        /// <param name="value">The token value.</param>
        /// <exception cref="ArgumentException"><paramref name="value"/> is not a valid <c>sf-token</c> per <see cref="StructuredFieldSyntax.IsValidToken(ReadOnlySpan{char})"/>.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public SfBareToken(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            if (!StructuredFieldSyntax.IsValidToken(value))
                throw new ArgumentException("Value must be a valid sf-token", nameof(value));
            Value = value;
        }

        /// <summary>
        /// Gets the token value.
        /// </summary>
        public string Value { get; }
        /// <inheritdoc/>
        public override SfBareItemKind Kind => SfBareItemKind.Token;

        /// <inheritdoc/>
        public override bool Equals(SfBareItem? other) => other is SfBareToken item && string.Equals(item.Value, Value, StringComparison.Ordinal);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Kind, Value);
        /// <summary>
        /// Returns the serialization of this bare item per RFC 9651 <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.7">section 4.1.7</see>.
        /// </summary>
        public override string ToString() => Value;
    }
    /// <summary>
    /// Represents a sf-binary.
    /// </summary>
    public sealed class SfBareByteSequence : SfBareItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareByteSequence"/> class with the specified byte sequence value.
        /// </summary>
        /// <param name="value">The byte sequence value.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public SfBareByteSequence(byte[] value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }

        /// <summary>
        /// Gets the byte sequence value.
        /// </summary>
        public byte[] Value { get; }
        /// <inheritdoc/>
        public override SfBareItemKind Kind => SfBareItemKind.ByteSequence;

        /// <inheritdoc/>
        public override bool Equals(SfBareItem? other) => other is SfBareByteSequence item && Value.AsSpan().SequenceEqual(item.Value);
        /// <inheritdoc/>
        public override int GetHashCode()
        {
            HashCode hash = new();
            hash.Add(Kind);
            hash.AddBytes(Value);
            return hash.ToHashCode();
        }
        /// <summary>
        /// Returns the serialization of this bare item per RFC 9651 <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.8">section 4.1.8</see>.
        /// </summary>
        public override string ToString() => $":{Convert.ToBase64String(Value)}:";
    }
    /// <summary>
    /// Represents a sf-boolean.
    /// </summary>
    /// <remarks>
    /// Initializes a new instance of the <see cref="SfBareBoolean"/> class with the specified boolean value.
    /// </remarks>
    /// <param name="value">The boolean value.</param>
    public sealed class SfBareBoolean(bool value) : SfBareItem
    {
        /// <summary>
        /// Gets the boolean value.
        /// </summary>
        public bool Value { get; } = value;
        /// <inheritdoc/>
        public override SfBareItemKind Kind => SfBareItemKind.Boolean;

        /// <inheritdoc/>
        public override bool Equals(SfBareItem? other) => other is SfBareBoolean item && item.Value == Value;
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Kind, Value);
        /// <summary>
        /// Returns the serialization of this bare item per RFC 9651 <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.9">section 4.1.9</see>.
        /// </summary>
        public override string ToString() => Value ? "?1" : "?0";
    }
    /// <summary>
    /// Represents a sf-date.
    /// </summary>
    public sealed class SfBareDate : SfBareItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareDate"/> class representing <paramref name="value"/> seconds relative to the
        /// Unix epoch 1970-01-01T00:00:00Z, which may be negative to represent dates before the epoch.
        /// </summary>
        /// <param name="value">The number of seconds since the Unix epoch.</param>
        /// <exception cref="ArgumentOutOfRangeException">The <paramref name="value"/> is outside [<see cref="SfBareInteger.MinInteger"/>, <see cref="SfBareInteger.MaxInteger"/>].</exception>
        public SfBareDate(long value)
        {
            if (value < SfBareInteger.MinInteger || value > SfBareInteger.MaxInteger)
                throw new ArgumentOutOfRangeException(nameof(value), value, $"sf-date must be within [{SfBareInteger.MinInteger}, {SfBareInteger.MaxInteger}]");
            Value = value;
        }
        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareDate"/> class from <paramref name="value"/> truncating to whole seconds.
        /// </summary>
        /// <param name="value">The date and time value.</param>
        public SfBareDate(DateTimeOffset value) : this(value.ToUnixTimeSeconds()) { }

        /// <summary>
        /// Gets the date as seconds since the Unix epoch.
        /// </summary>
        public long Value { get; }
        /// <inheritdoc/>
        public override SfBareItemKind Kind => SfBareItemKind.Date;

        /// <inheritdoc/>
        public override bool Equals(SfBareItem? other) => other is SfBareDate item && item.Value == Value;
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Kind, Value);
        /// <summary>
        /// Gets the date as a <see cref="DateTimeOffset"/>.
        /// </summary>
        /// <exception cref="InvalidOperationException">The <see cref="Value"/> is less than -62,135,596,800. -or- seconds is greater than 253,402,300,799.</exception>
        public DateTimeOffset ToDateTimeOffset()
        {
            try
            {
                return DateTimeOffset.FromUnixTimeSeconds(Value);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new InvalidOperationException($"The {nameof(Value)} is less than -62,135,596,800 or greater than 253,402,300,799", ex);
            }
        }
        /// <summary>
        /// Returns the serialization of this bare item per RFC 9651 <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.10">section 4.1.10</see>.
        /// </summary>
        public override string ToString() => $"@{Value.ToString(CultureInfo.InvariantCulture)}";
    }
    /// <summary>
    /// Represents a sf-displaystring.
    /// </summary>
    public sealed class SfBareDisplayString : SfBareItem
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SfBareDisplayString"/> class with the specified display string value.
        /// </summary>
        /// <param name="value">The display string value.</param>
        /// <exception cref="ArgumentNullException">The <paramref name="value"/> is <see langword="null"/>.</exception>
        public SfBareDisplayString(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            Value = value;
        }

        /// <summary>
        /// Gets the display string value.
        /// </summary>
        public string Value { get; }
        /// <inheritdoc/>
        public override SfBareItemKind Kind => SfBareItemKind.DisplayString;

        /// <inheritdoc/>
        public override bool Equals(SfBareItem? other) => other is SfBareDisplayString item && string.Equals(item.Value, Value, StringComparison.Ordinal);
        /// <inheritdoc/>
        public override int GetHashCode() => HashCode.Combine(Kind, Value);
        /// <summary>
        /// Returns the serialization of this bare item per RFC 9651 <see href="https://www.rfc-editor.org/rfc/rfc9651.html#section-4.1.11">section 4.1.11</see>.
        /// </summary>
        public override string ToString()
        {
            StringBuilder builder = new();
            builder.Append("%\"");
            foreach (byte b in Encoding.UTF8.GetBytes(Value))
            {
                if (b == (byte)'%' || b == (byte)'"' || b < 0x20 || b >= 0x7F)
                    builder.Append('%').Append(b.ToString("x2", CultureInfo.InvariantCulture));
                else
                    builder.Append((char)b);
            }
            return builder.Append('"').ToString();
        }
    }
}