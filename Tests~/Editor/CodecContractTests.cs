#nullable enable
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using OpWeave.Core;
using OpWeave.Sample.Types;

namespace OpWeave.Sample.Tests
{
    public sealed class CodecContractTests
    {
        [Test]
        public void NormalizeIsIdempotentAndCanonicalAcrossKeyOrder()
        {
            var codec = new BonePoseCodec();
            var a = JsonValue.From(JToken.Parse("{\"z\":0,\"x\":0,\"y\":1}"));
            var b = JsonValue.From(JToken.Parse("{\"x\":0.0,\"y\":1.0,\"z\":0.0}"));
            var normal = codec.Normalize(a);
            Assert.That(normal.CanonicalText, Is.EqualTo(codec.Normalize(b).CanonicalText));
            Assert.That(normal.CanonicalText, Is.EqualTo(codec.Normalize(normal).CanonicalText));
        }

        [Test]
        public void InvalidJsonDoesNotPassValidation()
        {
            var codec = new BonePoseCodec();
            var value = JsonValue.From(JToken.Parse("{\"x\":0,\"y\":1}"));
            Assert.That(codec.Validate(value, null), Is.Not.Empty);
        }
    }
}
