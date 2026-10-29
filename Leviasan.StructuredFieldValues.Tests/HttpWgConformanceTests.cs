using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace Leviasan.StructuredFieldValues.Tests
{
    public sealed class HttpWgConformanceTests
    {
        private static readonly string Root = Path.Combine(AppContext.BaseDirectory, "StructuredFieldTests");
        public static TheoryData<string, int, string> ParseCases() => Load(includeSerialisation: false);
        public static TheoryData<string, int, string> SerializeCases() => Load(includeSerialisation: true);

        [Theory]
        [MemberData(nameof(ParseCases))]
        public void Parse(string file, int index, string name)
        {
            JsonElement test = GetTest(file, index);
            bool mustFail = test.TryGetProperty("must_fail", out JsonElement mf) && mf.GetBoolean();
            bool canFail = test.TryGetProperty("can_fail", out JsonElement cf) && cf.GetBoolean();
            string type = test.GetProperty("header_type").GetString()!;
            string raw = string.Join(", ", test.GetProperty("raw").EnumerateArray().Select(e => e.GetString()));

            object? parsed = null;
            Exception? error = Record.Exception(() => parsed = ParseAny(type, raw));

            if (mustFail)
            {
                Assert.True(error is FormatException, $"[{file}] '{name}': expected FormatException for '{raw}', got {(error?.GetType().Name ?? "success")}");
                return;
            }
            if (error is not null)
            {
                if (canFail)
                    return;
                Assert.Fail($"[{file}] '{name}': unexpected {error.GetType().Name} for '{raw}': {error.Message}");
            }

            JsonElement expected = test.GetProperty("expected");
            object expectedModel = BuildAny(type, expected);
            if (!canFail)
                Assert.True(StructurallyEqual(expectedModel, parsed!), $"[{file}] '{name}': parsed structure differs from expected for '{raw}'");

            string? canonical = Canonical(test, raw);
            Assert.Equal(canonical, SerializeAny(parsed!));
        }
        [Theory]
        [MemberData(nameof(SerializeCases))]
        public void Serialize(string file, int index, string name)
        {
            JsonElement test = GetTest(file, index);
            bool mustFail = test.TryGetProperty("must_fail", out JsonElement mf) && mf.GetBoolean();
            bool canFail = test.TryGetProperty("can_fail", out JsonElement cf) && cf.GetBoolean();
            string type = test.GetProperty("header_type").GetString()!;

            if (mustFail)
            {
                Exception? failure = Record.Exception(() => SerializeAny(BuildAny(type, test.GetProperty("expected"))));
                Assert.True(failure is not null, $"[{file}] '{name}': expected serialization to fail");
                return;
            }

            string? actual = null;
            Exception? error = Record.Exception(() => actual = SerializeAny(BuildAny(type, test.GetProperty("expected"))));
            if (error is not null)
            {
                if (canFail)
                    return;
                Assert.Fail($"[{file}] '{name}': unexpected {error.GetType().Name}: {error.Message}");
            }

            string? canonical = Canonical(test, test.TryGetProperty("raw", out JsonElement r)
                ? string.Join(", ", r.EnumerateArray().Select(e => e.GetString()))
                : null);
            if (!canFail)
                Assert.Equal(canonical, actual);
        }

        private static string? Canonical(JsonElement test, string? raw)
        {
            if (test.TryGetProperty("canonical", out JsonElement canonical))
            {
                string[] lines = [.. canonical.EnumerateArray().Select(e => e.GetString()!)];
                return lines.Length == 0 ? null : string.Join(", ", lines);
            }
            return string.IsNullOrEmpty(raw) ? null : raw;
        }
        private static object ParseAny(string type, string raw) => type switch
        {
            "item" => StructuredFieldParser.ParseItem(raw),
            "list" => StructuredFieldParser.ParseList(raw),
            "dictionary" => StructuredFieldParser.ParseDictionary(raw),
            _ => throw new InvalidOperationException(type),
        };
        private static string? SerializeAny(object value) => value switch
        {
            SfItem item => StructuredFieldSerializer.SerializeItem(item),
            SfList list => StructuredFieldSerializer.SerializeList(list),
            SfDictionary dict => StructuredFieldSerializer.SerializeDictionary(dict),
            _ => throw new InvalidOperationException(value.GetType().Name),
        };
        private static object BuildAny(string type, JsonElement e) => type switch
        {
            "item" => BuildItem(e),
            "list" => new SfList(e.EnumerateArray().Select(BuildMember)),
            "dictionary" => new SfDictionary(e.EnumerateArray()
                .Select(p => new KeyValuePair<string, SfMember>(p[0].GetString()!, BuildMember(p[1])))),
            _ => throw new InvalidOperationException(type),
        };
        private static SfMember BuildMember(JsonElement e)
            => e[0].ValueKind == JsonValueKind.Array
                ? new SfInnerList(e[0].EnumerateArray().Select(BuildItem), BuildParameters(e[1]))
                : BuildItem(e);
        private static SfItem BuildItem(JsonElement e) => new(BuildBare(e[0]), BuildParameters(e[1]));
        private static SfParameters BuildParameters(JsonElement e)
            => new(e.EnumerateArray().Select(p => new KeyValuePair<string, SfBareItem>(p[0].GetString()!, BuildBare(p[1]))));
        private static SfBareItem BuildBare(JsonElement e)
        {
            switch (e.ValueKind)
            {
                case JsonValueKind.True:
                    return new SfBareBoolean(true);
                case JsonValueKind.False:
                    return new SfBareBoolean(false);
                case JsonValueKind.String:
                    return new SfBareString(e.GetString()!);
                case JsonValueKind.Number:
                    string text = e.GetRawText();
                    if (text.Contains('.') || text.Contains('e') || text.Contains('E'))
                        return new SfBareDecimal(decimal.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture));
                    return new SfBareInteger(long.Parse(text, CultureInfo.InvariantCulture));
                case JsonValueKind.Object:
                    string kind = e.GetProperty("__type").GetString()!;
                    JsonElement value = e.GetProperty("value");
                    return kind switch
                    {
                        "token" => new SfBareToken(value.GetString()!),
                        "binary" => new SfBareByteSequence(Base32Decode(value.GetString()!)),
                        "date" => new SfBareDate(value.GetInt64()),
                        "displaystring" => new SfBareDisplayString(value.GetString()!),
                        _ => throw new InvalidOperationException(kind),
                    };
                default:
                    throw new InvalidOperationException(e.ValueKind.ToString());
            }
        }
        private static bool StructurallyEqual(object a, object b) => (a, b) switch
        {
            (SfItem x, SfItem y) => x.Value.Equals(y.Value) && ParametersEqual(x.Parameters, y.Parameters),
            (SfInnerList x, SfInnerList y) => x.Count == y.Count
                && Enumerable.Range(0, x.Count).All(i => StructurallyEqual(x[i], y[i]))
                && ParametersEqual(x.Parameters, y.Parameters),
            (SfList x, SfList y) => x.Count == y.Count && Enumerable.Range(0, x.Count).All(i => StructurallyEqual(x[i], y[i])),
            (SfDictionary x, SfDictionary y) => x.Count == y.Count
                && Enumerable.Range(0, x.Count).All(i => x[i].Key == y[i].Key && StructurallyEqual(x[i].Value, y[i].Value)),
            _ => false,
        };
        private static bool ParametersEqual(SfParameters x, SfParameters y)
            => x.Count == y.Count && Enumerable.Range(0, x.Count).All(i => x[i].Key == y[i].Key && x[i].Value.Equals(y[i].Value));
        private static byte[] Base32Decode(string value)
        {
            const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
            List<byte> bytes = [];
            int buffer = 0, bits = 0;
            foreach (char c in value.TrimEnd('='))
            {
                buffer = (buffer << 5) | Alphabet.IndexOf(c);
                bits += 5;
                if (bits >= 8)
                {
                    bits -= 8;
                    bytes.Add((byte)((buffer >> bits) & 0xFF));
                }
            }
            return [.. bytes];
        }
        private static JsonElement GetTest(string file, int index)
        {
            using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Root, file)));
            return doc.RootElement[index].Clone();
        }
        private static TheoryData<string, int, string> Load(bool includeSerialisation)
        {
            TheoryData<string, int, string> data = [];
            string folder = includeSerialisation ? Path.Combine(Root, "serialisation-tests") : Root;
            foreach (string path in Directory.GetFiles(folder, "*.json"))
            {
                string file = Path.GetRelativePath(Root, path);
                using JsonDocument doc = JsonDocument.Parse(File.ReadAllText(path));
                int index = 0;
                foreach (JsonElement test in doc.RootElement.EnumerateArray())
                    data.Add(file, index++, test.GetProperty("name").GetString()!);
            }
            return data;
        }
    }
}