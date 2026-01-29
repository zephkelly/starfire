using System;
using UnityEngine;

namespace StarfireV2
{
    /// <summary>
    /// Color harmony modes for procedural palette generation.
    /// </summary>
    public enum ColorHarmonyMode
    {
        /// <summary>Colors adjacent on the color wheel (±30°). Creates cohesive, calm palettes.</summary>
        Analogous,
        /// <summary>Colors opposite on the wheel (180°). Creates high contrast, vibrant palettes.</summary>
        Complementary,
        /// <summary>Three colors evenly spaced (120° apart). Creates balanced, dynamic palettes.</summary>
        Triadic,
        /// <summary>Base + two colors adjacent to complement. Combines harmony with contrast.</summary>
        SplitComplementary,
        /// <summary>Monochromatic - same hue, varying saturation and value.</summary>
        Monochromatic,
    }

    /// <summary>
    /// A 4-color gradient palette used by nebula layers.
    /// Color1 is outer/faint, Color4 is core/brightest.
    /// </summary>
    [Serializable]
    public struct ColorPalette : IEquatable<ColorPalette>
    {
        /// <summary>Outer/faint color - typically low saturation, low value.</summary>
        public Color Color1;

        /// <summary>Mid-tone color - medium saturation and value.</summary>
        public Color Color2;

        /// <summary>Brighter color - higher saturation and value.</summary>
        public Color Color3;

        /// <summary>Core/brightest color - highest saturation and value, often warm-shifted.</summary>
        public Color Color4;

        /// <summary>Default nebula palette (purple-pink-orange gradient).</summary>
        public static ColorPalette Default => new ColorPalette
        {
            Color1 = new Color(0.1f, 0.05f, 0.2f, 1f),
            Color2 = new Color(0.4f, 0.1f, 0.3f, 1f),
            Color3 = new Color(0.8f, 0.3f, 0.4f, 1f),
            Color4 = new Color(1f, 0.8f, 0.6f, 1f),
        };

        /// <summary>
        /// Interpolate between two palettes using HSV color space for natural transitions.
        /// </summary>
        public static ColorPalette Lerp(ColorPalette a, ColorPalette b, float t)
        {
            return new ColorPalette
            {
                Color1 = LerpHSV(a.Color1, b.Color1, t),
                Color2 = LerpHSV(a.Color2, b.Color2, t),
                Color3 = LerpHSV(a.Color3, b.Color3, t),
                Color4 = LerpHSV(a.Color4, b.Color4, t),
            };
        }

        /// <summary>
        /// Interpolate colors in HSV space for smoother, more natural transitions.
        /// Handles hue wrapping to take the shortest path around the color wheel.
        /// </summary>
        public static Color LerpHSV(Color a, Color b, float t)
        {
            Color.RGBToHSV(a, out float h1, out float s1, out float v1);
            Color.RGBToHSV(b, out float h2, out float s2, out float v2);

            // Handle hue wrapping - take shortest path around color wheel
            float hueDiff = h2 - h1;
            if (hueDiff > 0.5f) h2 -= 1f;
            else if (hueDiff < -0.5f) h2 += 1f;

            float h = Mathf.Lerp(h1, h2, t);
            if (h < 0f) h += 1f;
            else if (h > 1f) h -= 1f;

            float s = Mathf.Lerp(s1, s2, t);
            float v = Mathf.Lerp(v1, v2, t);

            Color result = Color.HSVToRGB(h, s, v);
            result.a = Mathf.Lerp(a.a, b.a, t);
            return result;
        }

        /// <summary>
        /// Generate a 4-color palette from a base hue using color harmony rules.
        /// </summary>
        /// <param name="baseHue">Base hue in range 0-1 (maps to 0-360°).</param>
        /// <param name="mode">Color harmony mode to use.</param>
        /// <param name="saturationMultiplier">Overall saturation adjustment (1 = normal).</param>
        /// <param name="valueMultiplier">Overall value/brightness adjustment (1 = normal).</param>
        public static ColorPalette FromBaseHue(
            float baseHue,
            ColorHarmonyMode mode,
            float saturationMultiplier = 1f,
            float valueMultiplier = 1f)
        {
            baseHue = Mathf.Repeat(baseHue, 1f);

            return mode switch
            {
                ColorHarmonyMode.Analogous => GenerateAnalogous(baseHue, saturationMultiplier, valueMultiplier),
                ColorHarmonyMode.Complementary => GenerateComplementary(baseHue, saturationMultiplier, valueMultiplier),
                ColorHarmonyMode.Triadic => GenerateTriadic(baseHue, saturationMultiplier, valueMultiplier),
                ColorHarmonyMode.SplitComplementary => GenerateSplitComplementary(baseHue, saturationMultiplier, valueMultiplier),
                ColorHarmonyMode.Monochromatic => GenerateMonochromatic(baseHue, saturationMultiplier, valueMultiplier),
                _ => GenerateAnalogous(baseHue, saturationMultiplier, valueMultiplier),
            };
        }

        /// <summary>
        /// Analogous harmony: colors adjacent on the wheel (±15-30°).
        /// Creates cohesive nebulae with subtle color variation.
        /// </summary>
        private static ColorPalette GenerateAnalogous(float hue, float satMult, float valMult)
        {
            // Nebula-appropriate saturation/value curves:
            // Outer: dim and desaturated
            // Core: bright and saturated with warm shift

            return new ColorPalette
            {
                // Outer - slight cool shift, low sat/val
                Color1 = Color.HSVToRGB(
                    WrapHue(hue - 0.04f),  // -15°
                    Mathf.Clamp01(0.35f * satMult),
                    Mathf.Clamp01(0.15f * valMult)),

                // Mid-outer
                Color2 = Color.HSVToRGB(
                    WrapHue(hue),
                    Mathf.Clamp01(0.55f * satMult),
                    Mathf.Clamp01(0.35f * valMult)),

                // Mid-inner - slight warm shift
                Color3 = Color.HSVToRGB(
                    WrapHue(hue + 0.04f),  // +15°
                    Mathf.Clamp01(0.75f * satMult),
                    Mathf.Clamp01(0.65f * valMult)),

                // Core - warm shift toward yellow/orange, brightest
                Color4 = Color.HSVToRGB(
                    WrapHue(hue + 0.08f),  // +30° warm shift
                    Mathf.Clamp01(0.65f * satMult),
                    Mathf.Clamp01(0.95f * valMult)),
            };
        }

        /// <summary>
        /// Complementary harmony: opposite colors (180° apart).
        /// Creates high-contrast, dramatic nebulae.
        /// </summary>
        private static ColorPalette GenerateComplementary(float hue, float satMult, float valMult)
        {
            float complement = WrapHue(hue + 0.5f);

            return new ColorPalette
            {
                // Outer - base hue, desaturated
                Color1 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.3f * satMult),
                    Mathf.Clamp01(0.15f * valMult)),

                // Mid - base hue, more saturated
                Color2 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.6f * satMult),
                    Mathf.Clamp01(0.4f * valMult)),

                // Inner - transition toward complement
                Color3 = Color.HSVToRGB(
                    WrapHue(hue + 0.25f),  // Halfway to complement
                    Mathf.Clamp01(0.7f * satMult),
                    Mathf.Clamp01(0.7f * valMult)),

                // Core - complement, bright
                Color4 = Color.HSVToRGB(
                    complement,
                    Mathf.Clamp01(0.6f * satMult),
                    Mathf.Clamp01(0.95f * valMult)),
            };
        }

        /// <summary>
        /// Triadic harmony: three evenly spaced colors (120° apart).
        /// Creates balanced, colorful nebulae.
        /// </summary>
        private static ColorPalette GenerateTriadic(float hue, float satMult, float valMult)
        {
            float triad1 = WrapHue(hue + 0.333f);  // +120°
            float triad2 = WrapHue(hue + 0.666f);  // +240°

            return new ColorPalette
            {
                Color1 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.35f * satMult),
                    Mathf.Clamp01(0.15f * valMult)),

                Color2 = Color.HSVToRGB(
                    triad2,
                    Mathf.Clamp01(0.5f * satMult),
                    Mathf.Clamp01(0.35f * valMult)),

                Color3 = Color.HSVToRGB(
                    triad1,
                    Mathf.Clamp01(0.7f * satMult),
                    Mathf.Clamp01(0.65f * valMult)),

                Color4 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.55f * satMult),
                    Mathf.Clamp01(0.95f * valMult)),
            };
        }

        /// <summary>
        /// Split-complementary: base + two colors adjacent to complement.
        /// Combines harmony with contrast.
        /// </summary>
        private static ColorPalette GenerateSplitComplementary(float hue, float satMult, float valMult)
        {
            float splitA = WrapHue(hue + 0.416f);  // +150°
            float splitB = WrapHue(hue + 0.583f);  // +210°

            return new ColorPalette
            {
                Color1 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.35f * satMult),
                    Mathf.Clamp01(0.15f * valMult)),

                Color2 = Color.HSVToRGB(
                    splitA,
                    Mathf.Clamp01(0.5f * satMult),
                    Mathf.Clamp01(0.35f * valMult)),

                Color3 = Color.HSVToRGB(
                    splitB,
                    Mathf.Clamp01(0.7f * satMult),
                    Mathf.Clamp01(0.65f * valMult)),

                Color4 = Color.HSVToRGB(
                    WrapHue(hue + 0.05f),  // Slight warm shift
                    Mathf.Clamp01(0.6f * satMult),
                    Mathf.Clamp01(0.95f * valMult)),
            };
        }

        /// <summary>
        /// Monochromatic: single hue, varying saturation and value.
        /// Creates elegant, unified nebulae.
        /// </summary>
        private static ColorPalette GenerateMonochromatic(float hue, float satMult, float valMult)
        {
            return new ColorPalette
            {
                Color1 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.2f * satMult),
                    Mathf.Clamp01(0.12f * valMult)),

                Color2 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.45f * satMult),
                    Mathf.Clamp01(0.3f * valMult)),

                Color3 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.7f * satMult),
                    Mathf.Clamp01(0.6f * valMult)),

                Color4 = Color.HSVToRGB(
                    hue,
                    Mathf.Clamp01(0.5f * satMult),
                    Mathf.Clamp01(0.95f * valMult)),
            };
        }

        /// <summary>
        /// Apply zone-based modifications to the palette.
        /// </summary>
        /// <param name="nebulaDensity">0-1, increases saturation in nebula regions.</param>
        /// <param name="voidFactor">0-1, desaturates and cools colors in void regions.</param>
        /// <param name="anomalyStrength">0-1, shifts toward red and increases saturation.</param>
        public ColorPalette ApplyZoneModifiers(float nebulaDensity, float voidFactor, float anomalyStrength)
        {
            var result = this;

            // Void factor: desaturate and shift toward cool blue
            if (voidFactor > 0.01f)
            {
                float voidInfluence = voidFactor * 0.7f;
                Color voidTint = new Color(0.15f, 0.15f, 0.25f, 1f);

                result.Color1 = Color.Lerp(result.Color1, DesaturateAndDarken(result.Color1, voidInfluence), voidInfluence);
                result.Color2 = Color.Lerp(result.Color2, DesaturateAndDarken(result.Color2, voidInfluence), voidInfluence);
                result.Color3 = Color.Lerp(result.Color3, DesaturateAndDarken(result.Color3, voidInfluence), voidInfluence);
                result.Color4 = Color.Lerp(result.Color4, voidTint, voidInfluence * 0.3f);
            }

            // Nebula density: boost saturation
            if (nebulaDensity > 0.01f)
            {
                float satBoost = nebulaDensity * 0.3f;
                result.Color1 = AdjustSaturation(result.Color1, 1f + satBoost * 0.5f);
                result.Color2 = AdjustSaturation(result.Color2, 1f + satBoost);
                result.Color3 = AdjustSaturation(result.Color3, 1f + satBoost);
                result.Color4 = AdjustSaturation(result.Color4, 1f + satBoost * 0.5f);
            }

            // Anomaly: shift toward red, increase saturation and value
            if (anomalyStrength > 0.01f)
            {
                float anomalyInfluence = anomalyStrength * 0.5f;
                Color anomalyTint = new Color(1f, 0.3f, 0.2f, 1f);

                result.Color1 = ShiftHueToward(result.Color1, 0f, anomalyInfluence * 0.3f); // Toward red
                result.Color2 = ShiftHueToward(result.Color2, 0f, anomalyInfluence * 0.4f);
                result.Color3 = Color.Lerp(result.Color3, anomalyTint, anomalyInfluence * 0.3f);
                result.Color4 = Color.Lerp(result.Color4, new Color(1f, 0.6f, 0.3f), anomalyInfluence * 0.4f);
            }

            return result;
        }

        private static float WrapHue(float h)
        {
            h = h % 1f;
            if (h < 0f) h += 1f;
            return h;
        }

        private static Color DesaturateAndDarken(Color c, float amount)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * (1f - amount));
            v = Mathf.Clamp01(v * (1f - amount * 0.5f));
            return Color.HSVToRGB(h, s, v);
        }

        private static Color AdjustSaturation(Color c, float multiplier)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);
            s = Mathf.Clamp01(s * multiplier);
            return Color.HSVToRGB(h, s, v);
        }

        private static Color ShiftHueToward(Color c, float targetHue, float amount)
        {
            Color.RGBToHSV(c, out float h, out float s, out float v);

            // Take shortest path
            float diff = targetHue - h;
            if (diff > 0.5f) diff -= 1f;
            else if (diff < -0.5f) diff += 1f;

            h = WrapHue(h + diff * amount);
            return Color.HSVToRGB(h, s, v);
        }

        public bool Equals(ColorPalette other)
        {
            return Color1.Equals(other.Color1) &&
                   Color2.Equals(other.Color2) &&
                   Color3.Equals(other.Color3) &&
                   Color4.Equals(other.Color4);
        }

        public override bool Equals(object obj) => obj is ColorPalette other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Color1, Color2, Color3, Color4);

        public static bool operator ==(ColorPalette left, ColorPalette right) => left.Equals(right);
        public static bool operator !=(ColorPalette left, ColorPalette right) => !left.Equals(right);
    }
}
