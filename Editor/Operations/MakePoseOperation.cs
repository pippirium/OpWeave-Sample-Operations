#nullable enable
using OpWeave.Core;
using OpWeave.Core.Operations;
using OpWeave.UnityAdapter;
using OpWeave.Sample.Types;
using UnityEngine;

namespace OpWeave.Sample.Operations
{
    [OpWeaveOperation]
    public sealed class MakePoseOperation : OperationBase
    {
        // Complete WF example also shipped under Samples~/Workflows.
        private const string ExampleWorkflow = @"{
  ""schemaVersion"": ""1.0"", ""id"": ""example.make-bone-pose"",
  ""nodes"": [
    {""id"": ""start"", ""operation"": ""core.start"", ""version"": 1},
    {""id"": ""makePose"", ""operation"": ""org.opweave.sample.make_pose"", ""version"": 1,
     ""parameters"": {""position"": {""x"": 0, ""y"": 1, ""z"": 0}}}
  ],
  ""controlEdges"": [{""from"": ""start"", ""to"": ""makePose""}],
  ""outputs"": [{""id"": ""pose"", ""from"": {""node"": ""makePose"", ""port"": ""pose""}}]
}";

        public override OperationDescriptor Descriptor { get; } =
            Op.Define("org.opweave.sample.make_pose", 1)
                .DisplayName("Create Bone Pose")
                .Summary("Convert a position into a custom BonePose value.")
                .Ja("ポーズを生成", "位置からBonePose値を生成する。", "ポーズ", "生成")
                .Category("Sample")
                .Input("position", "unity.Vector3", required: true)
                .Output("pose", "org.opweave.sample.BonePose")
                .Effects("read")
                .Undo(UndoSupport.None)
                .Idempotency(Idempotency.Idempotent)
                .Requires("edit_mode")
                .Example("Create a BonePose value", ExampleWorkflow)
                .Build();

        public override StepResult Execute(StepContext ctx)
        {
            Vector3 position = ctx.Get<Vector3>("position");
            ctx.SetOutput("pose", new BonePose(position));
            return StepResult.Succeeded();
        }
    }
}
