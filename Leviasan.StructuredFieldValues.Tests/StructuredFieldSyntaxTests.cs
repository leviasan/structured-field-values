using Xunit;

namespace Leviasan.StructuredFieldValues.Tests
{
    public sealed class StructuredFieldSyntaxTests
    {
        [Theory]
        [InlineData("a", true)]
        [InlineData("*", true)]
        [InlineData("a_b-c.d*9", true)]
        [InlineData("", false)]
        [InlineData("A", false)]
        [InlineData("1a", false)]
        [InlineData("-a", false)]
        [InlineData("aB", false)]
        [InlineData("a b", false)]
        [InlineData("a;", false)]
        public void IsValidKey(string value, bool expected) => Assert.Equal(expected, StructuredFieldSyntax.IsValidKey(value));

        [Theory]
        [InlineData('a', true)]
        [InlineData('Z', true)]
        [InlineData('5', true)]
        [InlineData(':', true)]
        [InlineData('/', true)]
        [InlineData('~', true)]
        [InlineData(' ', false)]
        [InlineData('"', false)]
        [InlineData(',', false)]
        [InlineData('(', false)]
        [InlineData(';', false)]
        public void IsTokenChar(char c, bool expected) => Assert.Equal(expected, StructuredFieldSyntax.IsTokenChar(c));
    }
}