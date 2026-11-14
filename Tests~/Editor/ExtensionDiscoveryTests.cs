#nullable enable

using System.IO;
using System.Linq;
using NUnit.Framework;
using OpWeave.Core;
using OpWeave.Sample.Operations;
using OpWeave.Sample.Types;
using OpWeave.UnityAdapter;
using UnityEditor.PackageManager;
using UnityEngine;

namespace OpWeave.Sample.Tests
{
    /// <summary>
    /// The SPEC §X.3.2 manifest and §C3 discovery/registration tests: the
    /// installed package is located through <see cref="PackageInfo"/> (never
    /// a hard-coded path), its <c>opweave.extension.json</c> is parsed by the
    /// real reader, and the validator and the live TypeCache-driven discovery
    /// pass must both accept it with the bindings the manifest declares.
    /// </summary>
    internal sealed class ExtensionDiscoveryTests
    {
        private static string PackageRoot()
        {
            var info = PackageInfo.FindForAssembly(
                typeof(MakePoseOperation).Assembly);
            Assert.IsNotNull(
                info,
                "the production assembly must resolve to an installed UPM package");
            Assert.That(info!.name, Is.EqualTo(SampleFixtures.PackageId));
            return info.resolvedPath;
        }

        private static string ManifestText() =>
            File.ReadAllText(
                Path.Combine(PackageRoot(), "opweave.extension.json"));

        [Test]
        public void ManifestParsesAndDeclaresTheContract()
        {
            var parsed = ExtensionManifestReader.Parse(ManifestText());
            Assert.IsTrue(parsed.Ok,
                "opweave.extension.json must parse through the real reader");
            var manifest = parsed.Value!;

            Assert.That(manifest.SchemaVersion, Is.EqualTo("1.0"));
            Assert.That(manifest.ExtensionId, Is.EqualTo(SampleFixtures.ExtensionId));
            Assert.IsNotNull(manifest.CoreApi);
            Assert.That(manifest.UnityMinInclusive, Is.EqualTo("6000.0"));
            Assert.That(manifest.Assemblies,
                Is.EqualTo(new[] { SampleFixtures.EditorAssembly }));
            Assert.That(manifest.Operations,
                Is.EqualTo(new[]
                {
                    new ExtensionOperationRef(
                        SampleFixtures.OperationId,
                        SampleFixtures.OperationVersion),
                }));
            Assert.That(manifest.TypeProviders,
                Is.EqualTo(new[]
                {
                    new ExtensionTypeProviderRef(
                        SampleFixtures.ProviderId,
                        SampleFixtures.EditorAssembly),
                }));
            Assert.That(manifest.Examples, Is.Not.Empty);
            Assert.That(manifest.Blueprints, Is.Empty,
                "SPEC §X.8: blueprints ship only under Blueprints~ and " +
                "register nothing; the list stays empty until §X.9");
        }

        [Test]
        public void ManifestIsAcceptedByTheValidator()
        {
            var outcome = ExtensionManifestValidator.Validate(
                new ExtensionValidationInput(
                    SampleFixtures.PackageId,
                    PackageRoot(),
                    ManifestText(),
                    new[] { SampleFixtures.EditorAssembly },
                    new[]
                    {
                        new DiscoveredTypeProvider(
                            typeof(BonePoseTypeProvider).FullName!,
                            SampleFixtures.EditorAssembly),
                    },
                    new[]
                    {
                        new ObservedExtensionOperation(
                            SampleFixtures.OperationId,
                            SampleFixtures.OperationVersion,
                            SampleFixtures.EditorAssembly),
                    },
                    new[]
                    {
                        new ObservedExtensionType(
                            SampleFixtures.BonePoseTypeId,
                            SampleFixtures.EditorAssembly),
                    },
                    Application.unityVersion));

            Assert.That(
                outcome.Diagnostics.Where(d => d.Severity == Severity.Error),
                Is.Empty,
                () => "manifest errors: " + string.Join(
                    "; ", outcome.Diagnostics.Select(
                        d => d.Code + " " + d.Message)));
            Assert.That(outcome.Accepted, Is.True);
            Assert.That(
                outcome.TypeProviders,
                Is.EqualTo(new[]
                {
                    new TypeProviderBinding(
                        SampleFixtures.ProviderId,
                        typeof(BonePoseTypeProvider).FullName!,
                        SampleFixtures.EditorAssembly),
                }),
                "the declared Provider ID binds to the discovered class");
        }

        [Test]
        public void LiveDiscoveryAcceptsTheInstalledPackage()
        {
            var report = ExtensionDiscovery.Discover(
                new[]
                {
                    new ObservedExtensionOperation(
                        SampleFixtures.OperationId,
                        SampleFixtures.OperationVersion,
                        SampleFixtures.EditorAssembly),
                },
                new[]
                {
                    new ObservedExtensionType(
                        SampleFixtures.BonePoseTypeId,
                        SampleFixtures.EditorAssembly),
                });

            var outcome = report.Extensions.SingleOrDefault(
                e => e.PackageId == SampleFixtures.PackageId);
            Assert.IsNotNull(
                outcome,
                "the installed package must appear in live extension discovery");
            Assert.That(outcome!.Accepted, Is.True,
                () => "discovery rejected the package: " + string.Join(
                    "; ", outcome.Diagnostics.Select(
                        d => d.Code + " " + d.Message)));
            Assert.That(
                report.OperationProvenance.Keys,
                Does.Contain(new ExtensionOperationRef(
                    SampleFixtures.OperationId,
                    SampleFixtures.OperationVersion)),
                "the accepted extension publishes its Operation provenance");
        }
    }
}
