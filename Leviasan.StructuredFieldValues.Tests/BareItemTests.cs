using System;
using System.Collections;
using Xunit;

namespace Leviasan.StructuredFieldValues.Tests
{
    public sealed class BareItemTests
    {
        [Theory]
        [InlineData(999_999_999_999_999L)]
        [InlineData(-999_999_999_999_999L)]
        [InlineData(0L)]
        public void Integer_InRange_Succeeds(long value) => Assert.Equal(value, new SfBareInteger(value).Value);

        [Theory]
        [InlineData(1_000_000_000_000_000L)]
        [InlineData(-1_000_000_000_000_000L)]
        public void Integer_OutOfRange_Throws(long value) => Assert.Throws<ArgumentOutOfRangeException>(() => new SfBareInteger(value));

        [Theory]
        [InlineData("1.0005", "1.0")]
        [InlineData("1.0015", "1.002")]
        [InlineData("1.0025", "1.002")]
        [InlineData("1.5", "1.5")]
        [InlineData("-1.5", "-1.5")]
        [InlineData("-0.0", "0.0")]
        public void Decimal_RoundsHalfToEven(string input, string expected)
            => Assert.Equal(expected, new SfBareDecimal(decimal.Parse(input, System.Globalization.CultureInfo.InvariantCulture)).ToString());

        [Fact]
        public void Decimal_IntegerComponentTooLarge_Throws() => Assert.Throws<ArgumentOutOfRangeException>(() => new SfBareDecimal(1_000_000_000_000M));

        [Fact]
        public void Decimal_RoundingModeEqualityHash()
        {
            Assert.Equal(1.003m, new SfBareDecimal(1.0025m, MidpointRounding.AwayFromZero).Value);
            Assert.Equal(1.002m, new SfBareDecimal(1.0025m).Value);
            Assert.Equal(new SfBareDecimal(1.5m), new SfBareDecimal(1.50m));
            Assert.NotEqual(new SfBareDecimal(1.5m), new SfBareDecimal(1.6m));
            Assert.Equal(new SfBareDecimal(1.5m).GetHashCode(), new SfBareDecimal(1.5m).GetHashCode());
            Assert.False(new SfBareDecimal(1m).Equals(null));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfBareDecimal(-1_000_000_000_000m));
        }

        [Fact]
        public void String_NonPrintable_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SfBareString("a\tb"));
            Assert.Throws<ArgumentException>(() => new SfBareString("é"));
        }

        [Fact]
        public void String_ToString_EscapesQuoteAndBackslash()  => Assert.Equal("\"a\\\"b\\\\c\"", new SfBareString("a\"b\\c").ToString());

        [Theory]
        [InlineData("foo123/456", true)]
        [InlineData("*foo", true)]
        [InlineData("a:b", true)]
        [InlineData("", false)]
        [InlineData("1abc", false)]
        [InlineData("a b", false)]
        [InlineData("a\"b", false)]
        public void Token_Validation(string value, bool valid)
        {
            Assert.Equal(valid, StructuredFieldSyntax.IsValidToken(value));
            if (!valid)
                Assert.Throws<ArgumentException>(() => new SfBareToken(value));
        }

        [Fact]
        public void ByteSequence_ToString_IsBase64InColons() => Assert.Equal(":aGVsbG8=:", new SfBareByteSequence("hello"u8.ToArray()).ToString());

        [Fact]
        public void ByteSequence_Equality_ByContent()
        {
            Assert.Equal(new SfBareByteSequence([1, 2, 3]), new SfBareByteSequence([1, 2, 3]));
            Assert.NotEqual(new SfBareByteSequence([1, 2, 3]), new SfBareByteSequence([1, 2]));
        }

        [Fact]
        public void ByteSequence_HashAndNull()
        {
            Assert.Equal(new SfBareByteSequence([1, 2]).GetHashCode(), new SfBareByteSequence([1, 2]).GetHashCode());
            Assert.Throws<ArgumentNullException>(() => new SfBareByteSequence(null!));
            Assert.False(new SfBareByteSequence([1]).Equals((SfBareItem?)null));
            Assert.False(new SfBareByteSequence([1]).Equals(new SfBareBoolean(true)));
        }

        [Fact]
        public void Boolean_ToString()
        {
            Assert.Equal("?1", new SfBareBoolean(true).ToString());
            Assert.Equal("?0", new SfBareBoolean(false).ToString());
        }

        [Fact]
        public void Date_EqualityAndHash()
        {
            Assert.Equal(new SfBareDate(5), new SfBareDate(5));
            Assert.NotEqual(new SfBareDate(5), new SfBareDate(6));
            Assert.Equal(new SfBareDate(5).GetHashCode(), new SfBareDate(5).GetHashCode());
            Assert.False(new SfBareDate(5).Equals(null));
        }

        [Fact]
        public void Date_Range()
        {
            Assert.Equal(SfBareInteger.MaxInteger, new SfBareDate(SfBareInteger.MaxInteger).Value);
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfBareDate(SfBareInteger.MaxInteger + 1));
            Assert.Throws<ArgumentOutOfRangeException>(() => new SfBareDate(SfBareInteger.MinInteger - 1));
            Assert.Throws<InvalidOperationException>(() => new SfBareDate(SfBareInteger.MaxInteger).ToDateTimeOffset());
            Assert.Throws<InvalidOperationException>(() => new SfBareDate(SfBareInteger.MinInteger).ToDateTimeOffset());
        }

        [Fact]
        public void Date_RoundTripsDateTimeOffset()
        {
            var date = new SfBareDate(DateTimeOffset.FromUnixTimeSeconds(1659578233));
            Assert.Equal(1659578233, date.Value);
            Assert.Equal("@1659578233", date.ToString());
            Assert.Equal(DateTimeOffset.FromUnixTimeSeconds(1659578233), date.ToDateTimeOffset());
        }

        [Fact]
        public void DisplayString_ToString_PercentEncodes()
        {
            Assert.Equal("%\"f%c3%bc%c3%bc\"", new SfBareDisplayString("füü").ToString());
            Assert.Equal("%\"a%25b%22c\"", new SfBareDisplayString("a%b\"c").ToString());
        }

        [Fact]
        public void DifferentKinds_AreNotEqual()
        {
            Assert.NotEqual<SfBareItem>(new SfBareString("a"), new SfBareToken("a"));
            Assert.NotEqual<SfBareItem>(new SfBareInteger(1), new SfBareDate(1));
            Assert.True(new SfBareInteger(1) == new SfBareInteger(1));
            Assert.True(new SfBareInteger(1) != new SfBareInteger(2));
            Assert.Equal(new SfBareString("x").GetHashCode(), new SfBareString("x").GetHashCode());
        }

        [Fact]
        public void Kind_IsReported()
        {
            Assert.Equal(SfBareItemKind.Integer, new SfBareInteger(1).Kind);
            Assert.Equal(SfBareItemKind.Decimal, new SfBareDecimal(1.5m).Kind);
            Assert.Equal(SfBareItemKind.String, new SfBareString("a").Kind);
            Assert.Equal(SfBareItemKind.Token, new SfBareToken("a").Kind);
            Assert.Equal(SfBareItemKind.ByteSequence, new SfBareByteSequence([]).Kind);
            Assert.Equal(SfBareItemKind.Boolean, new SfBareBoolean(true).Kind);
            Assert.Equal(SfBareItemKind.Date, new SfBareDate(0).Kind);
            Assert.Equal(SfBareItemKind.DisplayString, new SfBareDisplayString("a").Kind);
        }

        [Fact]
        public void EqualityAcrossTypes_AndNull()
        {
            SfBareItem[] items =
            [
                new SfBareInteger(1), new SfBareDecimal(1m), new SfBareString("a"), new SfBareToken("a"),
                new SfBareByteSequence([1]), new SfBareBoolean(true), new SfBareDate(1), new SfBareDisplayString("a"),
            ];
            foreach (SfBareItem a in items)
            {
                Assert.NotNull(a);
                Assert.False(a.Equals((object?)null));
                Assert.False(a.Equals("text"));
                Assert.True(a.Equals((object)a));
                foreach (SfBareItem b in items)
                {
                    if (!ReferenceEquals(a, b))
                        Assert.NotEqual(a, b);
                }
            }
        }

        [Fact]
        public void Equality_SameKindDifferentValue()
        {
            Assert.NotEqual<SfBareItem>(new SfBareString("a"), new SfBareString("b"));
            Assert.NotEqual<SfBareItem>(new SfBareToken("a"), new SfBareToken("b"));
            Assert.NotEqual<SfBareItem>(new SfBareBoolean(true), new SfBareBoolean(false));
            Assert.NotEqual<SfBareItem>(new SfBareDisplayString("a"), new SfBareDisplayString("b"));
            Assert.Equal<SfBareItem>(new SfBareToken("a"), new SfBareToken("a"));
            Assert.Equal<SfBareItem>(new SfBareDisplayString("a"), new SfBareDisplayString("a"));
            Assert.Equal(new SfBareBoolean(true).GetHashCode(), new SfBareBoolean(true).GetHashCode());
            Assert.Equal(new SfBareToken("a").GetHashCode(), new SfBareToken("a").GetHashCode());
            Assert.Equal(new SfBareDisplayString("a").GetHashCode(), new SfBareDisplayString("a").GetHashCode());
            Assert.Equal(new SfBareInteger(3).GetHashCode(), new SfBareInteger(3).GetHashCode());
        }

        [Fact]
        public void NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => new SfBareString(null!));
            Assert.Throws<ArgumentNullException>(() => new SfBareToken(null!));
            Assert.Throws<ArgumentNullException>(() => new SfBareDisplayString(null!));
            Assert.Throws<ArgumentNullException>(() => new SfList(null!));
            Assert.Throws<ArgumentNullException>(() => new SfInnerList(null!));
            Assert.Throws<ArgumentNullException>(() => new SfDictionary(null!));
            Assert.Throws<ArgumentNullException>(() => new SfParameters(null!));
        }

        [Fact]
        public void ListAndInnerList_NonGenericEnumeration_AndIndexErrors()
        {
            SfList list = new([new SfItem(new SfBareInteger(1))]);
            SfInnerList inner = new([new SfItem(new SfBareInteger(1))]);
            foreach (object _ in (IEnumerable)list) { }
            foreach (object _ in (IEnumerable)inner) { }
            Assert.Throws<ArgumentOutOfRangeException>(() => list[1]);
            Assert.Throws<ArgumentOutOfRangeException>(() => inner[1]);
            Assert.Empty(SfList.Empty);
        }
    }
}