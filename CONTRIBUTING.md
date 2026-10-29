# Contributing

Thank you for your interest in the project.

## Getting started

1. Clone with submodules (they hold the conformance test data):
   ```powershell
   git clone --recurse-submodules https://github.com/leviasan/structured-field-values
   ```
   If you already cloned without them: `git submodule update --init`.
2. Install the .NET 10 SDK.
3. Build and run the tests:
   ```powershell
   dotnet build
   dotnet test
   ```

## Workflow

- The `master` branch is protected: changes go in through pull requests only.
- Create a branch from `dev` (or `master`), make your change and open a pull request.
- CI (build and tests) must pass before merging.
- Keep pull requests focused: one logical change per PR.

## Guidelines

- Follow RFC 9651. Behavior changes must be backed by the RFC or the conformance suite.
- Add or update tests in `Leviasan.StructuredFieldValues.Tests` for every fix and feature.
- Document new public members with XML comments. The build must stay free of warnings.
- Do not make breaking changes to the public API without discussing them in an issue first (the project follows [Semantic Versioning](https://semver.org/)).
- Add a line to `CHANGELOG.md` under the upcoming version.
- Do not edit files in `Leviasan.StructuredFieldValues.Tests/StructuredFieldTests`. It is a submodule of [httpwg/structured-field-tests](https://github.com/httpwg/structured-field-tests); report test data problems upstream.

## Reporting issues

For bugs, include the library version, the input string, the expected result and the actual result.
Security problems must be reported privately, see [SECURITY.md](SECURITY.md).

## License

By contributing, you agree that your contributions are licensed under the project license (see [LICENSE.txt](LICENSE.txt)).
