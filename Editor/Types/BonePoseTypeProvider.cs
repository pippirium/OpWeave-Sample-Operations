#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OpWeave.Core;

namespace OpWeave.Sample.Types
{
    /// <summary>
    /// The package's <see cref="ITypeProvider"/> (SPEC §X.6): registered under
    /// the Provider ID <c>org.opweave.sample.types</c> declared in
    /// <c>opweave.extension.json</c>, and offering the custom
    /// <c>org.opweave.sample.BonePose</c> value type with its
    /// <see cref="BonePoseCodec"/>.
    /// </summary>
    /// <remarks>
    /// Stateless and deterministic (SPEC §C0.1): <see cref="GetTypes"/> yields
    /// the same candidate definitions on every call.
    /// </remarks>
    [OpWeaveTypeProvider]
    public sealed class BonePoseTypeProvider : ITypeProvider
    {
        /// <summary>
        /// The §B8 schema fragment of <c>org.opweave.sample.BonePose</c> — the
        /// strict object shape <see cref="BonePoseCodec"/> validates, carried on
        /// the definition because an external Codec cannot implement the
        /// internal schema hook. Any member outside the §B8 keyword subset
        /// degrades to the unconstrained fragment, never a registration error.
        /// </summary>
        private static readonly JsonValue s_schema = JsonValue.From(new JObject
        {
            ["type"] = "object",
            ["required"] = new JArray("x", "y", "z"),
            ["additionalProperties"] = false,
            ["properties"] = new JObject
            {
                ["x"] = new JObject { ["type"] = "number" },
                ["y"] = new JObject { ["type"] = "number" },
                ["z"] = new JObject { ["type"] = "number" },
            },
        });

        /// <inheritdoc/>
        public IEnumerable<TypeDefinition> GetTypes()
        {
            yield return new TypeDefinition(
                "org.opweave.sample.BonePose",
                TypeKind.Value,
                0,
                Array.Empty<string>(),
                null,
                "Pose containing a finite 3D position.",
                new BonePoseCodec())
            {
                Schema = s_schema,
            };
        }
    }
}
