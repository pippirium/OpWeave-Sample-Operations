#nullable enable
using UnityEngine;

namespace OpWeave.Sample.Types
{
    /// <summary>Simple custom value type used to exercise the external Codec contract.</summary>
    public readonly struct BonePose
    {
        public Vector3 Position { get; }
        public BonePose(Vector3 position) => Position = position;
    }
}
