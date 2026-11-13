#nullable enable

using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using OpWeave.Core;
using UnityEngine;

namespace OpWeave.Sample.Types
{
    /// <summary>
    /// The <see cref="IClrValueCodec{T}"/> of the custom
    /// <c>org.opweave.sample.BonePose</c> value type (SPEC §X.6, §B7): a strict
    /// <c>{"x":number,"y":number,"z":number}</c> object whose components are
    /// finite and inside the <see cref="float"/> range.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="Validate"/> reports one OW-TYPE-003 diagnostic per violation
    /// and never throws for a malformed JSON value — an ordinary validation
    /// finding is data, not an exception (SPEC §0.4). <see cref="Normalize"/>
    /// is the §B7.1 CLR-codec form <c>ToJson(FromJson(value))</c>: the
    /// <see cref="JsonValue"/> layer canonicalizes key order and number
    /// spelling, so equivalent spellings such as <c>{"z":0,"x":0,"y":1}</c> and
    /// <c>{"x":0.0,"y":1.0,"z":0.0}</c> produce one canonical text, and a
    /// second <see cref="Normalize"/> is a fixed point.
    /// </para>
    /// <para>
    /// The Codec carries no observable side effects (SPEC §B0.1):
    /// <see cref="FromJson"/>/<see cref="ToJson"/> throw
    /// <see cref="ArgumentException"/> on a value outside the contract, which
    /// the step context and the contract probe convert into diagnostics.
    /// </para>
    /// </remarks>
    public sealed class BonePoseCodec : IClrValueCodec<BonePose>
    {
        /// <inheritdoc/>
        public IReadOnlyList<Diagnostic> Validate(JsonValue value, PortConstraints? constraints)
        {
            var diagnostics = new List<Diagnostic>();
            if (value is null || value.ToToken() is not JObject obj)
            {
                diagnostics.Add(ErrorCodes.Create(
                    ErrorCodes.OW_TYPE_003,
                    "BonePose must be a JSON object of the shape {\"x\":number,\"y\":number,\"z\":number}."));
                return diagnostics;
            }
            foreach (var member in obj.Properties())
            {
                if (!string.Equals(member.Name, "x", StringComparison.Ordinal) &&
                    !string.Equals(member.Name, "y", StringComparison.Ordinal) &&
                    !string.Equals(member.Name, "z", StringComparison.Ordinal))
                {
                    diagnostics.Add(ErrorCodes.Create(
                        ErrorCodes.OW_TYPE_003,
                        $"BonePose does not allow the member '{member.Name}'."));
                }
            }
            foreach (var name in new[] { "x", "y", "z" })
            {
                var token = obj[name];
                if (token is null)
                {
                    diagnostics.Add(ErrorCodes.Create(
                        ErrorCodes.OW_TYPE_003,
                        $"BonePose is missing the required member '{name}'."));
                    continue;
                }
                var number = NumberError(token);
                if (number is not null)
                {
                    diagnostics.Add(ErrorCodes.Create(
                        ErrorCodes.OW_TYPE_003,
                        $"BonePose member '{name}' {number}"));
                }
            }
            return diagnostics;
        }

        /// <inheritdoc/>
        public JsonValue Normalize(JsonValue value) => ToJson(FromJson(value));

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">
        /// The value is not exactly a finite <c>x</c>/<c>y</c>/<c>z</c> number object.
        /// </exception>
        public BonePose FromJson(JsonValue value)
        {
            if (value.ToToken() is not JObject obj || obj.Count != 3 ||
                !obj.ContainsKey("x") || !obj.ContainsKey("y") || !obj.ContainsKey("z"))
            {
                throw new ArgumentException(
                    "Expected exactly the numeric members x, y and z.", nameof(value));
            }
            return new BonePose(new Vector3(
                ReadNumber(obj["x"]), ReadNumber(obj["y"]), ReadNumber(obj["z"])));
        }

        /// <inheritdoc/>
        /// <exception cref="ArgumentException">Any coordinate is non-finite.</exception>
        public JsonValue ToJson(BonePose value)
        {
            var p = value.Position;
            if (!float.IsFinite(p.x) || !float.IsFinite(p.y) || !float.IsFinite(p.z))
            {
                throw new ArgumentException(
                    "BonePose contains a non-finite coordinate.", nameof(value));
            }
            return JsonValue.From(new JObject
            {
                ["x"] = new JValue((double)p.x),
                ["y"] = new JValue((double)p.y),
                ["z"] = new JValue((double)p.z),
            });
        }

        // The violation text for a member token, or null when it is a
        // representable finite number.
        private static string? NumberError(JToken token)
        {
            if (token.Type != JTokenType.Integer && token.Type != JTokenType.Float)
            {
                return "must be a JSON number.";
            }
            var n = token.Value<double>();
            if (double.IsNaN(n) || double.IsInfinity(n))
            {
                return "must be finite.";
            }
            if (n < -float.MaxValue || n > float.MaxValue)
            {
                return "is outside the representable float range.";
            }
            return null;
        }

        private static float ReadNumber(JToken? token)
        {
            var error = token is null ? "is missing." : NumberError(token);
            if (error is not null)
            {
                throw new ArgumentException("A BonePose member " + error);
            }
            return (float)token!.Value<double>();
        }
    }
}
