#nullable enable

using System;
using System.Linq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpWeave.Core;
using OpWeave.Sample.Types;
using UnityEngine;

namespace OpWeave.Sample.Tests
{
    /// <summary>
    /// The Codec/TypeProvider contract tests of SPEC §X.6 / §B7.1 applied to
    /// <see cref="BonePoseCodec"/> and <see cref="BonePoseTypeProvider"/>:
    /// strict shape validation, canonical deterministic Normalize, CLR
    /// round-trip, and registration inside the real type snapshot.
    /// </summary>
    internal sealed class CodecContractTests
    {
        private static readonly BonePoseCodec s_codec = new BonePoseCodec();

        private static JsonValue Json(string text) =>
            JsonValue.From(JToken.Parse(text));

        // ── Validate ─────────────────────────────────────────────────────

        [Test]
        public void ValidateAcceptsTheCanonicalShape()
        {
            Assert.That(
                s_codec.Validate(Json("{\"x\":0,\"y\":1,\"z\":0}"), null),
                Is.Empty);
        }

        [Test]
        public void ValidateRejectsMalformedShapes()
        {
            var cases = new[]
            {
                Json("{\"x\":0,\"y\":1}"),                        // missing member
                Json("{\"x\":0,\"y\":1,\"z\":0,\"w\":2}"),        // extra member
                Json("{\"x\":\"left\",\"y\":1,\"z\":0}"),         // non-numeric member
                Json("{\"x\":0,\"y\":1,\"z\":3.5e38}"),           // outside float range
                Json("[0,1,0]"),                                  // not an object
                Json("0"),                                        // not an object
            };
            foreach (var value in cases)
            {
                var diagnostics = s_codec.Validate(value, null);
                Assert.IsTrue(
                    diagnostics.Any(d => d.Severity == Severity.Error),
                    "expected an OW-TYPE-003 error for " + value.CanonicalText);
            }
        }

        [Test]
        public void ValidateRejectsNonFiniteMembers()
        {
            var value = JsonValue.From(new JObject
            {
                ["x"] = new JValue(double.PositiveInfinity),
                ["y"] = new JValue(0.0),
                ["z"] = new JValue(0.0),
            });
            Assert.IsTrue(
                s_codec.Validate(value, null)
                    .Any(d => d.Severity == Severity.Error));
        }

        // ── Normalize (SPEC §B7.1) ────────────────────────────────────────

        [Test]
        public void NormalizeIsDeterministicAcrossKeyOrderAndNumberSpelling()
        {
            var a = Json("{\"z\":0,\"x\":0,\"y\":1}");
            var b = Json("{\"x\":0.0,\"y\":1.0,\"z\":0.0}");
            Assert.That(
                s_codec.Normalize(a).CanonicalText,
                Is.EqualTo(s_codec.Normalize(b).CanonicalText));
        }

        [Test]
        public void NormalizeIsIdempotentAndRevalidates()
        {
            var once = s_codec.Normalize(Json("{\"z\":0,\"x\":0,\"y\":1}"));
            Assert.That(
                s_codec.Normalize(once).CanonicalText,
                Is.EqualTo(once.CanonicalText));
            Assert.That(s_codec.Validate(once, null), Is.Empty,
                "a normalized value must be accepted by the same Codec");
        }

        [Test]
        public void ClrRoundTripPreservesTheValue()
        {
            var pose = new BonePose(new Vector3(1.5f, -2.25f, 3f));
            var json = s_codec.ToJson(pose);
            Assert.That(s_codec.Validate(json, null), Is.Empty);
            var back = s_codec.FromJson(json);
            Assert.That(back.Position, Is.EqualTo(pose.Position));
            Assert.That(
                s_codec.ToJson(back).CanonicalText,
                Is.EqualTo(json.CanonicalText));
        }

        [Test]
        public void ToJsonRejectsNonFiniteCoordinates()
        {
            Assert.Throws<ArgumentException>(
                () => s_codec.ToJson(
                    new BonePose(new Vector3(float.NaN, 0f, 0f))));
        }

        // ── TypeProvider registration (SPEC §X.6, §C3 step 4) ────────────

        [Test]
        public void ProviderClassIsEligible()
        {
            var type = typeof(BonePoseTypeProvider);
            Assert.IsNotNull(
                Attribute.GetCustomAttribute(
                    type, typeof(OpWeaveTypeProviderAttribute)),
                "the provider class must carry [OpWeaveTypeProvider]");
            Assert.IsFalse(type.IsAbstract);
            Assert.IsNotNull(type.GetConstructor(Type.EmptyTypes),
                "a public parameterless constructor is required");
            Assert.IsTrue(typeof(ITypeProvider).IsAssignableFrom(type));
        }

        [Test]
        public void OfferedTypesRegisterAlongsideStandardProviders()
        {
            var build = SampleFixtures.BuildTypes();
            Assert.That(
                build.Failures, Is.Empty,
                () => "type build failures: " + string.Join(
                    "; ", build.Failures.Select(
                        f => f.ProviderId + "/" + f.TypeId)));

            var parsed = TypeRef.Parse(SampleFixtures.BonePoseTypeId);
            Assert.IsTrue(parsed.Ok, "the Type ID must be a canonical TypeRef");
            var resolved = build.Registry.Resolve(parsed.Value!);
            Assert.IsTrue(resolved.Ok,
                "org.opweave.sample.BonePose must resolve in the snapshot");
            var definition = resolved.Value!;
            Assert.That(definition.Kind, Is.EqualTo(TypeKind.Value));
            Assert.That(definition.GenericArity, Is.EqualTo(0));
            Assert.That(definition.BaseTypes, Is.Empty);
            Assert.IsNull(definition.GenericConstraint);
            Assert.IsInstanceOf<BonePoseCodec>(definition.Codec);
            Assert.That(definition.Description, Is.Not.Empty);
        }

        [Test]
        public void OfferedTypeIdsAreCanonicalAndOutsideReservedNamespaces()
        {
            var provider = new BonePoseTypeProvider();
            foreach (var definition in provider.GetTypes())
            {
                var parsed = TypeRef.Parse(definition.Id);
                Assert.IsTrue(parsed.Ok,
                    "offered Type ID '" + definition.Id + "' must parse");
                Assert.That(
                    IdGrammar.HasReservedOperationPrefix(definition.Id),
                    Is.False,
                    "extension types must not use core.*/unity.*/opweave.*");
            }
        }
    }
}
