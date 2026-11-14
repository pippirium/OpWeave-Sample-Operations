# Public GitHub Repository Status and Verification Boundaries

The standalone repository exists and is public: [`pippirium/OpWeave-Sample-Operations`](https://github.com/pippirium/OpWeave-Sample-Operations).

## Repository publication vs runtime verification

- **Repository status**: The external UPM extension is implemented and published to GitHub under [`pippirium/OpWeave-Sample-Operations`](https://github.com/pippirium/OpWeave-Sample-Operations): a working custom Operation, TypeProvider and Codec written against the public `com.opweave.workflow` contracts, plus a complete sample workflow and `OpWeave.Testing`-based contract tests.
- **Runtime verification status**: Implementation against the real API does not equal a verified install. Acceptance requires resolving this package in a Unity `6000.0` project **from a pushed commit SHA** (not a local folder) together with `com.opweave.workflow` in `testables`, and running the shipped EditMode tests to green.
- Publication of this repository does **not** imply that the sample is verified or ready for production use.

## Tagging and installation rules

- Create a release tag only after end-to-end Unity tests pass on the tagged commit.
- In Unity projects (`Packages/manifest.json`), use pinned full commit SHAs for both `com.opweave.workflow` and `com.opweave.sample.operations`.
- Git UPM does not transitively resolve dependencies. Both packages must be explicitly added to the consumer project's manifest.

**Do not report W2.13 or AT-F01 as passing just because this repository and its sources exist.**
