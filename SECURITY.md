# Security Policy

## Supported versions

| Version | Supported |
|---|---|
| 1.x | Yes |
| < 1.0 | No |

## Reporting a vulnerability

Please do not report security vulnerabilities in public issues.

Use [private vulnerability reporting](https://github.com/leviasan/structured-field-values/security/advisories/new) on GitHub.
Include the affected version, a description of the problem and, if possible, a minimal input that reproduces it.

You can expect an acknowledgement within a few days. After the fix is released, the advisory is published and the reporter is credited, unless they prefer to stay anonymous.

## Scope

The library parses and serializes untrusted strings, so the following are treated as security issues:

- denial of service from crafted input (excessive CPU or memory use, stack overflow);
- parsing differences from RFC 9651 that can lead to header smuggling or injection on serialization;
- unhandled exceptions other than the documented `FormatException` and `ArgumentException`.
