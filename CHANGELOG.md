# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.0.0] - 2026-10-29
### Added
- Parser and serializer for HTTP Structured Field Values as defined in RFC 9651 (obsoletes RFC 8941).
- Support for Lists, Dictionaries and Items, including Inner Lists and Parameters.
- Support for bare item types: Integer, Decimal, String, Token, Byte Sequence, Boolean, Date and Display String.
- XML documentation shipped with the package.
- Source Link and symbol package (`.snupkg`) for debugging.
- Conformance tests based on the [httpwg/structured-field-tests](https://github.com/httpwg/structured-field-tests) suite (included as a git submodule).

[1.0.0]: https://github.com/leviasan/structured-field-values/releases/tag/v1.0.0