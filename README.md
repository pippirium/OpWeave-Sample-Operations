# OpWeave Sample Operations

An **external Git UPM example package** for OpWeave v1 (`W2.13` / SPEC §X). It demonstrates one custom `Operation`, one `TypeProvider`, one `Codec`, a complete Workflow, and Editor contract tests. OpWeave Core is not modified.

> **Status: scaffold, not a verified release.** The main OpWeave v1 product implementation does not yet exist in the referenced repository. The C# sources follow the approved SPEC APIs, but namespaces/signatures and Unity compilation must be reconciled and tested when the actual Core package is implemented. No validation or runtime behavior is claimed at this stage.

## Package identity

- Unity package name: `com.opweave.sample.operations`
- Extension ID: `org.opweave.sample`
- Operation ID: `org.opweave.sample.make_pose@1`
- Provider ID: `org.opweave.sample.types`
- Type ID: `org.opweave.sample.BonePose`
- Required OpWeave public Core API: `>=1.0.0 <2.0.0`
- Minimum Unity Editor: `6000.0`
- Main Editor assembly: `OpWeave.Sample.Operations.Editor`

## Installing (when OpWeave Core is available)

Add both OpWeave Core and this external package **explicitly** to your Unity project's `Packages/manifest.json`. Use real pinned revisions (full commit SHAs), not fictitious tags:

```json
{
  "dependencies": {
    "com.opweave.workflow": "https://github.com/pippirium/OpWeave-dev.git?path=/Packages/com.opweave.workflow#<CORE_COMMIT_SHA>",
    "com.opweave.sample.operations": "https://github.com/pippirium/OpWeave-Sample-Operations.git#<SAMPLE_COMMIT_SHA>"
  }
}
```

The example shows only two dependencies; preserve all existing dependencies in a real project. Git UPM does not transitively fetch the OpWeave Core package. An uncommitted or unpublished repository is not installable by this URL.

## Example workflow

`Samples~/Workflows/create-bone-pose.opweave.json` declares `core.start → org.opweave.sample.make_pose`, with `position: {"x":0,"y":1,"z":0}`. It exposes the `pose` result as a custom BonePose value. No scene or asset changes are expected.

## Test/acceptance checklist (not run)

1. Install both packages into a Unity 6000.0+ Editor project. The shipped Core needs to support the approved SPEC's `Normalize` API.
2. Confirm an Editor-only compiled assembly and extension manifest metadata are consistent; the sample must not enter Player builds.
3. Confirm the provider and custom type register and the Operation is searchable as `origin=custom`.
4. Verify the complete Workflow with the actual Validator, then execute it and check the Pose output; these are separate acceptance tests.
5. Verify `Codec.Normalize` round-trip/byte determinism and idempotency; run `Tests~/Editor` from a test project with `com.opweave.workflow` in `testables` and the documented `OpWeave.Testing` assembly available.
6. Uninstall this package and verify Operation/Type/Provider disappear on reload. Reinstall the same pinned commit and verify reappearance.
7. Exercise `DiscoveryMode=whitelist`, failed metadata/API range, wrong Editor assembly, duplicate IDs, version changes, and diagnostic behavior. Confirm no changes to the OpWeave Core repository.
8. Record the Unity version, compiler/test command, exact package commits, test results, and reviewer. Do not mark W2.13/AT-F01 complete based on this scaffold.

## Security and boundaries

External Editor C# may execute during Unity's compile/reload. The OpWeave whitelist only controls OpWeave discovery and use; it is **not a sandbox**. Review extensions before importing; no credentials should be embedded in Git URLs or logs. This sample does not request permission changes or create CI workflows.

For public repository status and verification boundaries, see [PUBLISHING.md](PUBLISHING.md).
