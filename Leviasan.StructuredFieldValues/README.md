# Leviasan.StructuredFieldValues

Parser and serializer for **HTTP Structured Field Values** — a typed, unambiguous format for HTTP header and trailer values.

## Implemented specifications

| RFC | Title | Status |
|---|---|---|
| [RFC 9651](https://www.rfc-editor.org/rfc/rfc9651.html) | Structured Field Values for HTTP | Implemented in full: parsing (section 4.2) and serialization (section 4.1) |
| [RFC 8941](https://www.rfc-editor.org/rfc/rfc8941.html) | Structured Field Values for HTTP | Obsoleted by RFC 9651. Its syntax is a subset of RFC 9651, so such values are parsed as well |

RFC 9651 adds two types to RFC 8941: **Date** and **Display String**. Both are supported.

## Installation

```powershell
dotnet add package Leviasan.StructuredFieldValues
```

## Data model

| Type | Description |
|---|---|
| `SfItem` | A bare item with parameters |
| `SfInnerList` | An inner list `( ... )` with parameters |
| `SfList` | A top-level list (`IReadOnlyList<SfMember>`) |
| `SfDictionary` | A top-level dictionary with preserved order and unique keys |
| `SfParameters` | Ordered parameters `;key=value` |
| `SfBareItem` | The base class of bare items |

`SfMember` is the base class of `SfItem` and `SfInnerList`. It exposes `Parameters`.

### Bare item types

| Class | `SfBareItemKind` | Example | Notes |
|---|---|---|---|
| `SfBareInteger` | `Integer` | `42` | `long`, range ±(10^15 − 1) |
| `SfBareDecimal` | `Decimal` | `4.5` | `decimal`, up to 12 integer and 3 fractional digits, rounded half-to-even |
| `SfBareString` | `String` | `"hello"` | Printable ASCII only |
| `SfBareToken` | `Token` | `foo/bar` | Validated by `StructuredFieldSyntax.IsValidToken` |
| `SfBareByteSequence` | `ByteSequence` | `:aGVsbG8=:` | `byte[]`, Base64 |
| `SfBareBoolean` | `Boolean` | `?1` | |
| `SfBareDate` | `Date` | `@1659578233` | Unix time in seconds, `ToDateTimeOffset()` |
| `SfBareDisplayString` | `DisplayString` | `%"f%c3%bc"` | A Unicode string, UTF-8 with percent-encoding |

## Usage

### Parsing

```csharp
using Leviasan.StructuredFieldValues;

// Dictionary: a=1, b;x=2, c=(1 2)
SfDictionary dict = StructuredFieldParser.ParseDictionary("a=1, b;x=2, c=(1 2)");
SfItem a = (SfItem)dict["a"];
long value = ((SfBareInteger)a.Value).Value; // 1

// List: 1, "two", three;p=?0
SfList list = StructuredFieldParser.ParseList("1, \"two\", three;p=?0");

// Item: 5;units="kg"
SfItem item = StructuredFieldParser.ParseItem("5;units=\"kg\"");
SfBareItem units = item.Parameters["units"];
```

Invalid input throws `FormatException`.

If a field arrives as several header lines, join them with `", "` before parsing, as RFC 9651 section 4.2 requires.

### Inspecting values

```csharp
foreach (var (key, member) in dict)
{
    switch (member)
    {
        case SfItem { Value: SfBareInteger number }:
            Console.WriteLine($"{key} = {number.Value}");
            break;
        case SfInnerList inner:
            Console.WriteLine($"{key} is an inner list of {inner.Count} items");
            break;
    }
}
```

### Building and serializing

```csharp
SfDictionary dict = new(
[
    new("max-age", new SfItem(new SfBareInteger(3600))),
    new("public", new SfItem(new SfBareBoolean(true))),
    new("tags", new SfInnerList([new SfItem(new SfBareToken("a")), new SfItem(new SfBareToken("b"))])),
]);

string? header = StructuredFieldSerializer.SerializeDictionary(dict);
// max-age=3600, public, tags=(a b)

string item = StructuredFieldSerializer.SerializeItem(
    new SfItem(new SfBareDate(DateTimeOffset.UtcNow), new SfParameters([new("src", new SfBareString("sensor"))])));
```

`SerializeList` and `SerializeDictionary` return `null` for an empty value. In that case the header must not be sent, as RFC 9651 section 4.1 requires.

## Behavior defined by the RFC

- Dictionary and parameter keys must match `lcalpha / "*"` followed by `lcalpha / DIGIT / "_" / "-" / "." / "*"`. Violations throw `ArgumentException`.
- A duplicate key overwrites the earlier value but keeps its original position.
- A `?1` value for a dictionary key or parameter is serialized without `=?1`.
- Decimals are always serialized with at least one fractional digit (`1.0`).
- Base64 is parsed with or without padding and is always serialized with padding.
- Parsing is strict: a trailing comma, stray characters, and an unterminated string, inner list or Display String are all errors.

## Limitations

- Parsing accepts a single string. The library does not read HTTP headers.
- Serialization produces the canonical form, so insignificant whitespace in the source is not preserved.