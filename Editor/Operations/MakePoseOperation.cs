#nullable enable

using System;
using OpWeave.Core;
using OpWeave.Core.Operations;
using OpWeave.Sample.Types;
using OpWeave.UnityAdapter;
using UnityEngine;

namespace OpWeave.Sample.Operations
{
    /// <summary>
    /// The <c>org.opweave.sample.make_pose@1</c> extension Operation: converts a
    /// <c>unity.Vector3</c> position input into a value of the custom
    /// <c>org.opweave.sample.BonePose</c> type this package's TypeProvider
    /// registers (SPEC §X.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// The Operation is <see cref="ExecutionMode.Immediate"/>, read-only and
    /// <see cref="Operations.Idempotency.Idempotent"/>: it touches no scene,
    /// asset, package or code surface, so it declares only the
    /// <see cref="EffectNames.Read"/> effect, <see cref="UndoSupport.None"/>
    /// and a default retry/timeout. The same inputs always produce the same
    /// output, which is the honest §F5 Idempotent declaration the contract
    /// test verifies.
    /// </para>
    /// <para>
    /// The instance is stateless (SPEC §C2 item 4): every value it needs
    /// arrives through <see cref="StepContext"/> and every value it produces
    /// leaves through <see cref="StepContext.SetOutput{T}"/>.
    /// </para>
    /// </remarks>
    [OpWeaveOperation("org.opweave.sample.make_pose", 1)]
    public sealed class MakePoseOperation : OperationBase
    {
        /// <summary>
        /// The complete §C1 item 4 example workflow — the same document shipped
        /// under <c>Samples~/Workflows/create-bone-pose.opweave.json</c>, never
        /// a node fragment (SPEC §X.7).
        /// </summary>
        private const string ExampleWorkflowJson = @"{
  ""schemaVersion"": ""1.0"",
  ""id"": ""example.make-bone-pose"",
  ""nodes"": [
    {""id"": ""start"", ""operation"": ""core.start"", ""version"": 1},
    {""id"": ""makePose"", ""operation"": ""org.opweave.sample.make_pose"", ""version"": 1,
     ""parameters"": {""position"": {""x"": 0, ""y"": 1, ""z"": 0}}}
  ],
  ""controlEdges"": [{""from"": ""start"", ""fromPort"": ""next"", ""to"": ""makePose""}],
  ""outputs"": [{""id"": ""pose"", ""from"": {""node"": ""makePose"", ""port"": ""pose""}}]
}";

        /// <inheritdoc/>
        public override OperationDescriptor Descriptor { get; } = BuildDescriptor();

        /// <inheritdoc/>
        public override StepResult Execute(StepContext ctx)
        {
            var position = ctx.Get<Vector3>("position");
            var pose = new BonePose(position);
            // The pose port's Codec lives in this package's TypeProvider, so the
            // value is written through BonePoseCodec itself as canonical JSON —
            // the same form the port's registered Codec would produce.
            ctx.SetOutput("pose", new BonePoseCodec().ToJson(pose));
            return StepResult.Succeeded();
        }

        private static OperationDescriptor BuildDescriptor() => Op
            .Define("org.opweave.sample.make_pose", 1)
            .Summary("Converts a position (unity.Vector3) into a BonePose value.")
            .Ja("BonePoseを生成", "位置 (Vector3) からBonePose値を生成する。", "ポーズ", "生成", "ボーン")
            .Input("position", "unity.Vector3", required: true)
            .Output("pose", "org.opweave.sample.BonePose")
            .Effects(EffectNames.Read)
            .Undo(UndoSupport.None)
            .Idempotency(Idempotency.Idempotent)
            .Requires("edit_mode")
            .Build() with
            {
                DisplayName = "Create Bone Pose",
                Description =
                    "Reads a 3D position and produces an " +
                    "org.opweave.sample.BonePose value of the shape " +
                    "{\"x\":number,\"y\":number,\"z\":number}. The Operation is " +
                    "read-only: it changes no scene, asset or project state.",
                Category = "Sample",
                Tags = new[] { "sample" },
                Keywords = new[] { "pose", "bone", "vector3" },
                Examples = new[]
                {
                    new OperationExample(
                        "Create a BonePose from a position",
                        ParseJson(ExampleWorkflowJson)),
                },
            };

        private static JsonValue ParseJson(string text)
        {
            var parsed = JsonValue.Parse(text);
            if (!parsed.Ok || parsed.Value is null)
            {
                throw new InvalidOperationException(
                    "The org.opweave.sample.make_pose example workflow JSON did not parse.");
            }
            return parsed.Value;
        }
    }
}
