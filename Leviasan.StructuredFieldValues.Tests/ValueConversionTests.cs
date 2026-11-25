using System;
using Xunit;

namespace Leviasan.StructuredFieldValues.Tests
{
    public sealed class ValueConversionTests
    {
        [Fact]
        public void Integer_RoundTrips()
        {
            long value = (SfBareInteger)42L;
            Assert.Equal(42L, value);
        }

        [Fact]
        public void Integer_OutOfRange_Throws() => Assert.ThrowsAny<ArgumentException>(() => (SfBareInteger)1_000_000_000_000_000L);

        [Fact]
        public void Decimal_RoundTrips()
        {
            decimal value = (SfBareDecimal)4.5m;
            Assert.Equal(4.5m, value);
        }

        [Fact]
        public void String_RoundTrips()
        {
            string value = (SfBareString)"hello";
            Assert.Equal("hello", value);
        }

        [Fact]
        public void String_NonPrintable_Throws() => Assert.ThrowsAny<ArgumentException>(() => (SfBareString)"a\tb");

        [Fact]
        public void Token_RoundTrips()
        {
            string value = (SfBareToken)"foo/bar";
            Assert.Equal("foo/bar", value);
        }

        [Fact]
        public void Token_Invalid_Throws() => Assert.ThrowsAny<ArgumentException>(() => (SfBareToken)"1bad");

        [Fact]
        public void ByteSequence_RoundTrips()
        {
            byte[] data = [1, 2, 3];
            byte[] value = (SfBareByteSequence)data;
            Assert.Equal(data, value);
        }

        [Fact]
        public void Boolean_ImplicitBothWays()
        {
            SfBareBoolean bare = true;
            bool value = bare;
            Assert.True(value);
        }

        [Fact]
        public void Date_FromLong_RoundTrips()
        {
            long value = (SfBareDate)1659578233L;
            Assert.Equal(1659578233L, value);
        }

        [Fact]
        public void Date_FromDateTimeOffset_ToDateTimeOffset()
        {
            DateTimeOffset moment = DateTimeOffset.FromUnixTimeSeconds(1659578233);
            SfBareDate bare = (SfBareDate)moment;
            Assert.Equal(moment, (DateTimeOffset)bare);
            Assert.Equal(1659578233L, (long)bare);
        }

        [Fact]
        public void Date_OutOfDateTimeOffsetRange_ExplicitThrows()
        {
            SfBareDate bare = (SfBareDate)999_999_999_999_999L;
            _ = Assert.ThrowsAny<Exception>(() => (DateTimeOffset)bare);
        }

        [Fact]
        public void DisplayString_RoundTrips()
        {
            string value = (SfBareDisplayString)"f\u00fc\u00fc";
            Assert.Equal("f\u00fc\u00fc", value);
        }

        [Fact]
        public void Unwrap_Null_Throws()
        {
            _ = Assert.ThrowsAny<ArgumentException>(() => (long)(SfBareInteger)null!);
            _ = Assert.ThrowsAny<ArgumentException>(() => (string)(SfBareString)null!);
            _ = Assert.ThrowsAny<ArgumentException>(() => (bool)(SfBareBoolean)null!);
        }
    }
}