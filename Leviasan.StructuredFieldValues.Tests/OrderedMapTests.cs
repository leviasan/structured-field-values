using System;
using System.Collections;
using System.Collections.Generic;
using Xunit;

namespace Leviasan.StructuredFieldValues.Tests
{
    public sealed class OrderedMapTests
    {
        [Fact]
        public void Constructor_DuplicateKeys_OverwritesInPlace()
        {
            SfParameters map = new([new("a", new SfBareInteger(1)), new("b", new SfBareInteger(2)), new("a", new SfBareInteger(3))]);
            Assert.Equal(2, map.Count);
            Assert.Equal("a", map[0].Key);
            Assert.Equal(new SfBareInteger(3), map["a"]);
        }

        [Fact]
        public void SetItem_AppendsOrReplaces()
        {
            SfParameters map = new([new("a", new SfBareInteger(1))]);
            map.SetItem("b", new SfBareInteger(2));
            map.SetItem("a", new SfBareInteger(9));
            Assert.Equal(["a", "b"], map.Keys);
            Assert.Equal(new SfBareInteger(9), map["a"]);
        }

        [Fact]
        public void Lookup_Works()
        {
            SfParameters map = new([new("a", new SfBareInteger(1))]);
            Assert.True(map.ContainsKey("a"));
            Assert.False(map.ContainsKey("z"));
            Assert.True(map.TryGetValue("a", out SfBareItem? value));
            Assert.Equal(new SfBareInteger(1), value);
            Assert.False(map.TryGetValue("z", out _));
            Assert.Throws<KeyNotFoundException>(() => map["z"]);
        }

        [Fact]
        public void Member_DefaultParametersAreEmpty()
            => Assert.Empty(new SfItem(new SfBareInteger(1)).Parameters);

        [Fact]
        public void List_And_InnerList_Enumerate()
        {
            SfList list = new([new SfItem(new SfBareInteger(1)), new SfItem(new SfBareInteger(2))]);
            Assert.Equal(2, list.Count);
            SfInnerList inner = new([new SfItem(new SfBareInteger(1))]);
            Assert.Single(inner);
            Assert.Equal(new SfBareInteger(1), inner[0].Value);
        }

        [Fact]
        public void OrderedMap_Validation()
        {
            SfParameters map = new([]);
            Assert.Throws<ArgumentNullException>(() => map.SetItem(null!, new SfBareInteger(1)));
            Assert.Throws<ArgumentNullException>(() => map.SetItem("a", null!));
            Assert.Throws<ArgumentException>(() => map.SetItem("A", new SfBareInteger(1)));
            Assert.Throws<ArgumentException>(() => new SfParameters([new("1", new SfBareInteger(1))]));
            Assert.Throws<ArgumentNullException>(() => map.TryGetValue(null!, out _));
        }

        [Fact]
        public void OrderedMap_Interfaces()
        {
            SfParameters map = new([new("a", new SfBareInteger(1)), new("b", new SfBareInteger(2))]);
            Assert.Equal(["a", "b"], map.Keys);
            Assert.Equal([new SfBareInteger(1), new SfBareInteger(2)], map.Values);
            Assert.Equal("b", map[1].Key);
            Assert.Throws<ArgumentOutOfRangeException>(() => map[2]);

            IReadOnlyDictionary<string, SfBareItem> dict = map;
            Assert.True(dict.ContainsKey("a"));
            Assert.Equal(2, dict.Count);

            IEnumerable nonGeneric = map;
            int count = 0;
            foreach (object _ in nonGeneric)
                count++;
            Assert.Equal(2, count);
        }
    }
}