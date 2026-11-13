# Changelog

## 0.1.0 — Implemented extension (Unity verification pending)

- Implemented the external UPM extension against the shipped `com.opweave.workflow` public API: `org.opweave.sample.make_pose@1` (`[OpWeaveOperation]`-declared `OperationBase`, Immediate/read-only/idempotent, `edit_mode` requirement), the `org.opweave.sample.types` TypeProvider offering `org.opweave.sample.BonePose`, and the strict `BonePoseCodec` (`{"x","y","z"}` object, OW-TYPE-003 validation, canonical `ToJson(FromJson(value))` Normalize).
- Shipped the complete workflow example `Samples~/Workflows/create-bone-pose.opweave.json` (same document as the Descriptor example) and an inert `Editor/Blueprints~/` marker file.
- Replaced the placeholder tests with real `OpWeave.Testing` + public-Core contract tests: descriptor and registration checks, Codec/TypeProvider contracts, extension manifest validation and live discovery, per-workflow static + Dry-Run validation, and harness execution with the declared output.
- `package.json` now declares the `com.unity.nuget.newtonsoft-json` dependency; no Git-URL dependency on OpWeave Core is embedded.
- **Not yet verified in Unity**: compilation and EditMode test results are pending a `6000.0` run inside a consuming project with `com.opweave.workflow` in `testables`.
