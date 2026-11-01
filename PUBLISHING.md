# Public GitHub Repository Status and Verification Boundaries

The standalone repository has been created and published: [`pippirium/OpWeave-Sample-Operations`](https://github.com/pippirium/OpWeave-Sample-Operations), **public**.

## Repository publication vs runtime verification

- **Repository status**: The initial external UPM scaffold is published to GitHub under the approved public repository [`pippirium/OpWeave-Sample-Operations`](https://github.com/pippirium/OpWeave-Sample-Operations).
- **Runtime verification status**: Normal installation and runtime execution cannot yet be verified because the OpWeave Core package implementation (`com.opweave.workflow`) does not yet exist in `OpWeave-dev`.
- Publication of this repository does **not** imply that the sample is verified or ready for production use.

## Tagging and installation rules

- Create a release tag only after the actual OpWeave Core API is implemented and end-to-end Unity tests pass.
- In Unity projects (`Packages/manifest.json`), use pinned full commit SHAs for both `com.opweave.workflow` and `com.opweave.sample.operations`.
- Git UPM does not transitively resolve dependencies. Both packages must be explicitly added to the consumer project's manifest.

**Do not report W2.13 or AT-F01 as passing just because this repository and scaffold files exist.**
