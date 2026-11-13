#nullable enable

using System;
using System.Collections.Generic;
using OpWeave.Core;
using OpWeave.Core.Compiler;
using OpWeave.Core.Operations;
using OpWeave.Core.Registry;
using OpWeave.Operations;
using OpWeave.Sample.Operations;
using OpWeave.Sample.Types;
using OpWeave.UnityAdapter;

namespace OpWeave.Sample.Tests
{
    /// <summary>
    /// The public-API plumbing the contract tests share: the identities this
    /// package declares, and a type/operation snapshot plus workflow compiler
    /// built the way SPEC §C3 builds them — standard providers and
    /// <c>core.start</c> beside this package's custom candidates — using only
    /// the public Core/UnityAdapter surface (no Registry or Runner internals).
    /// </summary>
    internal static class SampleFixtures
    {
        internal const string PackageId = "com.opweave.sample.operations";
        internal const string ExtensionId = "org.opweave.sample";
        internal const string OperationId = "org.opweave.sample.make_pose";
        internal const int OperationVersion = 1;
        internal const string ProviderId = "org.opweave.sample.types";
        internal const string BonePoseTypeId = "org.opweave.sample.BonePose";
        internal const string EditorAssembly = "OpWeave.Sample.Operations.Editor";
        internal const string StandardOperationsAssembly = "OpWeave.Operations";

        /// <summary>
        /// The provider candidates the §C3 step-4 type build receives: the two
        /// standard providers plus this package's provider bound to its declared
        /// Provider ID with <see cref="OperationOrigin.Custom"/>.
        /// </summary>
        internal static TypeProviderCandidate[] ProviderCandidates() => new[]
        {
            new TypeProviderCandidate(
                CoreTypeProvider.ProviderId,
                new CoreTypeProvider(),
                OperationOrigin.Standard),
            new TypeProviderCandidate(
                UnityTypeProvider.ProviderId,
                new UnityTypeProvider(),
                OperationOrigin.Standard),
            new TypeProviderCandidate(
                ProviderId,
                new BonePoseTypeProvider(),
                OperationOrigin.Custom),
        };

        /// <summary>The type snapshot over every candidate, built like §C3 step 4.</summary>
        internal static TypeRegistryBuildResult BuildTypes() =>
            TypeRegistryBuilder.Build(
                ProviderCandidates(), new UnityClassRelationProvider());

        /// <summary>
        /// The §C3 step-6 candidate of one instantiated Operation: the
        /// attribute-declared identity, the defining assembly and the compiled
        /// assembly fingerprint the registry consumes, computed the public way.
        /// </summary>
        internal static OperationCandidate CandidateFor(
            IOperation operation, OperationOrigin origin)
        {
            var declared = OperationEligibility.DeclaredIdentityOf(operation.GetType());
            var fingerprint = WeakFingerprintProvider.ForDefiningType(operation.GetType());
            var assemblyName =
                fingerprint.Ok && fingerprint.Value is { } fp
                    ? fp.DefiningAssemblyName
                    : operation.GetType().Assembly.GetName().Name ?? string.Empty;
            var implementationHash =
                fingerprint.Ok && fingerprint.Value is { } value
                    ? value.DefiningAssemblySha256
                    : new string('0', 64);
            return new OperationCandidate(
                operation.Descriptor,
                origin,
                assemblyName,
                declared?.SourceFile,
                implementationHash,
                HasPollImplementation: false,
                PackageId: origin == OperationOrigin.Custom ? PackageId : null,
                Implementation: operation,
                DeclaredId: declared?.Id,
                DeclaredVersion: declared?.Version);
        }

        /// <summary>
        /// The operation snapshot the examples validate against:
        /// <c>core.start</c> (required by every workflow, SPEC §D5) registered
        /// as Standard beside this package's custom Operation — the two
        /// operations any sample workflow here names.
        /// </summary>
        internal static RegistrySnapshot BuildSnapshot()
        {
            var types = BuildTypes();
            var candidates = new[]
            {
                CandidateFor(new StartOperation(), OperationOrigin.Standard),
                CandidateFor(new MakePoseOperation(), OperationOrigin.Custom),
            };
            return RegistrySnapshotBuilder.Build(
                candidates, types.Registry, types.Failures);
        }

        /// <summary>
        /// The public snapshot-backed <see cref="WorkflowCompiler"/> production
        /// entry point, with the UnityAdapter type probe and plan provenance a
        /// host supplies. Inside the Editor the adapter's bootstrap has bound
        /// the Dry-Run environment, so <see cref="ValidationMode.DryRun"/>
        /// requests run Preflight for real.
        /// </summary>
        internal static WorkflowCompiler NewCompiler(RegistrySnapshot snapshot) =>
            new WorkflowCompiler(
                snapshot,
                (nodeId, parameters) => new StubStaticContext(nodeId, parameters),
                new UnityTypeProbe(),
                UnityPlanProvenance.Capture(
                    IntegrityLevel.Weak, ProviderCandidates()));

        /// <summary>The static context the D3/D6 phases hand to ValidateStatic.</summary>
        private sealed class StubStaticContext : StaticContext
        {
            private readonly IReadOnlyDictionary<string, InputValue> _parameters;

            internal StubStaticContext(
                string nodeId, IReadOnlyDictionary<string, InputValue> parameters)
            {
                NodeId = nodeId;
                _parameters = parameters;
            }

            public override string NodeId { get; }

            public override IReadOnlyDictionary<string, InputValue> Parameters =>
                _parameters;

            public override T Get<T>(string port) =>
                throw new NotSupportedException(
                    "the fixture context resolves no CLR values");
        }
    }
}
