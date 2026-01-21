Shader "Starfire/Nebula"
{
    Properties
    {
        [Header(Noise Configuration)]
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 1.0
        [IntRange] _Octaves ("FBM Octaves", Range(1, 8)) = 5
        _Persistence ("Persistence", Range(0.3, 0.7)) = 0.5
        _Lacunarity ("Lacunarity", Range(1.5, 3.0)) = 2.0

        [Header(Domain Warping)]
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

        [Header(Density and Shape)]
        _Density ("Overall Density", Range(0, 2)) = 1.0
        _EdgeSoftness ("Edge Softness", Range(0.1, 2)) = 0.5
        _Threshold ("Visibility Threshold", Range(0, 0.5)) = 0.1
        _DetailFrequency ("Detail Frequency", Range(0.5, 4)) = 2.0

        [Header(Parallax)]
        _ParallaxFactor ("Parallax Factor", Float) = 0.02

        [Header(Background)]
        [Toggle] _RenderBackground ("Render Background", Float) = 0
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)

        [Header(Seed)]
        _Seed ("Random Seed", Float) = 0

        [Header(Region Masking)]
        _RegionCenter ("Region Center (World XY)", Vector) = (0, 0, 0, 0)
        _RegionRadius ("Region Radius", Float) = 10000
        _RegionFalloff ("Falloff Distance", Float) = 50
        _RegionEdgeMode ("Edge Mode (0=Smooth, 1=Sharp, 2=Inverse)", Float) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Background+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Nebula"

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
                float _NoiseScale;
                int _Octaves;
                float _Persistence;
                float _Lacunarity;
                float _WarpStrength;
                float _WarpScale;
                int _ColorCount;
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float4 _Color4;
                float _GradientBias;
                float _GradientContrast;
                float _EmissionIntensity;
                float _CoreEmissionBoost;
                float _Density;
                float _EdgeSoftness;
                float _Threshold;
                float _DetailFrequency;
                float _ParallaxFactor;
                float _RenderBackground;
                float4 _BackgroundColor;
                float _Seed;
                float4 _RegionCenter;
                float _RegionRadius;
                float _RegionFalloff;
                float _RegionEdgeMode;
            CBUFFER_END

            // Global camera properties (set by StarfieldManager)
            float2 _CameraWorldPos;
            float _ScreenAspect;
            float _CameraOrthoSize;
            float _ReferenceZoom;

            // Warp effect globals (set by WarpEffectController)
            float _WarpIntensity;
            float _WarpNebulaStretch;
            float _WarpNebulaFade;
            float2 _WarpDirection;

            // ============================================
            // Hash Functions (PCG-style)
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
            // 2D Perlin Noise with Quintic Interpolation
            // ============================================

            float perlin2D(float2 p, float seed)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // Quintic interpolation for smoother results
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
            // FBM (Fractal Brownian Motion)
            // ============================================

            float fbmNebula(float2 coord, int octaves, float persistence, float lacunarity, float seed)
            {
                float value = 0.0;
                float amplitude = 1.0;
                float frequency = 1.0;
                float maxValue = 0.0;

                int maxOctaves = min(octaves, 8);

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
            // Domain Warping for Organic Look
            // ============================================

            float2 domainWarp(float2 coord, float warpStrength, float warpScale, float seed)
            {
                // First layer of warping
                float2 warp1 = float2(
                    perlin2D(coord * warpScale, seed),
                    perlin2D(coord * warpScale + 100.0, seed)
                );

                // Second layer for more complexity
                float2 warp2 = float2(
                    perlin2D((coord + warp1 * warpStrength) * warpScale * 2.0, seed + 50.0),
                    perlin2D((coord + warp1 * warpStrength) * warpScale * 2.0 + 100.0, seed + 50.0)
                );

                return coord + (warp1 + warp2 * 0.5) * warpStrength;
            }

            // ============================================
            // Nebula Field Generation
            // ============================================

            float nebulaField(float2 uv, float noiseScale, int octaves, float persistence,
                              float lacunarity, float warpStrength, float warpScale,
                              float detailFreq, float seed)
            {
                // Apply domain warping for organic shapes
                float2 warpedUV = domainWarp(uv * noiseScale, warpStrength, warpScale, seed);

                // Base nebula shape
                float baseNoise = fbmNebula(warpedUV, octaves, persistence, lacunarity, seed);

                // Add fine detail layer
                int detailOctaves = max(octaves - 2, 2);
                float detail = fbmNebula(warpedUV * detailFreq, detailOctaves,
                                         persistence * 0.8, lacunarity, seed + 200.0);

                // Combine: base shape modulated by detail
                float combined = baseNoise * (0.7 + detail * 0.3);

                return combined;
            }

            // ============================================
            // Color Gradient Sampling
            // ============================================

            float3 sampleNebulaGradient(float t, int colorCount, float4 c1, float4 c2, float4 c3, float4 c4)
            {
                t = saturate(t);

                // 2-color gradient
                if (colorCount <= 2)
                {
                    return lerp(c1.rgb, c2.rgb, t);
                }

                // 3-color gradient
                if (colorCount == 3)
                {
                    if (t < 0.5)
                        return lerp(c1.rgb, c2.rgb, t * 2.0);
                    else
                        return lerp(c2.rgb, c3.rgb, (t - 0.5) * 2.0);
                }

                // 4-color gradient
                if (t < 0.333)
                    return lerp(c1.rgb, c2.rgb, t * 3.0);
                else if (t < 0.666)
                    return lerp(c2.rgb, c3.rgb, (t - 0.333) * 3.0);
                else
                    return lerp(c3.rgb, c4.rgb, (t - 0.666) * 3.0);
            }

            // ============================================
            // Apply Nebula Coloring
            // ============================================

            float3 applyNebulaColor(float noiseValue, float density, float threshold,
                                    float edgeSoftness, float gradientBias, float gradientContrast,
                                    float emissionIntensity, float coreBoost,
                                    int colorCount, float4 c1, float4 c2, float4 c3, float4 c4)
            {
                // Apply density scaling
                float scaled = noiseValue * density;

                // Threshold with soft edge
                float visible = smoothstep(threshold, threshold + edgeSoftness, scaled);

                // Apply bias and contrast for gradient mapping
                float normalizedValue = saturate((scaled - threshold) / max(1.0 - threshold, 0.001));
                float gradientT = pow(normalizedValue, gradientBias);
                gradientT = saturate((gradientT - 0.5) * gradientContrast + 0.5);

                // Sample color from gradient
                float3 baseColor = sampleNebulaGradient(gradientT, colorCount, c1, c2, c3, c4);

                // Apply emission (brighter in denser regions)
                float emission = lerp(emissionIntensity, emissionIntensity * coreBoost, gradientT);

                return baseColor * visible * emission;
            }

            // ============================================
            // Region Masking
            // ============================================

            float calculateRegionMask(float2 worldPos, float2 regionCenter, float radius, float falloff, float edgeMode)
            {
                float distToCenter = distance(worldPos, regionCenter);
                float regionMask = 1.0;

                if (edgeMode < 0.5) // Smooth falloff
                {
                    regionMask = 1.0 - smoothstep(radius - falloff, radius, distToCenter);
                }
                else if (edgeMode < 1.5) // Sharp boundary
                {
                    regionMask = 1.0 - step(radius, distToCenter);
                }
                else // Inverse (clear zone - nebula outside, clear inside)
                {
                    regionMask = smoothstep(radius, radius + falloff, distToCenter);
                }

                return regionMask;
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

                // Calculate zoom factor
                float zoomFactor = _CameraOrthoSize / max(_ReferenceZoom, 0.001);

                // Depth-aware zoom: distant layers (low parallax) zoom less, nearby layers zoom more
                float depthZoomFactor = lerp(1.0, zoomFactor, saturate(_ParallaxFactor * 10.0));

                // Scale UVs around center
                float2 scaledUV = (uv - 0.5) * depthZoomFactor + 0.5;

                // Apply parallax offset
                float2 parallaxOffset = _CameraWorldPos * _ParallaxFactor;
                float2 parallaxUV = scaledUV + parallaxOffset;

                // Aspect ratio correction
                float2 aspectCorrectedUV = float2(parallaxUV.x * _ScreenAspect, parallaxUV.y);

                // Apply warp stretching (subtle for nebulae)
                [branch] if (_WarpIntensity > 0.001 && _WarpNebulaStretch > 0.001)
                {
                    float parallel = dot(aspectCorrectedUV, _WarpDirection);
                    float2 perp = aspectCorrectedUV - _WarpDirection * parallel;
                    parallel /= _WarpNebulaStretch;
                    aspectCorrectedUV = perp + _WarpDirection * parallel;
                }

                // Calculate world position for region masking
                // Convert screen UV to world position based on camera parameters
                float2 worldPos = _CameraWorldPos + (uv - 0.5) * _CameraOrthoSize * 2.0 * float2(_ScreenAspect, 1.0);

                // Calculate region mask
                float regionMask = calculateRegionMask(worldPos, _RegionCenter.xy, _RegionRadius, _RegionFalloff, _RegionEdgeMode);

                // Apply region mask to density
                float maskedDensity = _Density * regionMask;

                // Generate nebula field
                float noiseValue = nebulaField(
                    aspectCorrectedUV,
                    _NoiseScale,
                    _Octaves,
                    _Persistence,
                    _Lacunarity,
                    _WarpStrength,
                    _WarpScale,
                    _DetailFrequency,
                    _Seed
                );

                // Apply coloring with region-masked density
                float3 nebulaColor = applyNebulaColor(
                    noiseValue, maskedDensity, _Threshold, _EdgeSoftness,
                    _GradientBias, _GradientContrast,
                    _EmissionIntensity, _CoreEmissionBoost,
                    _ColorCount, _Color1, _Color2, _Color3, _Color4
                );

                // Apply warp fade
                nebulaColor *= lerp(1.0, 1.0 - _WarpNebulaFade, _WarpIntensity);

                // Calculate alpha from luminance
                float alpha = saturate(dot(nebulaColor, float3(0.299, 0.587, 0.114)));

                // Background handling
                if (_RenderBackground > 0.5)
                {
                    float3 finalColor = _BackgroundColor.rgb + nebulaColor;
                    return half4(finalColor, 1.0);
                }
                else
                {
                    // Premultiplied alpha for proper blending
                    return half4(nebulaColor, alpha);
                }
            }

            ENDHLSL
        }
    }
}
