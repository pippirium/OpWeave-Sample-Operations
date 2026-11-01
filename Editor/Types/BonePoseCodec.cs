#nullable enable
using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OpWeave.Core;
using OpWeave.Core.Types;
using UnityEngine;

namespace OpWeave.Sample.Types
{
    // API names follow the approved SPEC B7/B7.1; verify against the implemented Core API.
    public sealed class BonePoseCodec : IClrValueCodec<BonePose>
    {
        public IReadOnlyList<Diagnostic> Validate(JsonValue value, PortConstraints? constraints)
        {
            try { _ = FromJson(value); return Array.Empty<Diagnostic>(); }
            catch (Exception)
            {
                return new[] { new Diagnostic("OW-TYPE-003", Severity.Error,
                    "BonePose must have finite numeric x, y and z fields.") };
            }
        }

        public JsonValue Normalize(JsonValue value) => ToJson(FromJson(value));

        public BonePose FromJson(JsonValue value)
        {
            if (value.ToToken() is not JObject obj || obj.Count != 3 ||
                !obj.ContainsKey("x") || !obj.ContainsKey("y") || !obj.ContainsKey("z"))
                throw new ArgumentException("Expected exactly x/y/z.", nameof(value));
            return new BonePose(new Vector3(ReadNumber(obj["x"]), ReadNumber(obj["y"]), ReadNumber(obj["z"])));
        }

        public JsonValue ToJson(BonePose value)
        {
            var p = value.Position;
            if (!float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z))
                throw new ArgumentException("BonePose contains non-finite coordinates.", nameof(value));
            // Each JValue is a floating-point token. CanonicalJson v1 then writes e notation.
            return JsonValue.From(new JObject {
                ["x"] = new JValue((double)p.x), ["y"] = new JValue((double)p.y), ["z"] = new JValue((double)p.z)
            });
        }

        private static float ReadNumber(JToken? token)
        {
            if (token == null || (token.Type != JTokenType.Integer && token.Type != JTokenType.Float))
                throw new ArgumentException("Expected a JSON number.");
            double n = token.Value<double>();
            if (double.IsNaN(n) || double.IsInfinity(n) || n < -float.MaxValue || n > float.MaxValue)
                throw new ArgumentException("Number is outside the finite float range.");
            return (float)n;
        }
    }
}
