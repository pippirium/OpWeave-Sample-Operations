#nullable enable
using NUnit.Framework;
using OpWeave.Testing;
using UnityEngine;

namespace OpWeave.Sample.Tests
{
    public sealed class OperationContractTests
    {
        [Test]
        public void CustomOperationDescriptorMatchesRegistryContract()
        {
            OperationContract.AssertDescriptorValid("org.opweave.sample.make_pose");
        }

        [Test]
        public void CustomOperationIsIdempotentAndHasNoUndeclaredEffects()
        {
            var inputs = new { position = new Vector3(0, 1, 0) };
            OperationContract.AssertIdempotency("org.opweave.sample.make_pose", inputs);
            OperationContract.AssertDeclaredEffectsOnly("org.opweave.sample.make_pose", inputs);
        }
    }
}
