#nullable enable

using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpWeave.Core;
using OpWeave.Core.Compiler;
using OpWeave.Core.Model;
using OpWeave.Core.Operations;
using OpWeave.Core.Persistence;
using OpWeave.Sample.Operations;
using OpWeave.Sample.Types;
using OpWeave.Testing;
using UnityEngine;

namespace OpWeave.Sample.Tests
{
    /// <summary>
    /// The Operation contract tests of SPEC §X.5 / §M.6 item 2 applied to
    /// <c>org.opweave.sample.make_pose@1</c> — run through the real
    /// <see cref="OperationTestHarness"/> step pipeline and the public
    /// registry/compiler checks, never against strings or mocks.
    /// </summary>
    internal sealed class OperationContractTests
    {
        private static JsonValue PositionInput =>
            JsonValue.From(JObject.Parse("{\"x\":0,\"y\":1,\"z\":0}"));

        [Test]
        public void OperationClassIsEligibleAndIdentityMatchesDescriptor()
        {
            Assert.That(
                OperationEligibility.Check(typeof(MakePoseOperation)),
                Is.Empty,
                "the SPEC §C2 item 1 class screen must pass");

            var declared = OperationEligibility.DeclaredIdentityOf(
                typeof(MakePoseOperation));
            Assert.IsNotNull(declared, "the class must carry [OpWeaveOperation]");
            Assert.That(declared!.Id, Is.EqualTo(SampleFixtures.OperationId));
            Assert.That(
                declared.Version, Is.EqualTo(SampleFixtures.OperationVersion));

            var descriptor = new MakePoseOperation().Descriptor;
            Assert.That(descriptor.Id, Is.EqualTo(declared.Id),
                "attribute id must equal the Descriptor id (SPEC §C2 item 2)");
            Assert.That(descriptor.Version, Is.EqualTo(declared.Version),
                "attribute version must equal the Descriptor version");
        }

        [Test]
        public void DescriptorDeclaresTheFullRequiredSurface()
        {
            var d = new MakePoseOperation().Descriptor;
            Assert.That(d.DisplayName, Is.Not.Empty);
            Assert.That(d.Summary, Is.Not.Empty);
            Assert.That(d.Summary.Length,
                Is.LessThanOrEqualTo(OperationDescriptorValidator.SummaryMaxLength));
            Assert.That(d.Description, Is.Not.Empty);
            Assert.That(d.Category, Is.Not.Empty);

            var input = d.Inputs.Single();
            Assert.That(input.Name, Is.EqualTo("position"));
            Assert.That(input.Type, Is.EqualTo("unity.Vector3"));
            Assert.That(input.Required, Is.True);

            var output = d.Outputs.Single();
            Assert.That(output.Name, Is.EqualTo("pose"));
            Assert.That(output.Type, Is.EqualTo(SampleFixtures.BonePoseTypeId));

            Assert.That(d.SideEffects, Is.EquivalentTo(new[] { EffectNames.Read }));
            Assert.That(d.Undo, Is.EqualTo(UndoSupport.None));
            Assert.That(d.Idempotency, Is.EqualTo(Idempotency.Idempotent));
            Assert.That(d.ExecutionMode, Is.EqualTo(ExecutionMode.Immediate));
            Assert.That(d.ControlOutputs, Is.EquivalentTo(new[] { "next" }));
            Assert.That(d.DefaultTimeoutSeconds, Is.GreaterThan(0));
            Assert.IsNotNull(d.DefaultRetry);
            Assert.That(
                d.Requirements.Select(r => r.Kind), Does.Contain("edit_mode"));
            Assert.That(d.Examples, Is.Not.Empty,
                "SPEC §X.5 requires at least one complete workflow example");
        }

        [Test]
        public void DescriptorPassesRegistrationChecksAgainstExtensionSnapshot()
        {
            var types = SampleFixtures.BuildTypes();
            Assert.That(types.Failures, Is.Empty,
                "the type build must accept every provider's candidates");

            var diagnostics = OperationDescriptorValidator.Validate(
                new MakePoseOperation().Descriptor, types.Registry);
            Assert.That(
                diagnostics, Is.Empty,
                () => "registration violations: " + string.Join(
                    "; ", diagnostics.Select(d => d.Code + " " + d.Message)));
        }

        [Test]
        public void EveryDescriptorExampleIsACompleteValidatableWorkflow()
        {
            var snapshot = SampleFixtures.BuildSnapshot();
            Assert.That(snapshot.Failures, Is.Empty,
                "the operation snapshot must register both operations cleanly");
            var compiler = SampleFixtures.NewCompiler(snapshot);

            foreach (var example in new MakePoseOperation().Descriptor.Examples)
            {
                var read = WorkflowSerializer.Read(
                    example.WorkflowJson.CanonicalText);
                Assert.IsTrue(read.Ok,
                    "descriptor example '" + example.Title + "' must parse");
                var report = compiler.Validate(new ValidationRequest(
                    read.Value!, null, ValidationMode.Static, ActorKind.Agent));
                Assert.That(report.Ok, Is.True,
                    () => "descriptor example '" + example.Title +
                        "' validation errors: " + string.Join(
                            "; ", report.Errors.Select(d => d.Code + " " + d.Message)));
                Assert.That(report.Warnings, Is.Empty,
                    () => "descriptor example '" + example.Title +
                        "' warnings: " + string.Join(
                            "; ", report.Warnings.Select(d => d.Code + " " + d.Message)));
            }
        }

        [Test]
        public void HarnessRunProducesTheDeclaredPoseOutput()
        {
            using var harness = OperationTestHarness.Create();
            var result = harness.Run(
                SampleFixtures.OperationId,
                new { position = PositionInput });

            Assert.That(result.Ok, Is.True, () => result.ToString());
            Assert.That(result.Diagnostics, Is.Empty, () => result.ToString());
            Assert.That(result.Changes, Is.Empty,
                "a read-only Operation records no changes");

            Assert.IsTrue(
                result.Outputs.TryGetValue("pose", out var pose),
                "the step must write its 'pose' output");
            var codec = new BonePoseCodec();
            Assert.That(
                codec.Validate(pose!, null), Is.Empty,
                "the output must conform to the declared BonePose codec");
            var value = codec.FromJson(pose!);
            Assert.That(value.Position, Is.EqualTo(new Vector3(0f, 1f, 0f)));

            var journalTypes = result.Journal.Select(r => r.Type).ToList();
            Assert.That(journalTypes, Does.Contain(JournalRecordType.StepStarted));
            Assert.That(journalTypes, Does.Contain(JournalRecordType.StepSucceeded));
        }

        [Test]
        public void DeclaredIdempotencyAndDeclaredEffectsHold()
        {
            var inputs = new { position = PositionInput };
            OperationContract.AssertIdempotency(
                SampleFixtures.OperationId, inputs);
            OperationContract.AssertDeclaredEffectsOnly(
                SampleFixtures.OperationId, inputs);
        }

        [Test]
        public void OperationAssemblyReferencesNoSpecProhibitedApi()
        {
            var warnings = OperationIlScan.Scan(
                typeof(MakePoseOperation).Assembly);
            Assert.That(
                warnings, Is.Empty,
                () => "SPEC §G0 prohibited API references: " + string.Join(
                    "; ", warnings.Select(w => w.ToString())));
        }
    }
}
