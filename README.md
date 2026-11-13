# OpWeave Sample Operations

An **external Git UPM extension package** for OpWeave v1 (`W2.13` / SPEC §X). It demonstrates one custom `Operation`, one `TypeProvider`, one `Codec`, a complete Workflow, and Editor contract tests — all against the implemented public OpWeave Core API. OpWeave Core is not modified.

> **Verification status:** The sources compile against the implemented `com.opweave.workflow` public contracts, and the contract tests under `Tests~/Editor/` exercise the real Core/UnityAdapter/`OpWeave.Testing` path — but they only prove anything once they run in a Unity `6000.0` EditMode session inside a consuming project. Until that run is reported, treat this package as implemented but not yet verified.

## Package identity

- Unity package name: `com.opweave.sample.operations`
- Extension ID: `org.opweave.sample`
- Operation ID: `org.opweave.sample.make_pose@1`
- Provider ID: `org.opweave.sample.types`
- Type ID: `org.opweave.sample.BonePose` (CLR `OpWeave.Sample.Types.BonePose`, Codec `BonePoseCodec`)
- Required OpWeave public Core API: `>=1.0.0 <2.0.0`
- Minimum Unity Editor: `6000.0`
- Main Editor assembly: `OpWeave.Sample.Operations.Editor`
- Test assembly: `OpWeave.Sample.Operations.Tests` (Editor-only, `UNITY_INCLUDE_TESTS`)

## Installing

Add both OpWeave Core and this external package **explicitly** to your Unity project's `Packages/manifest.json`, pinned to full commit SHAs:

```json
{
  "dependencies": {
    "com.opweave.workflow": "https://github.com/pippirium/OpWeave-dev.git?path=/Packages/com.opweave.workflow#<CORE_COMMIT_SHA>",
    "com.opweave.sample.operations": "https://github.com/pippirium/OpWeave-Sample-Operations.git#<SAMPLE_COMMIT_SHA>"
  }
}
```

The example shows only the two OpWeave dependencies; preserve all existing dependencies in a real project. Git UPM does not transitively fetch dependencies — `package.json` intentionally declares no Git-URL dependency on `com.opweave.workflow`, so omitting the first line leaves the extension unresolvable. The package does declare the ordinary Unity dependency `com.unity.nuget.newtonsoft-json`.

## What the extension registers

- **`org.opweave.sample.make_pose@1`** — an `Immediate`, read-only, idempotent custom Operation. It reads `position` (`unity.Vector3`, required) and writes `pose` (`org.opweave.sample.BonePose`). Declared effects: `read` only; `UndoSupport.None`; requirement `edit_mode`; stateless.
- **`org.opweave.sample.types`** — an `ITypeProvider` offering the value type **`org.opweave.sample.BonePose`**, a finite 3D pose, with the strict **`BonePoseCodec`**: JSON is exactly `{"x":number,"y":number,"z":number}` — no missing members, no extra members, finite values inside the `float` range — and `Normalize` is `ToJson(FromJson(value))`, deterministic and idempotent.

`Editor/Blueprints~/` ships a single inert `*.opweave-blueprint.json` file as a marker of where future Blueprint content would live; v1 registers and executes nothing from it (SPEC §X.8–X.9).

## Example workflow

`Samples~/Workflows/create-bone-pose.opweave.json` is a complete schema-`1.0` workflow: `core.start → org.opweave.sample.make_pose` (control edge `next`), input `position: {"x":0,"y":1,"z":0}`, and the workflow output `pose` bound to the custom node's `pose` port.

**Expected result:** the run produces a `BonePose` whose `Position` is `(0, 1, 0)` — canonical JSON `{"x":0,"y":1,"z":0}` — and nothing else. The Operation performs no scene, asset, package, code, or other project-state changes. The same document is embedded in the Descriptor's `Examples`.

## Running the contract tests

Unity never compiles folders suffixed with `~`, so `Tests~/Editor/` ships as source inside the package but must be staged into the consuming project:

1. Install both packages as above into a Unity `6000.0` project that has `"com.opweave.workflow"` in its `testables` list (OpWeave.Core's `OpWeave.Testing` assembly is `UNITY_INCLUDE_TESTS`-gated).
2. Copy the resolved package's `Tests~/Editor/` (contents, not the folder) into the project's `Assets/` — e.g. `Assets/OpWeave.Sample.Operations.Tests.Editor/` — keeping the `.asmdef` and `.meta` files.
3. Run EditMode tests; the `OpWeave.Sample.Operations.Tests` assembly covers:
   - Operation eligibility, `[OpWeaveOperation]` ↔ Descriptor identity, and the full Descriptor contract (ports, effects, idempotency, requirements, examples);
   - Descriptor registration checks against a snapshot that includes this package's TypeProvider, and static validation of every Descriptor example through the real workflow compiler;
   - Harness execution of `make_pose` with the declared pose output, plus `OperationContract` idempotency and declared-effects probes;
   - Codec `Validate`/`Normalize`/`FromJson`/`ToJson` contract (strict shape, OW-TYPE-003 diagnostics, canonical determinism);
   - TypeProvider eligibility and registration alongside the standard providers;
   - `opweave.extension.json` parsing, manifest-validator acceptance, and live `ExtensionDiscovery` acceptance of the installed package;
   - static validation, Dry-Run validation, and execution of every `Samples~/Workflows/*.opweave.json` — located via `PackageInfo`, never a hard-coded path — including the prohibited-API IL scan of the production assembly.

## Security and boundaries

External Editor C# may execute during Unity's compile/reload. The OpWeave discovery whitelist controls OpWeave registration only; it is **not a sandbox**. Review extensions before importing; no credentials should be embedded in Git URLs or logs. This sample requests no permission changes and creates no CI workflows.

For public repository status and verification boundaries, see [PUBLISHING.md](PUBLISHING.md).
