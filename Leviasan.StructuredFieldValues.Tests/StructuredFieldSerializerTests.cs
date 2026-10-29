using System;
using Xunit;

namespace Leviasan.StructuredFieldValues.Tests
{
    public sealed class StructuredFieldSerializerTests
    {
        private sealed class UnknownMember() : SfMember(null);

        [Fact]
        public void Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => StructuredFieldSerializer.SerializeItem(null!));
            Assert.Throws<ArgumentNullException>(() => StructuredFieldSerializer.SerializeList(null!));
            Assert.Throws<ArgumentNullException>(() => StructuredFieldSerializer.SerializeDictionary(null!));
        }

        [Fact]
        public void EmptyListAndDictionary_ReturnNull()
        {
            Assert.Null(StructuredFieldSerializer.SerializeList(new SfList([])));
            Assert.Null(StructuredFieldSerializer.SerializeList(SfList.Empty));
            Assert.Null(StructuredFieldSerializer.SerializeDictionary(new SfDictionary([])));
        }

        [Fact]
        public void Item_WithParameters()
        {
            SfItem item = new(new SfBareInteger(1), new SfParameters(
            [
                new("a", new SfBareBoolean(true)),
                new("b", new SfBareBoolean(false)),
                new("c", new SfBareString("x")),
            ]));
            Assert.Equal("1;a;b=?0;c=\"x\"", StructuredFieldSerializer.SerializeItem(item));
        }

        [Fact]
        public void List_WithInnerList()
        {
            SfList list = new(
            [
                new SfItem(new SfBareInteger(1)),
                new SfInnerList([new SfItem(new SfBareToken("a")), new SfItem(new SfBareToken("b"))], new SfParameters([new("x", new SfBareInteger(1))])),
                new SfInnerList([]),
            ]);
            Assert.Equal("1, (a b);x=1, ()", StructuredFieldSerializer.SerializeList(list));
        }

        [Fact]
        public void Dictionary_BooleanTrueOmitsValue()
        {
            SfDictionary dict = new(
            [
                new("a", new SfItem(new SfBareInteger(1))),
                new("b", new SfItem(new SfBareBoolean(true))),
                new("c", new SfItem(new SfBareBoolean(true), new SfParameters([new("x", new SfBareInteger(2))]))),
                new("d", new SfItem(new SfBareBoolean(false))),
                new("e", new SfInnerList([new SfItem(new SfBareInteger(1))])),
            ]);
            Assert.Equal("a=1, b, c;x=2, d=?0, e=(1)", StructuredFieldSerializer.SerializeDictionary(dict));
        }

        [Theory]
        [InlineData("a=1, b, c;x=2, d=?0, e=(1 2);f")]
        [InlineData("a=:aGVsbG8=:, b=@1659578233, c=%\"f%c3%bc\", d=\"x\\\"y\", e=tok, f=1.5")]
        [InlineData("a=-1;b=2.25;c")]
        public void Dictionary_RoundTrip(string input)
            => Assert.Equal(input, StructuredFieldSerializer.SerializeDictionary(StructuredFieldParser.ParseDictionary(input)));

        [Theory]
        [InlineData("1, 2.5, \"s\", t, :AQID:, ?1, @0, %\"x\"")]
        [InlineData("(a b);x, (), c;d=1")]
        public void List_RoundTrip(string input)
            => Assert.Equal(input, StructuredFieldSerializer.SerializeList(StructuredFieldParser.ParseList(input)));

        [Theory]
        [InlineData("42")]
        [InlineData("1.0")]
        [InlineData("-0.5;a;b=3")]
        [InlineData("\"a\\\\b\"")]
        [InlineData("%\"%25%22\"")]
        public void Item_RoundTrip(string input)
            => Assert.Equal(input, StructuredFieldSerializer.SerializeItem(StructuredFieldParser.ParseItem(input)));

        [Fact]
        public void Decimal_AlwaysHasFractionalDigit()
        {
            Assert.Equal("1.0", StructuredFieldSerializer.SerializeItem(new SfItem(new SfBareDecimal(1m))));
            Assert.Equal("1.5", StructuredFieldSerializer.SerializeItem(new SfItem(new SfBareDecimal(1.5m))));
            Assert.Equal("1.123", StructuredFieldSerializer.SerializeItem(new SfItem(new SfBareDecimal(1.1234m))));
        }

        [Fact]
        public void Serializer_UnknownMember_Throws()
        {
            SfList list = new([new UnknownMember()]);
            Assert.Throws<InvalidOperationException>(() => StructuredFieldSerializer.SerializeList(list));
        }

        [Fact]
        public void Serializer_DictionaryItemWithFalseOrParamsAndOtherTypes()
        {
            SfDictionary dict = new([new("a", new SfItem(new SfBareBoolean(true), new SfParameters([new("p", new SfBareBoolean(true))])))]);
            Assert.Equal("a;p", StructuredFieldSerializer.SerializeDictionary(dict));
        }
    }
}