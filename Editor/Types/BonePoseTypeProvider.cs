#nullable enable
using System;
using System.Collections.Generic;
using OpWeave.Core;
using OpWeave.Core.Types;

namespace OpWeave.Sample.Types
{
    // Attribute signature and namespace must be verified against shipped Core implementation.
    [OpWeaveTypeProvider]
    public sealed class BonePoseTypeProvider : ITypeProvider
    {
        public IEnumerable<TypeDefinition> GetTypes()
        {
            yield return new TypeDefinition(
                "org.opweave.sample.BonePose", TypeKind.Value, 0,
                Array.Empty<string>(), null,
                "Pose containing a finite 3D position.", new BonePoseCodec());
        }
    }
}
