using System;
using Xunit;

namespace Leviasan.StructuredFieldValues.Tests
{
    public sealed class ImplicitConversionTests
    {
        [Fact]
        public void Integer_ConvertsToItem() => AssertConverted(new SfBareInteger(42), "42");

        [Fact]
        public void Decimal_ConvertsToItem() => AssertConverted(new SfBareDecimal(4.5m), "4.5");

        [Fact]
        public void String_ConvertsToItem() => AssertConverted(new SfBareString("hello"), "\"hello\"");

        [Fact]
        public void Token_ConvertsToItem() => AssertConverted(new SfBareToken("foo/bar"), "foo/bar");

        [Fact]
        public void ByteSequence_ConvertsToItem() => AssertConverted(new SfBareByteSequence([104, 101, 108, 108, 111]), ":aGVsbG8=:");

        [Fact]
        public void Boolean_ConvertsToItem() => AssertConverted(new SfBareBoolean(true), "?1");

        [Fact]
        public void Date_ConvertsToItem() => AssertConverted(new SfBareDate(1659578233), "@1659578233");

        [Fact]
        public void DisplayString_ConvertsToItem() => AssertConverted(new SfBareDisplayString("f\u00fc\u00fc"), "%\"f%c3%bc%c3%bc\"");

        [Fact]
        public void Conversion_PreservesBareItemInstance()
        {
            SfBareInteger bare = new(7);
            SfItem item = bare;
            Assert.Same(bare, item.Value);
        }

        [Fact]
        public void Conversion_ProducesNoParameters()
        {
            SfItem item = new SfBareToken("a");
            Assert.Empty(item.Parameters);
        }

        [Fact]
        public void Conversion_IsUsableAsDictionaryAndInnerListMember()
        {
            SfDictionary dict = new(
            [
                new("max-age", new SfBareInteger(3600)),
                new("public", new SfBareBoolean(true)),
                new("tags", new SfInnerList([new SfBareToken("a"), new SfBareToken("b")])),
            ]);

            Assert.Equal("max-age=3600, public, tags=(a b)", StructuredFieldSerializer.SerializeDictionary(dict));
        }

        [Fact]
        public void EmptyDictionary_Constructor_CreatesEmptyDictionary()
        {
            SfDictionary dict = new();
            Assert.Empty(dict);
            Assert.Null(StructuredFieldSerializer.SerializeDictionary(dict));
        }

        [Fact]
        public void EmptyParameters_Constructor_CreatesEmptyParameters()
        {
            SfParameters parameters = new();
            Assert.Empty(parameters);
            Assert.Equal("1", StructuredFieldSerializer.SerializeItem(new SfItem(new SfBareInteger(1), parameters)));
        }

        private static void AssertConverted(SfBareItem bare, string expected)
        {
            SfItem item = bare switch
            {
                SfBareInteger v => v,
                SfBareDecimal v => v,
                SfBareString v => v,
                SfBareToken v => v,
                SfBareByteSequence v => v,
                SfBareBoolean v => v,
                SfBareDate v => v,
                SfBareDisplayString v => v,
                _ => throw new InvalidOperationException(),
            };

            Assert.Same(bare, item.Value);
            Assert.Equal(expected, StructuredFieldSerializer.SerializeItem(item));
        }
    }
}