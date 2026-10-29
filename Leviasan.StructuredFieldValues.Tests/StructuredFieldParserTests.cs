using System;
using Xunit;

namespace Leviasan.StructuredFieldValues.Tests
{
    public sealed class StructuredFieldParserTests
    {
        [Fact]
        public void Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => StructuredFieldParser.ParseItem(null!));
            Assert.Throws<ArgumentNullException>(() => StructuredFieldParser.ParseList(null!));
            Assert.Throws<ArgumentNullException>(() => StructuredFieldParser.ParseDictionary(null!));
        }

        [Theory]
        [InlineData("42", 42L)]
        [InlineData("-42", -42L)]
        [InlineData("0", 0L)]
        [InlineData("999999999999999", 999_999_999_999_999L)]
        public void Item_Integer(string input, long expected)
            => Assert.Equal(expected, Assert.IsType<SfBareInteger>(StructuredFieldParser.ParseItem(input).Value).Value);

        [Theory]
        [InlineData("4.5", "4.5")]
        [InlineData("-0.001", "-0.001")]
        [InlineData("999999999999.999", "999999999999.999")]
        [InlineData("-0.0", "0.0")]
        public void Item_Decimal(string input, string expected)
        {
            Assert.Equal(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture), Assert.IsType<SfBareDecimal>(StructuredFieldParser.ParseItem(input).Value).Value);
        }

        [Theory]
        [InlineData("1000000000000000")]
        [InlineData("1.")]
        [InlineData("1.1234")]
        [InlineData("1234567890123.0")]
        [InlineData("-")]
        [InlineData("-a")]
        public void Item_InvalidNumber_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Theory]
        [InlineData("\"hello\"", "hello")]
        [InlineData("\"\"", "")]
        [InlineData("\"a\\\"b\\\\c\"", "a\"b\\c")]
        public void Item_String(string input, string expected)
            => Assert.Equal(expected, Assert.IsType<SfBareString>(StructuredFieldParser.ParseItem(input).Value).Value);

        [Theory]
        [InlineData("\"abc")]
        [InlineData("\"a\\nb\"")]
        [InlineData("\"a\\")]
        [InlineData("\"a\tb\"")]
        [InlineData("\"é\"")]
        public void Item_InvalidString_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Theory]
        [InlineData("foo", "foo")]
        [InlineData("*foo", "*foo")]
        [InlineData("foo/bar:baz", "foo/bar:baz")]
        public void Item_Token(string input, string expected)
            => Assert.Equal(expected, Assert.IsType<SfBareToken>(StructuredFieldParser.ParseItem(input).Value).Value);

        [Theory]
        [InlineData(":aGVsbG8=:", "hello")]
        [InlineData(":aGVsbG8:", "hello")]
        [InlineData("::", "")]
        public void Item_ByteSequence(string input, string expected)
            => Assert.Equal(expected, System.Text.Encoding.ASCII.GetString(
                Assert.IsType<SfBareByteSequence>(StructuredFieldParser.ParseItem(input).Value).Value));

        [Theory]
        [InlineData(":aGVsbG8")]
        [InlineData(":a*b:")]
        [InlineData(":a:")]
        public void Item_InvalidByteSequence_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Theory]
        [InlineData("?1", true)]
        [InlineData("?0", false)]
        public void Item_Boolean(string input, bool expected)
            => Assert.Equal(expected, Assert.IsType<SfBareBoolean>(StructuredFieldParser.ParseItem(input).Value).Value);

        [Theory]
        [InlineData("?")]
        [InlineData("?2")]
        [InlineData("?true")]
        public void Item_InvalidBoolean_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Theory]
        [InlineData("@1659578233", 1659578233L)]
        [InlineData("@-62135596800", -62135596800L)]
        [InlineData("@0", 0L)]
        public void Item_Date(string input, long expected)
            => Assert.Equal(expected, Assert.IsType<SfBareDate>(StructuredFieldParser.ParseItem(input).Value).Value);

        [Theory]
        [InlineData("@1.5")]
        [InlineData("@")]
        [InlineData("@a")]
        public void Item_InvalidDate_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Theory]
        [InlineData("%\"foo\"", "foo")]
        [InlineData("%\"f%c3%bc%c3%bc\"", "füü")]
        [InlineData("%\"%22quoted%22\"", "\"quoted\"")]
        [InlineData("%\"\"", "")]
        public void Item_DisplayString(string input, string expected)
            => Assert.Equal(expected, Assert.IsType<SfBareDisplayString>(StructuredFieldParser.ParseItem(input).Value).Value);

        [Theory]
        [InlineData("%\"abc")]
        [InlineData("%abc")]
        [InlineData("%\"%\"")]
        [InlineData("%\"%a\"")]
        [InlineData("%\"%zz\"")]
        [InlineData("%\"%C3%BC\"")]
        [InlineData("%\"%c3\"")]
        [InlineData("%\"é\"")]
        public void Item_InvalidDisplayString_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Fact]
        public void Item_WithParameters()
        {
            SfItem item = StructuredFieldParser.ParseItem("1; a; b=?0;c=\"x\"");
            Assert.Equal(3, item.Parameters.Count);
            Assert.Equal(new SfBareBoolean(true), item.Parameters["a"]);
            Assert.Equal(new SfBareBoolean(false), item.Parameters["b"]);
            Assert.Equal(new SfBareString("x"), item.Parameters["c"]);
        }

        [Fact]
        public void Parameters_DuplicateKey_LastWinsKeepsPosition()
        {
            SfItem item = StructuredFieldParser.ParseItem("1;a=1;b=2;a=3");
            Assert.Equal(["a", "b"], item.Parameters.Keys);
            Assert.Equal(new SfBareInteger(3), item.Parameters["a"]);
        }

        [Theory]
        [InlineData("1;A")]
        [InlineData("1;")]
        [InlineData("1;1a")]
        public void Parameters_InvalidKey_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Theory]
        [InlineData("")]
        [InlineData("1 2")]
        [InlineData("1,2")]
        [InlineData("1\t")]
        public void Item_InvalidInput_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Fact]
        public void Item_SurroundingSpacesAllowed()
            => Assert.Equal(new SfBareInteger(1), StructuredFieldParser.ParseItem("  1  ").Value);

        [Fact]
        public void List_Basic()
        {
            SfList list = StructuredFieldParser.ParseList("1, 42,\tfoo ,\"x\"");
            Assert.Equal(4, list.Count);
            Assert.Equal(new SfBareToken("foo"), Assert.IsType<SfItem>(list[2]).Value);
        }

        [Fact]
        public void List_Empty() => Assert.Empty(StructuredFieldParser.ParseList(""));

        [Fact]
        public void List_InnerLists()
        {
            SfList list = StructuredFieldParser.ParseList("(a b;x=1 c);y, (), (1)");
            var first = Assert.IsType<SfInnerList>(list[0]);
            Assert.Equal(3, first.Count);
            Assert.Equal(new SfBareInteger(1), first[1].Parameters["x"]);
            Assert.Equal(new SfBareBoolean(true), first.Parameters["y"]);
            Assert.Empty(Assert.IsType<SfInnerList>(list[1]));
            Assert.Single(Assert.IsType<SfInnerList>(list[2]));
        }

        [Theory]
        [InlineData("1,")]
        [InlineData("1, ")]
        [InlineData("1 2")]
        [InlineData(",1")]
        [InlineData("(1")]
        [InlineData("(1,2)")]
        [InlineData("(1)a")]
        public void List_Invalid_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseList(input));

        [Fact]
        public void Dictionary_Basic()
        {
            SfDictionary dict = StructuredFieldParser.ParseDictionary("a=1, b, c=?0, d;x=2, e=(1 2)");
            Assert.Equal(["a", "b", "c", "d", "e"], dict.Keys);
            Assert.Equal(new SfBareBoolean(true), Assert.IsType<SfItem>(dict["b"]).Value);
            Assert.Equal(new SfBareBoolean(false), Assert.IsType<SfItem>(dict["c"]).Value);
            Assert.Equal(new SfBareBoolean(true), Assert.IsType<SfItem>(dict["d"]).Value);
            Assert.Equal(new SfBareInteger(2), dict["d"].Parameters["x"]);
            Assert.Equal(2, Assert.IsType<SfInnerList>(dict["e"]).Count);
        }

        [Fact]
        public void Dictionary_DuplicateKey_LastWinsKeepsPosition()
        {
            SfDictionary dict = StructuredFieldParser.ParseDictionary("a=1, b=2, a=3");
            Assert.Equal(["a", "b"], dict.Keys);
            Assert.Equal(new SfBareInteger(3), Assert.IsType<SfItem>(dict["a"]).Value);
        }

        [Fact]
        public void Dictionary_Empty() => Assert.Empty(StructuredFieldParser.ParseDictionary(""));

        [Theory]
        [InlineData("a=1,")]
        [InlineData("A=1")]
        [InlineData("a=")]
        [InlineData("=1")]
        [InlineData("a=1 b=2")]
        public void Dictionary_Invalid_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseDictionary(input));

        [Theory]
        [InlineData("%\"%a")]
        [InlineData("%\"%")]
        [InlineData("%\"abc%a\"")]
        [InlineData("%\"\u0001\"")]
        [InlineData("%\"%g0\"")]
        [InlineData("%\"%0g\"")]
        [InlineData("%\"%ff\"")]
        public void Parser_DisplayString_Errors(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Theory]
        [InlineData("(")]
        [InlineData("(1")]
        [InlineData("(1 ")]
        [InlineData("(a;b=")]
        public void Parser_UnterminatedInnerList_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseList(input));

        [Theory]
        [InlineData("a=1;b=")]
        [InlineData("a=(1")]
        [InlineData("a;")]
        [InlineData("a=1 ,")]
        [InlineData("a=1, ")]
        [InlineData("a=1,,b=2")]
        public void Parser_Dictionary_MoreErrors(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseDictionary(input));

        [Theory]
        [InlineData("!")]
        [InlineData("\u00e9")]
        [InlineData("=")]
        [InlineData(" ;")]
        public void Parser_UnknownBareItem_Throws(string input)
            => Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(input));

        [Fact]
        public void Parser_ListWithTabSeparators_AndTrailingSpaces()
        {
            Assert.Equal(2, StructuredFieldParser.ParseList(" 1\t,\t2  ").Count);
            Assert.Throws<FormatException>(() => StructuredFieldParser.ParseList("1\t"[..1] + "\t2"));
        }

        [Fact]
        public void Parser_DecimalBoundaries()
        {
            Assert.Equal(999_999_999_999.999m, ((SfBareDecimal)StructuredFieldParser.ParseItem("999999999999.999").Value).Value);
            Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem("1.1234"));
            Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem("1234567890123.5"));
            Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem("1234567890123456"));
        }

        [Fact]
        public void Parser_ByteSequence_PaddingVariants()
        {
            Assert.Equal([1, 2], ((SfBareByteSequence)StructuredFieldParser.ParseItem(":AQI=:").Value).Value);
            Assert.Equal([1, 2], ((SfBareByteSequence)StructuredFieldParser.ParseItem(":AQI:").Value).Value);
            Assert.Equal([1], ((SfBareByteSequence)StructuredFieldParser.ParseItem(":AQ==:").Value).Value);
            Assert.Equal([1], ((SfBareByteSequence)StructuredFieldParser.ParseItem(":AQ:").Value).Value);
            Assert.Throws<FormatException>(() => StructuredFieldParser.ParseItem(":A=Q=:"));
        }
    }
}