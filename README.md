# structured-field-values

The parser and serializer for HTTP Structured Field Values ([RFC 9651](https://www.rfc-editor.org/rfc/rfc9651.html)).
Usage and API are described in the [package README](Leviasan.StructuredFieldValues/README.md).

## Repository layout

| Path | Description |
|---|---|
| `Leviasan.StructuredFieldValues` | The library |
| `Leviasan.StructuredFieldValues.Tests` | xUnit tests, including the official conformance suite |

## Getting the sources

The conformance test data is a Git submodule, so clone with submodules:

```powershell
git clone --recurse-submodules https://github.com/leviasan/structured-field-values
```

If the repository is already cloned without them:

```powershell
git submodule update --init
```

Without the submodule, `Leviasan.StructuredFieldValues.Tests/StructuredFieldTests` is empty and all conformance tests fail.

## Running the tests

```powershell
dotnet test
```

With code coverage:

```powershell
dotnet test --collect:"XPlat Code Coverage"
```

## Conformance tests

The tests in `HttpWgConformanceTests` run the official [httpwg/structured-field-tests](https://github.com/httpwg/structured-field-tests) suite, which is shared by implementations of RFC 9651. The suite is mounted as a submodule at `Leviasan.StructuredFieldValues.Tests/StructuredFieldTests` and pinned to a specific commit. The pinned commit is recorded in the repository itself and is shown by:

```powershell
git submodule status
```

Cases marked `can_fail` in the suite are optional (SHOULD) requirements, so a failure there does not fail the test.

### Updating the test data

```powershell
git submodule update --remote Leviasan.StructuredFieldValues.Tests/StructuredFieldTests
dotnet test
git add Leviasan.StructuredFieldValues.Tests/StructuredFieldTests
git commit -m "Update structured-field-tests"
```

Only the new commit reference is stored in the commit. If the tests fail on the new data, either fix the library or return to the previous commit:

```powershell
git -C Leviasan.StructuredFieldValues.Tests/StructuredFieldTests checkout <commit>
```

## Building the NuGet package

```powershell
dotnet pack Leviasan.StructuredFieldValues -c Release -p:ContinuousIntegrationBuild=true -o artifacts
```

`ContinuousIntegrationBuild=true` makes the build deterministic and normalizes source paths for Source Link. It is enabled automatically on GitHub Actions, but must be passed explicitly for a local pack (for example, a manual first publish). Do not use it for everyday debugging.

## License

See [LICENSE.txt](LICENSE.txt). The conformance data keeps its own license, which is included in the submodule (`LICENSE.md`).