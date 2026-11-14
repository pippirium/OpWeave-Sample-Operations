#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpWeave.AgentApi;
using OpWeave.Core;
using OpWeave.Sample.Operations;
using OpWeave.Sample.Types;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.TestTools;

namespace OpWeave.Sample.Tests
{
    /// <summary>
    /// The T-W2.13-01 workflow-execution acceptance (SPEC §H1, §X.7): every
    /// complete workflow the package ships under <c>Samples~/Workflows/</c>
    /// executes end to end through the public <c>execute_workflow</c> →
    /// <c>get_run_status</c> command path of the installed
    /// <see cref="AgentApiService"/> — the same calls an Agent makes — until
    /// the Run is terminal, after which the Succeeded Run's outputs and its
    /// §F7 journaled change list (via <c>get_run_logs</c>) are compared with
    /// the results README.md documents. The single-Operation
    /// <c>OperationTestHarness</c> probe in
    /// <see cref="SampleWorkflowTests"/> stays a contract test; this class is
    /// the whole-workflow execution the ticket's Verification method
    /// requires. No Runner or Registry internals are touched — only the
    /// installed public Agent API.
    /// </summary>
    internal sealed class SampleWorkflowExecutionTests
    {
        // The overall polling deadline per workflow. A sample Run is two
        // Immediate steps, so the bound is generous — its only job is to
        // turn a stuck Run into a failure, never a hang.
        private const double PollDeadlineSeconds = 120.0;

        // The §E1 states in which a Run stops advancing for this test: the
        // three terminal states, plus needs_review and awaiting_approval —
        // both resolvable only by a human decision the Agent path can never
        // supply (SPEC §H4 item 5), so polling past them cannot succeed.
        private static readonly HashSet<string> s_decidedStates =
            new HashSet<string>(StringComparer.Ordinal)
            {
                "succeeded", "failed", "cancelled",
                "needs_review", "awaiting_approval",
            };

        private static IEnumerable<string> SampleWorkflowFileNames() =>
            Directory.GetFiles(WorkflowDirectory(), "*.opweave.json")
                .Select(Path.GetFileName)
                .OrderBy(name => name, StringComparer.Ordinal);

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

        private static string Describe(IEnumerable<Diagnostic> diagnostics) =>
            string.Join("; ", diagnostics.Select(d => d.Code + " " + d.Message));

        /// <summary>
        /// Loads the shipped document, validates it through
        /// <c>validate_workflow</c> (its planHash binds the §E2 acceptance
        /// re-check, exactly as an Agent binds it), admits it through
        /// <c>execute_workflow</c>, long-polls <c>get_run_status</c> across
        /// Editor frames until the Run is decided, then asserts the README's
        /// documented outcome.
        /// </summary>
        [UnityTest]
        public IEnumerator ExecutesToSucceededThroughTheAgentApi(
            [ValueSource(nameof(SampleWorkflowFileNames))] string fileName)
        {
            var path = Path.Combine(WorkflowDirectory(), fileName);

            // The installed §H1 service — the same single entry point the
            // Pipeline adapter binds (SPEC §H2). An uninstalled service
            // means the consuming project has no live OpWeave domain:
            // fail loudly rather than substitute another execution path.
            AgentApiService api;
            try
            {
                api = AgentApiHost.Api;
            }
            catch (InvalidOperationException exception)
            {
                Assert.Fail(
                    "the AgentApi service must be installed in this Editor domain: " +
                    exception.Message);
                yield break;
            }

            // The workflow source an Agent passes: the document body as the
            // 'workflow' argument (SPEC §H1). The 'path' alternative is
            // equally legal — the body form is used because it keeps the
            // test independent of where the package manager materialized
            // the installed package (embedded folder or PackageCache).
            var body = JObject.Parse(File.ReadAllText(path));
            var validated = api.ValidateWorkflow(
                new ValidateWorkflowRequest(body, null));
            Assert.That(validated.Ok, Is.True,
                () => path + " validate_workflow: " + Describe(validated.Errors));
            var planHash = (string?)((JObject)validated.Data!)["planHash"];
            Assert.That(planHash, Is.Not.Null,
                "validate_workflow must report the §D0.1 planHash");

            var executed = api.ExecuteWorkflow(
                new ExecuteWorkflowRequest(
                    body, null, null, ExpectedPlanHash: planHash));
            Assert.That(executed.Ok, Is.True,
                () => path + " execute_workflow: " + Describe(executed.Errors));
            var admission = (JObject)executed.Data!;
            var runId = (string?)admission["runId"];
            Assert.That(runId, Is.Not.Null.And.Not.Empty,
                "a successful execute_workflow names the admitted Run");
            Assert.That((string?)admission["state"], Is.EqualTo("queued"),
                "a no-approval Run is admitted Queued (SPEC §E2)");

            // Poll get_run_status once per Editor frame until the Run is
            // decided or the deadline passes. Every call is the bounded
            // waitSeconds:0 form: a positive wait sleeps the calling thread
            // (SPEC §H2 item 3 — the long-poll exists for worker threads),
            // and this test runs on the Editor main thread, where sleeping
            // would stall the EditorApplication.update pump that drives the
            // Run itself and the §E8 snapshot the command reads. Frame
            // yields — never sleeps — let both advance between polls.
            var deadline =
                UnityEditor.EditorApplication.timeSinceStartup +
                PollDeadlineSeconds;
            JObject? status = null;
            var state = "";
            while (UnityEditor.EditorApplication.timeSinceStartup < deadline)
            {
                var polled = api.GetRunStatus(
                    new GetRunStatusRequest(runId, WaitSeconds: 0));
                if (!polled.Ok)
                {
                    // A Run the tracker has not enrolled yet answers
                    // OW-RUN-010; the just-admitted Run arrives through the
                    // same §E8 feed the next frames deliver.
                    if (polled.Errors.Count == 1 &&
                        polled.Errors[0].Code == "OW-RUN-010")
                    {
                        yield return null;
                        continue;
                    }
                    Assert.Fail(path + " get_run_status(" + runId + "): " +
                                Describe(polled.Errors));
                }
                status = (JObject)polled.Data!;
                state = (string?)status["state"] ?? "";
                if (s_decidedStates.Contains(state))
                {
                    break;
                }
                yield return null;
            }

            Assert.That(status, Is.Not.Null,
                () => path + " get_run_status never observed Run " + runId);
            Assert.That(state, Is.EqualTo("succeeded"),
                () => path + " reached '" + state + "' — final status: " +
                      status!.ToString(Newtonsoft.Json.Formatting.None));

            // README "Expected result": every Plan step of the Run itself
            // is succeeded — the whole workflow ran, not one Operation.
            var steps = (JArray)status!["steps"]!;
            Assert.That(
                steps.Select(s => (string?)s["state"]).ToArray(),
                Is.All.EqualTo("succeeded"),
                "every step of a Succeeded sample Run is itself succeeded");

            // ... and the run produces exactly the documented 'pose'
            // output: a BonePose whose Position is (0, 1, 0), decoded
            // through the package's own Codec.
            var outputs = status["outputs"] as JObject;
            Assert.That(outputs, Is.Not.Null,
                "a Succeeded Run carries the workflow's outputs");
            Assert.That(
                outputs!.Properties().Select(p => p.Name).ToArray(),
                Is.EqualTo(new[] { "pose" }),
                "README: the run produces the 'pose' output and nothing else");
            var pose = new BonePoseCodec().FromJson(
                JsonValue.From(outputs["pose"]!));
            Assert.That(
                pose.Position, Is.EqualTo(new Vector3(0f, 1f, 0f)),
                "README: a BonePose whose Position is (0, 1, 0)");

            // README: "The Operation performs no scene, asset, package,
            // code, or other project-state changes" — the §F7 journaled
            // change union of the whole Run is empty.
            var logs = api.GetRunLogs(new GetRunLogsRequest(runId));
            Assert.That(logs.Ok, Is.True,
                () => "get_run_logs(" + runId + "): " + Describe(logs.Errors));
            var changes = (JArray)((JObject)logs.Data!)["changes"]!;
            Assert.That(changes, Is.Empty,
                "README: the sample Run records no project-state change");
        }
    }
}
