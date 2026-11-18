# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - Unreleased

### Added
- Parameterless constructors for `SfDictionary` and `SfParameters` that create an empty instance.
- Implicit conversions from every concrete bare item (`SfBareInteger`, `SfBareDecimal`, `SfBareString`, `SfBareToken`, `SfBareByteSequence`, `SfBareBoolean`, `SfBareDate`, `SfBareDisplayString`) to `SfItem` without parameters. The `new SfItem(...)` wrapper can now be omitted when building dictionaries, lists and inner lists.
- Conversion operators between concrete bare items and their underlying values. Wrapper-to-value conversions are implicit (`SfBareDate` to `DateTimeOffset` is explicit because it can throw); value-to-wrapper conversions are explicit because they validate input (`bool` to `SfBareBoolean` is implicit).

## [1.0.0] - 2026-10-29

### Added
- Parser and serializer for HTTP Structured Field Values as defined in RFC 9651 (obsoletes RFC 8941).
- Support for Lists, Dictionaries and Items, including Inner Lists and Parameters.
- Support for bare item types: Integer, Decimal, String, Token, Byte Sequence, Boolean, Date and Display String.
- XML documentation shipped with the package.
- Source Link and symbol package (`.snupkg`) for debugging.
- Conformance tests based on the [httpwg/structured-field-tests](https://github.com/httpwg/structured-field-tests) suite (included as a git submodule).

[1.1.0]: https://github.com/leviasan/structured-field-values/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/leviasan/structured-field-values/releases/tag/v1.0.0