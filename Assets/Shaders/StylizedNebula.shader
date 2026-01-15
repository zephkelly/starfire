Shader "Starfire/StylizedNebula"
{
    Properties
    {
        [Header(Style Features)]
        [Toggle] _EnablePillars ("Enable Cosmic Pillars", Float) = 0
        [Toggle] _EnablePainterly ("Enable Painterly Style", Float) = 0
        [Toggle] _EnableTendrils ("Enable Wispy Tendrils", Float) = 0
        [Toggle] _EnableCellular ("Enable Cellular/Organic", Float) = 0

        [Header(Pillar Settings)]
        _PillarStretch ("Pillar Stretch", Range(1, 5)) = 2.5
        _PillarAngle ("Pillar Angle", Range(-180, 180)) = 0
        _PillarWarpBias ("Pillar Warp Bias", Range(0, 2)) = 0.5

        [Header(Painterly Settings)]
        [IntRange] _PosterizeLevels ("Posterize Levels", Range(3, 12)) = 6
        _BrushStrokeScale ("Brush Stroke Scale", Range(0.5, 5)) = 2
        _BrushWarpAmount ("Brush Warp Amount", Range(0, 1)) = 0.3

        [Header(Tendril Settings)]
        _CurlStrength ("Curl Strength", Range(0, 2)) = 0.5
        _CurlScale ("Curl Scale", Range(0.5, 4)) = 1.5
        _TendrilLength ("Tendril Length", Range(0.1, 2)) = 0.8

        [Header(Cellular Settings)]
        _VoronoiScale ("Voronoi Scale", Range(2, 20)) = 8
        _CellEdgeWidth ("Cell Edge Width", Range(0.01, 0.5)) = 0.15
        [Toggle] _BubbleInvert ("Bubble Invert", Float) = 0

        [Header(Internal Structure)]
        [Toggle] _EnableDenseCores ("Enable Dense Cores", Float) = 0
        _CoreIntensity ("Core Intensity", Range(0, 2)) = 1
        _HaloSize ("Halo Size", Range(0, 0.5)) = 0.2

        [Toggle] _EnableEdgeLit ("Enable Edge Lighting", Float) = 0
        _RimLightStrength ("Rim Light Strength", Range(0, 2)) = 0.5
        _RimLightColor ("Rim Light Color", Color) = (1, 0.9, 0.7, 1)

        [Toggle] _EnableDepthBands ("Enable Depth Bands", Float) = 0
        [IntRange] _BandCount ("Band Count", Range(2, 8)) = 4
        _BandContrast ("Band Contrast", Range(0, 1)) = 0.5

        [Toggle] _EnableBrightSpots ("Enable Bright Spots", Float) = 0
        _SpotDensity ("Spot Density", Range(5, 50)) = 20
        _SpotIntensity ("Spot Intensity", Range(0, 3)) = 1.5

        [Header(Edge Style)]
        [IntRange] _EdgeMode ("Edge Mode (0=Soft, 1=Sharp, 2=Mixed)", Range(0, 2)) = 2
        _SilhouetteSharpness ("Silhouette Sharpness", Range(0, 1)) = 0.7
        _InternalSoftness ("Internal Softness", Range(0.1, 1)) = 0.5

        [Header(Base Noise)]
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1.0
        [IntRange] _Octaves ("FBM Octaves", Range(1, 6)) = 4
        _Persistence ("Persistence", Range(0.3, 0.7)) = 0.5
        _Lacunarity ("Lacunarity", Range(1.5, 3.0)) = 2.0
        _WarpStrength ("Warp Strength", Range(0, 2)) = 0.5
        _WarpScale ("Warp Scale", Range(0.1, 5)) = 0.5

        [Header(Color Gradient)]
        [IntRange] _ColorCount ("Active Color Count", Range(2, 4)) = 3
        _Color1 ("Color 1 (Outer)", Color) = (0.1, 0.05, 0.2, 1)
        _Color2 ("Color 2", Color) = (0.4, 0.1, 0.3, 1)
        _Color3 ("Color 3", Color) = (0.8, 0.3, 0.4, 1)
        _Color4 ("Color 4 (Inner/Bright)", Color) = (1, 0.8, 0.6, 1)
        _GradientBias ("Gradient Bias", Range(0.1, 3)) = 1.0
        _GradientContrast ("Gradient Contrast", Range(0.5, 3)) = 1.5

        [Header(Emission)]
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 1.0
        _CoreEmissionBoost ("Core Emission Boost", Range(1, 5)) = 2.0

        [Header(Density)]
        _Density ("Overall Density", Range(0, 2)) = 1.0
        _Threshold ("Visibility Threshold", Range(0, 0.5)) = 0.1

        [Header(Parallax)]
        _ParallaxFactor ("Parallax Factor", Float) = 0.02

        [Header(Background)]
        [Toggle] _RenderBackground ("Render Background", Float) = 0
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)

        [Header(Seed)]
        _Seed ("Random Seed", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Background+2"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StylizedNebula"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            CBUFFER_START(UnityPerMaterial)
                // Style Features
                float _EnablePillars;
                float _EnablePainterly;
                float _EnableTendrils;
                float _EnableCellular;

                // Pillar Settings
                float _PillarStretch;
                float _PillarAngle;
                float _PillarWarpBias;

                // Painterly Settings
                int _PosterizeLevels;
                float _BrushStrokeScale;
                float _BrushWarpAmount;

                // Tendril Settings
                float _CurlStrength;
                float _CurlScale;
                float _TendrilLength;

                // Cellular Settings
                float _VoronoiScale;
                float _CellEdgeWidth;
                float _BubbleInvert;

                // Internal Structure
                float _EnableDenseCores;
                float _CoreIntensity;
                float _HaloSize;
                float _EnableEdgeLit;
                float _RimLightStrength;
                float4 _RimLightColor;
                float _EnableDepthBands;
                int _BandCount;
                float _BandContrast;
                float _EnableBrightSpots;
                float _SpotDensity;
                float _SpotIntensity;

                // Edge Style
                int _EdgeMode;
                float _SilhouetteSharpness;
                float _InternalSoftness;

                // Base Noise
                float _NoiseScale;
                int _Octaves;
                float _Persistence;
                float _Lacunarity;
                float _WarpStrength;
                float _WarpScale;

                // Color Gradient
                int _ColorCount;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float _GradientBias;
                float _GradientContrast;

                // Emission
                float _EmissionIntensity;
                float _CoreEmissionBoost;

                // Density
                float _Density;
                float _Threshold;

                // Parallax
                float _ParallaxFactor;

                // Background
                float _RenderBackground;
                float4 _BackgroundColor;

                // Seed
                float _Seed;
            CBUFFER_END

            // Global camera properties
            float2 _CameraWorldPos;
            float _ScreenAspect;
            float _CameraOrthoSize;
            float _ReferenceZoom;

            // ============================================
            // Hash Functions
            // ============================================

            float hash1(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float2 hash2(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy);
            }

            // ============================================
            // Gradient for Perlin Noise
            // ============================================

            float2 grad2(float2 p, float seed)
            {
                float angle = hash1(p + seed) * 6.28318530718;
                return float2(cos(angle), sin(angle));
            }

            // ============================================
            // 2D Perlin Noise
            // ============================================

            float perlin2D(float2 p, float seed)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float2 g00 = grad2(i + float2(0.0, 0.0), seed);
                float2 g10 = grad2(i + float2(1.0, 0.0), seed);
                float2 g01 = grad2(i + float2(0.0, 1.0), seed);
                float2 g11 = grad2(i + float2(1.0, 1.0), seed);

                float n00 = dot(g00, f - float2(0.0, 0.0));
                float n10 = dot(g10, f - float2(1.0, 0.0));
                float n01 = dot(g01, f - float2(0.0, 1.0));
                float n11 = dot(g11, f - float2(1.0, 1.0));

                float nx0 = lerp(n00, n10, u.x);
                float nx1 = lerp(n01, n11, u.x);
                return lerp(nx0, nx1, u.y) * 0.5 + 0.5;
            }

            // ============================================
            // Standard FBM
            // ============================================

            float fbmNebula(float2 coord, int octaves, float persistence, float lacunarity, float seed)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                float maxValue = 0.0;

                int maxOctaves = min(octaves, 6);

                [loop]
                for (int i = 0; i < maxOctaves; i++)
                {
                    value += perlin2D(coord * frequency, seed + float(i) * 100.0) * amplitude;
                    maxValue += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                return value / maxValue;
            }

            // ============================================
            // Ridged Noise (for Pillars)
            // ============================================

            float ridgedNoise(float2 p, float seed)
            {
                float n = perlin2D(p, seed);
                return 1.0 - abs(n - 0.5) * 2.0;
            }

            float ridgedFBM(float2 coord, int octaves, float persistence, float lacunarity, float seed)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                float maxValue = 0.0;
                float weight = 1.0;

                int maxOctaves = min(octaves, 6);

                [loop]
                for (int i = 0; i < maxOctaves; i++)
                {
                    float signal = ridgedNoise(coord * frequency, seed + float(i) * 100.0);
                    signal = signal * signal;
                    signal *= weight;
                    weight = saturate(signal * 2.0);

                    value += signal * amplitude;
                    maxValue += amplitude;
                    amplitude *= persistence;
                    frequency *= lacunarity;
                }

                return value / maxValue;
            }

            // ============================================
            // Voronoi Noise (for Cellular)
            // ============================================

            float2 voronoi(float2 p, out float edgeDist)
            {
                float2 n = floor(p);
                float2 f = frac(p);

                float minDist = 8.0;
                float secondMinDist = 8.0;

                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 cellPoint = hash2(n + neighbor);
                        float2 diff = neighbor + cellPoint - f;
                        float dist = dot(diff, diff);

                        if (dist < minDist)
                        {
                            secondMinDist = minDist;
                            minDist = dist;
                        }
                        else if (dist < secondMinDist)
                        {
                            secondMinDist = dist;
                        }
                    }
                }

                edgeDist = secondMinDist - minDist;
                return float2(sqrt(minDist), sqrt(secondMinDist));
            }

            // ============================================
            // Curl Noise (for Tendrils)
            // ============================================

            float2 curlNoise(float2 p, float seed, float epsilon)
            {
                float n = perlin2D(p, seed);
                float nx = perlin2D(p + float2(epsilon, 0), seed);
                float ny = perlin2D(p + float2(0, epsilon), seed);

                float2 gradient = float2(nx - n, ny - n) / epsilon;
                return float2(-gradient.y, gradient.x);
            }

            // ============================================
            // Domain Warping
            // ============================================

            float2 domainWarp(float2 coord, float warpStrength, float warpScale, float seed)
            {
                float2 warp1 = float2(
                    perlin2D(coord * warpScale, seed),
                    perlin2D(coord * warpScale + 100.0, seed)
                );

                float2 warp2 = float2(
                    perlin2D((coord + warp1 * warpStrength) * warpScale * 2.0, seed + 50.0),
                    perlin2D((coord + warp1 * warpStrength) * warpScale * 2.0 + 100.0, seed + 50.0)
                );

                return coord + (warp1 + warp2 * 0.5) * warpStrength;
            }

            // ============================================
            // Pillar Warp (Anisotropic)
            // ============================================

            float2 pillarWarp(float2 uv, float stretch, float angle, float warpBias, float seed)
            {
                float rad = angle * 0.0174533;
                float cosA = cos(rad);
                float sinA = sin(rad);
                float2 rotatedUV = float2(
                    uv.x * cosA - uv.y * sinA,
                    uv.x * sinA + uv.y * cosA
                );

                rotatedUV.y *= stretch;

                float2 warp = float2(
                    perlin2D(rotatedUV * 0.5, seed) * warpBias,
                    perlin2D(rotatedUV * 0.5 + 100.0, seed) * warpBias * 0.3
                );

                return rotatedUV + warp;
            }

            // ============================================
            // Brush Stroke Warp (for Painterly)
            // ============================================

            float2 brushStrokeWarp(float2 uv, float brushScale, float brushAmount, float seed)
            {
                float angle = perlin2D(uv * brushScale * 0.5, seed) * 6.28;
                float2 strokeDir = float2(cos(angle), sin(angle));
                float strokeNoise = perlin2D(uv * brushScale, seed + 50.0);
                return uv + strokeDir * strokeNoise * brushAmount;
            }

            // ============================================
            // Tendril Warp (Curl-based)
            // ============================================

            float2 tendrilWarp(float2 uv, float curlStrength, float curlScale, float tendrilLength, float seed)
            {
                float2 offset = float2(0, 0);
                float2 pos = uv * curlScale;

                int steps = 6;
                float stepSize = tendrilLength / float(steps);

                [unroll(6)]
                for (int i = 0; i < steps; i++)
                {
                    float2 curl = curlNoise(pos, seed, 0.01);
                    offset += curl * stepSize;
                    pos += curl * 0.1;
                }

                return uv + offset * curlStrength;
            }

            // ============================================
            // Posterization (for Painterly)
            // ============================================

            float posterize(float value, int levels)
            {
                return floor(value * levels + 0.5) / levels;
            }

            // ============================================
            // Cellular Field
            // ============================================

            float cellularField(float2 uv, float voronoiScale, float cellEdgeWidth, bool bubbleInvert, float seed)
            {
                float edgeDist;
                float2 voronoiDist = voronoi(uv * voronoiScale + hash2(float2(seed, seed * 1.7)) * 100.0, edgeDist);

                float cellValue = voronoiDist.x;

                if (bubbleInvert)
                {
                    cellValue = 1.0 - cellValue;
                }

                float edge = 1.0 - smoothstep(0.0, cellEdgeWidth, edgeDist);
                return lerp(cellValue, cellValue + edge * 0.5, 0.5);
            }

            // ============================================
            // Dense Cores with Halos
            // ============================================

            float applyDenseCore(float noiseValue, float coreIntensity, float haloSize)
            {
                float coreThreshold = 0.7;
                float core = smoothstep(coreThreshold, 1.0, noiseValue);
                float haloThreshold = coreThreshold - haloSize;
                float halo = smoothstep(haloThreshold, coreThreshold, noiseValue);

                float boosted = noiseValue + core * coreIntensity;
                boosted += halo * coreIntensity * 0.3;

                return saturate(boosted);
            }

            // ============================================
            // Layered Depth Bands
            // ============================================

            float layeredDepthBands(float noiseValue, int bandCount, float bandContrast)
            {
                float bandedValue = floor(noiseValue * bandCount) / bandCount;
                float bandFrac = frac(noiseValue * bandCount);
                float softEdge = smoothstep(0.0, 0.3, bandFrac) * smoothstep(1.0, 0.7, bandFrac);

                return lerp(noiseValue, bandedValue + softEdge * (1.0 / bandCount), bandContrast);
            }

            // ============================================
            // Bright Spots
            // ============================================

            float brightSpots(float2 uv, float spotDensity, float spotIntensity, float seed)
            {
                float2 gridUV = uv * spotDensity;
                float2 cellID = floor(gridUV);
                float2 cellUV = frac(gridUV);

                float spots = 0.0;

                float spawnChance = hash1(cellID + seed);
                if (spawnChance > 0.85)
                {
                    float2 spotPos = hash2(cellID + seed * 2.0);
                    float2 toSpot = cellUV - spotPos;
                    float dist = length(toSpot);

                    float spotSize = 0.08 + hash1(cellID + seed * 3.0) * 0.12;
                    float spot = 1.0 - smoothstep(0.0, spotSize, dist);
                    spots = spot * spot * spotIntensity;
                }

                return spots;
            }

            // ============================================
            // Edge Style Application
            // ============================================

            float applyEdgeStyle(float noiseValue, float threshold, float silhouetteSharpness,
                                  float internalSoftness, int edgeMode)
            {
                if (edgeMode == 0) // Soft
                {
                    return smoothstep(threshold - internalSoftness * 0.5, threshold + internalSoftness, noiseValue);
                }
                else if (edgeMode == 1) // Sharp
                {
                    float sharpEdge = (1.0 - silhouetteSharpness) * 0.1 + 0.01;
                    return smoothstep(threshold - sharpEdge, threshold + sharpEdge, noiseValue);
                }
                else // Mixed
                {
                    float sharpEdge = (1.0 - silhouetteSharpness) * 0.05 + 0.01;
                    float silhouette = smoothstep(threshold - sharpEdge, threshold + sharpEdge, noiseValue);
                    float internalT = saturate((noiseValue - threshold) / max(1.0 - threshold, 0.001));
                    float softInternal = smoothstep(0.0, internalSoftness, internalT);
                    return silhouette * softInternal;
                }
            }

            // ============================================
            // Color Gradient Sampling
            // ============================================

            float3 sampleNebulaGradient(float t, int colorCount, float4 c1, float4 c2, float4 c3, float4 c4)
            {
                t = saturate(t);

                if (colorCount <= 2)
                {
                    return lerp(c1.rgb, c2.rgb, t);
                }

                if (colorCount == 3)
                {
                    if (t < 0.5)
                        return lerp(c1.rgb, c2.rgb, t * 2.0);
                    else
                        return lerp(c2.rgb, c3.rgb, (t - 0.5) * 2.0);
                }

                if (t < 0.333)
                    return lerp(c1.rgb, c2.rgb, t * 3.0);
                else if (t < 0.666)
                    return lerp(c2.rgb, c3.rgb, (t - 0.333) * 3.0);
                else
                    return lerp(c3.rgb, c4.rgb, (t - 0.666) * 3.0);
            }

            // ============================================
            // Noise Gradient for Edge Lighting
            // ============================================

            float3 calculateNoiseNormal(float2 uv, float noiseValue, float seed, float epsilon)
            {
                float right = perlin2D(uv + float2(epsilon, 0), seed);
                float up = perlin2D(uv + float2(0, epsilon), seed);

                float3 normal = normalize(float3(
                    (noiseValue - right) * 5.0,
                    (noiseValue - up) * 5.0,
                    1.0
                ));

                return normal;
            }

            // ============================================
            // Vertex Shader
            // ============================================

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            // ============================================
            // Fragment Shader
            // ============================================

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // === Parallax ===
                float zoomFactor = _CameraOrthoSize / max(_ReferenceZoom, 0.001);
                float depthZoomFactor = lerp(1.0, zoomFactor, saturate(_ParallaxFactor * 10.0));
                float2 scaledUV = (uv - 0.5) * depthZoomFactor + 0.5;
                float2 parallaxOffset = _CameraWorldPos * _ParallaxFactor;
                float2 parallaxUV = scaledUV + parallaxOffset;
                float2 aspectCorrectedUV = float2(parallaxUV.x * _ScreenAspect, parallaxUV.y);

                // === Style-Specific UV Warping ===
                float2 styledUV = aspectCorrectedUV;

                [branch] if (_EnablePillars > 0.5)
                {
                    styledUV = pillarWarp(styledUV, _PillarStretch, _PillarAngle, _PillarWarpBias, _Seed);
                }

                [branch] if (_EnablePainterly > 0.5)
                {
                    styledUV = brushStrokeWarp(styledUV, _BrushStrokeScale, _BrushWarpAmount, _Seed);
                }

                [branch] if (_EnableTendrils > 0.5)
                {
                    styledUV = tendrilWarp(styledUV, _CurlStrength, _CurlScale, _TendrilLength, _Seed);
                }

                // === Generate Base Noise ===
                float baseNoise;

                [branch] if (_EnablePillars > 0.5)
                {
                    baseNoise = ridgedFBM(styledUV * _NoiseScale, _Octaves, _Persistence, _Lacunarity, _Seed);
                }
                else
                {
                    float2 warpedUV = domainWarp(styledUV * _NoiseScale, _WarpStrength, _WarpScale, _Seed);
                    baseNoise = fbmNebula(warpedUV, _Octaves, _Persistence, _Lacunarity, _Seed);
                }

                // === Blend Cellular if enabled ===
                [branch] if (_EnableCellular > 0.5)
                {
                    float cellular = cellularField(aspectCorrectedUV, _VoronoiScale, _CellEdgeWidth,
                                                   _BubbleInvert > 0.5, _Seed);
                    baseNoise = lerp(baseNoise, baseNoise * cellular, 0.6);
                }

                // === Apply Painterly Posterization ===
                [branch] if (_EnablePainterly > 0.5)
                {
                    float brushVar = perlin2D(styledUV * _BrushStrokeScale * 2.0, _Seed + 100.0);
                    float posterized = posterize(baseNoise, _PosterizeLevels);
                    baseNoise = lerp(posterized, posterized * (0.85 + brushVar * 0.3), 0.5);
                }

                // Apply density
                float scaledNoise = baseNoise * _Density;

                // === Internal Structure Features ===
                float structuredNoise = scaledNoise;

                [branch] if (_EnableDenseCores > 0.5)
                {
                    structuredNoise = applyDenseCore(structuredNoise, _CoreIntensity, _HaloSize);
                }

                [branch] if (_EnableDepthBands > 0.5)
                {
                    structuredNoise = layeredDepthBands(structuredNoise, _BandCount, _BandContrast);
                }

                // === Edge Styling ===
                float visible = applyEdgeStyle(structuredNoise, _Threshold, _SilhouetteSharpness,
                                               _InternalSoftness, _EdgeMode);

                // === Color Gradient ===
                float normalizedValue = saturate((structuredNoise - _Threshold) / max(1.0 - _Threshold, 0.001));
                float gradientT = pow(normalizedValue, _GradientBias);
                gradientT = saturate((gradientT - 0.5) * _GradientContrast + 0.5);
                float3 baseColor = sampleNebulaGradient(gradientT, _ColorCount, _Color1, _Color2, _Color3, _Color4);

                // === Edge Lighting ===
                float rimEffect = 0;
                [branch] if (_EnableEdgeLit > 0.5)
                {
                    float3 normal = calculateNoiseNormal(styledUV * _NoiseScale, baseNoise, _Seed, 0.002);
                    float rim = 1.0 - saturate(abs(normal.z));
                    rim = pow(rim, 2.0);

                    float2 lightDir = normalize(float2(1.0, 0.5));
                    float directionalRim = saturate(dot(normal.xy, lightDir) * 0.5 + 0.5);
                    rimEffect = rim * _RimLightStrength * directionalRim;
                }

                // === Bright Spots ===
                float spots = 0;
                [branch] if (_EnableBrightSpots > 0.5)
                {
                    spots = brightSpots(aspectCorrectedUV, _SpotDensity, _SpotIntensity, _Seed);
                    spots *= visible; // Only show spots within nebula
                }

                // === Final Emission ===
                float emission = lerp(_EmissionIntensity, _EmissionIntensity * _CoreEmissionBoost, gradientT);
                float3 nebulaColor = baseColor * visible * emission;

                // Add rim lighting
                nebulaColor += _RimLightColor.rgb * rimEffect * visible;

                // Add bright spots
                nebulaColor += spots * _Color4.rgb;

                // === Output ===
                float alpha = saturate(dot(nebulaColor, float3(0.299, 0.587, 0.114)));

                if (_RenderBackground > 0.5)
                {
                    return half4(_BackgroundColor.rgb + nebulaColor, 1.0);
                }
                else
                {
                    return half4(nebulaColor, alpha);
                }
            }

            ENDHLSL
        }
    }
}
