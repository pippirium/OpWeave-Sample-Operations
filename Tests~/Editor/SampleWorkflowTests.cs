#nullable enable

using System.IO;
using System.Linq;
using NUnit.Framework;
using OpWeave.Core;
using OpWeave.Core.Compiler;
using OpWeave.Core.Model;
using OpWeave.Sample.Operations;
using OpWeave.Sample.Types;
using OpWeave.Testing;
using OpWeave.UnityAdapter;
using UnityEditor.PackageManager;
using UnityEngine;

namespace OpWeave.Sample.Tests
{
    /// <summary>
    /// The SPEC §X.7 workflow tests: every <c>Samples~/Workflows/*.opweave.json</c>
    /// the installed package ships is parsed by the real serializer and
    /// statically validated (and Dry-Run validated) by the real
    /// <see cref="WorkflowCompiler"/> over a snapshot that registers the
    /// extension's own Operation — then, as a separate test (§X.7), the same
    /// example's operation step is executed through the real
    /// <see cref="OperationTestHarness"/> step pipeline and its declared
    /// output is compared against the workflow's exposed <c>pose</c> output.
    /// Package files are located through <see cref="PackageInfo"/>, never a
    /// hard-coded path.
    /// </summary>
    internal sealed class SampleWorkflowTests
    {
        private static string WorkflowDirectory()
        {
            var info = PackageInfo.FindForAssembly(
                typeof(MakePoseOperation).Assembly);
            Assert.IsNotNull(
                info,
                "the production assembly must resolve to an installed UPM package");
            Assert.That(info!.name, Is.EqualTo(SampleFixtures.PackageId));
            var directory = Path.Combine(
                info.resolvedPath, "Samples~", "Workflows");
            Assert.IsTrue(Directory.Exists(directory),
                "the package must ship Samples~/Workflows/");
            return directory;
        }

        private static string[] WorkflowFiles() =>
            Directory.GetFiles(WorkflowDirectory(), "*.opweave.json")
                .OrderBy(path => path, System.StringComparer.Ordinal)
                .ToArray();

        private static WorkflowDocument ParseWorkflow(string path)
        {
            var read = WorkflowSerializer.Read(File.ReadAllText(path));
            Assert.That(read.Ok, Is.True,
                () => path + " must parse: " + string.Join(
                    "; ", read.Diagnostics.Select(
                        d => d.Code + " " + d.Message)));
            return read.Value!;
        }

        private static ValidationReport Validate(
            WorkflowCompiler compiler, WorkflowDocument document,
            ValidationMode mode) =>
            compiler.Validate(new ValidationRequest(
                document, null, mode, ActorKind.Agent));

        private static void AssertClean(
            ValidationReport report, string path, ValidationMode mode)
        {
            Assert.That(report.Ok, Is.True,
                () => path + " " + mode + " errors: " + string.Join(
                    "; ", report.Errors.Select(d => d.Code + " " + d.Message)));
            Assert.That(report.Warnings, Is.Empty,
                () => path + " " + mode + " warnings: " + string.Join(
                    "; ", report.Warnings.Select(d => d.Code + " " + d.Message)));
        }

        [Test]
        public void ShipsAtLeastOneCompleteWorkflowExample()
        {
            Assert.That(WorkflowFiles(), Is.Not.Empty,
                "SPEC §X.7 requires shippable workflow examples");

            foreach (var path in WorkflowFiles())
            {
                var document = ParseWorkflow(path);
                Assert.That(document.SchemaVersion, Is.EqualTo("1.0"));
                Assert.That(
                    document.Nodes.Select(n => n.Operation),
                    Does.Contain(SampleFixtures.OperationId),
                    "the sample must exercise the custom Operation");
            }
        }

        [Test]
        public void EverySampleWorkflowPassesStaticValidation()
        {
            var snapshot = SampleFixtures.BuildSnapshot();
            Assert.That(snapshot.Failures, Is.Empty,
                "the snapshot registers core.start and make_pose cleanly");
            var compiler = SampleFixtures.NewCompiler(snapshot);

            foreach (var path in WorkflowFiles())
            {
                var report = Validate(
                    compiler, ParseWorkflow(path), ValidationMode.Static);
                AssertClean(report, path, ValidationMode.Static);
                Assert.IsNotNull(report.Plan,
                    "static validation must yield a compiled Plan");
            }
        }

        [Test]
        public void EverySampleWorkflowPassesDryRunValidation()
        {
            var compiler =
                SampleFixtures.NewCompiler(SampleFixtures.BuildSnapshot());

            foreach (var path in WorkflowFiles())
            {
                var report = Validate(
                    compiler, ParseWorkflow(path), ValidationMode.DryRun);
                AssertClean(report, path, ValidationMode.DryRun);
                Assert.IsNotNull(report.PlannedChanges,
                    "a Dry Run report must carry the §D8 predicted changes");
                Assert.That(report.PlannedChanges, Is.Empty,
                    "a read-only example predicts no changes");
            }
        }

        [Test]
        public void ExampleOperationExecutesAndProducesTheDeclaredPose()
        {
            // Execute the custom step of the shipped example through the real
            // StepContext pipeline, with its document-declared parameter, and
            // check the value the workflow exposes as 'pose' (SPEC §X.7 —
            // separate from the static-validation tests above).
            var path = WorkflowFiles()
                .First(p => p.EndsWith(
                    "create-bone-pose.opweave.json",
                    System.StringComparison.Ordinal));
            var document = ParseWorkflow(path);

            var node = document.Nodes.Single(
                n => n.Operation == SampleFixtures.OperationId);
            Assert.That(node.Version,
                Is.EqualTo(SampleFixtures.OperationVersion));

            var output = document.Outputs.Single(o => o.Id == "pose");
            Assert.That(output.From.Node, Is.EqualTo(node.Id));
            Assert.That(output.From.Port, Is.EqualTo("pose"));

            using var harness = OperationTestHarness.Create();
            var result = harness.Run(
                SampleFixtures.OperationId,
                new { position = node.Parameters["position"] });

            Assert.That(result.Ok, Is.True, () => result.ToString());
            Assert.That(result.Diagnostics, Is.Empty, () => result.ToString());
            Assert.That(result.Changes, Is.Empty,
                "the example performs no scene, asset or project change");

            var pose = new BonePoseCodec().FromJson(result.Outputs["pose"]);
            Assert.That(
                pose.Position, Is.EqualTo(new Vector3(0f, 1f, 0f)),
                "the example's documented output: a BonePose at (0, 1, 0)");
        }
    }
}
