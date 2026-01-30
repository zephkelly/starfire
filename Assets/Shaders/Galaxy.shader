Shader "Starfire/Galaxy"
{
    Properties
    {
        [Header(Galaxy Placement)]
        _GalaxyCellSize ("Cell Size (larger = more sparse)", Range(1, 20)) = 8.0
        _GalaxySpawnChance ("Spawn Chance", Range(0, 1)) = 0.3
        _GalaxySizeMin ("Galaxy Size Min", Range(0.05, 2)) = 0.3
        _GalaxySizeMax ("Galaxy Size Max", Range(0.1, 4)) = 1.0

        [Header(Spiral Structure)]
        _ArmCount ("Arm Count", Range(1, 6)) = 2
        _ArmWinding ("Arm Winding Tightness", Range(0.1, 3)) = 0.8
        _ArmSpread ("Arm Width", Range(0.05, 1)) = 0.3
        _ArmFalloff ("Arm Density Falloff", Range(0.5, 5)) = 2.0

        [Header(Core)]
        _CoreSize ("Core Size", Range(0.01, 0.5)) = 0.12
        _CoreBrightness ("Core Brightness", Range(0.5, 5)) = 2.5
        _CoreFalloff ("Core Falloff", Range(1, 8)) = 3.0

        [Header(Star Population)]
        _StarDensity ("Star Density (grid cells)", Range(20, 400)) = 120
        _StarBrightness ("Star Brightness", Range(0.1, 5)) = 1.5
        _StarSizeMin ("Star Size Min", Range(0.01, 0.2)) = 0.05
        _StarSizeMax ("Star Size Max", Range(0.05, 0.8)) = 0.3
        _StarFalloff ("Radial Falloff", Range(0.5, 5)) = 1.5
        _StarConcentration ("Arm Concentration", Range(1, 15)) = 5.0

        [Header(Bright Stars and Supergiants)]
        _SupergiantChance ("Supergiant Chance", Range(0, 0.15)) = 0.03
        _SupergiantSize ("Supergiant Size", Range(0.1, 1.5)) = 0.6
        _SupergiantBrightness ("Supergiant Brightness", Range(1, 8)) = 4.0
        _SpikeLength ("Diffraction Spike Length", Range(0, 0.5)) = 0.15
        _SpikeWidth ("Diffraction Spike Width", Range(0.001, 0.05)) = 0.008

        [Header(Star Color Variation)]
        _BlueStarChance ("Blue Star Fraction", Range(0, 1)) = 0.3
        _RedStarChance ("Red Star Fraction", Range(0, 1)) = 0.15
        _YellowStarChance ("Yellow Star Fraction", Range(0, 1)) = 0.25
        _ColorBlue ("Blue Star Color", Color) = (0.6, 0.7, 1.0, 1)
        _ColorRed ("Red Giant Color", Color) = (1.0, 0.5, 0.3, 1)
        _ColorYellow ("Yellow Star Color", Color) = (1.0, 0.95, 0.7, 1)
        _ColorWhite ("White Star Color", Color) = (1.0, 1.0, 1.0, 1)

        [Header(Dust and Gas)]
        _DustIntensity ("Dust Glow Intensity", Range(0, 2)) = 0.5
        _DustNoiseScale ("Dust Noise Scale", Range(1, 10)) = 4.0
        _DustWarpStrength ("Dust Warp Strength", Range(0, 1)) = 0.3

        [Header(Halo)]
        _HaloSize ("Halo Size", Range(0.1, 2)) = 0.8
        _HaloIntensity ("Halo Brightness", Range(0, 1)) = 0.15
        _HaloFalloff ("Halo Falloff", Range(1, 6)) = 2.5

        [Header(Color)]
        _ColorCore ("Core Color", Color) = (1, 0.95, 0.8, 1)
        _ColorMid ("Arm Color", Color) = (0.6, 0.7, 1.0, 1)
        _ColorOuter ("Outer/Halo Color", Color) = (0.3, 0.35, 0.6, 1)
        _OverallBrightness ("Overall Brightness", Range(0.1, 3)) = 1.0
        _Opacity ("Opacity", Range(0, 1)) = 1.0

        [Header(Variation)]
        _EllipticityRange ("Ellipticity Range", Range(0, 0.6)) = 0.3
        _TiltRange ("Tilt Variation", Range(0, 1)) = 1.0

        [Header(Parallax)]
        _ParallaxFactor ("Parallax Factor", Float) = 0.001
        _ZoomResponse ("Zoom Response", Range(0, 1)) = 0.5

        [Header(Background)]
        [Toggle] _RenderBackground ("Render Background", Float) = 0
        _BackgroundColor ("Background Color", Color) = (0, 0, 0, 1)

        [Header(Seed)]
        _Seed ("Random Seed", Float) = 0

        [HideInInspector] _FabricNebulaDensity ("Fabric Nebula Density", Float) = 0
        [HideInInspector] _FabricAsteroidDensity ("Fabric Asteroid Density", Float) = 0
        [HideInInspector] _FabricVoidFactor ("Fabric Void Factor", Float) = 0
        [HideInInspector] _FabricAnomalyStrength ("Fabric Anomaly Strength", Float) = 0
        [HideInInspector] _FabricVoidStarFade ("Fabric Void Star Fade", Float) = 0
        [HideInInspector] _FabricVoidBgDarken ("Fabric Void Bg Darken", Float) = 0
        [HideInInspector] _FabricNebulaTint ("Fabric Nebula Tint", Vector) = (0.6, 0.3, 0.7, 1)
        [HideInInspector] _FabricNebulaTintStrength ("Fabric Nebula Tint Strength", Float) = 0
        [HideInInspector] _FabricAnomalyShift ("Fabric Anomaly Shift", Float) = 0
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
            Name "Galaxy"

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
                float _GalaxyCellSize;
                float _GalaxySpawnChance;
                float _GalaxySizeMin;
                float _GalaxySizeMax;
                float _ArmCount;
                float _ArmWinding;
                float _ArmSpread;
                float _ArmFalloff;
                float _CoreSize;
                float _CoreBrightness;
                float _CoreFalloff;
                float _StarDensity;
                float _StarBrightness;
                float _StarSizeMin;
                float _StarSizeMax;
                float _StarFalloff;
                float _StarConcentration;
                float _SupergiantChance;
                float _SupergiantSize;
                float _SupergiantBrightness;
                float _SpikeLength;
                float _SpikeWidth;
                float _BlueStarChance;
                float _RedStarChance;
                float _YellowStarChance;
                float4 _ColorBlue;
                float4 _ColorRed;
                float4 _ColorYellow;
                float4 _ColorWhite;
                float _DustIntensity;
                float _DustNoiseScale;
                float _DustWarpStrength;
                float _HaloSize;
                float _HaloIntensity;
                float _HaloFalloff;
                float4 _ColorCore;
                float4 _ColorMid;
                float4 _ColorOuter;
                float _OverallBrightness;
                float _Opacity;
                float _EllipticityRange;
                float _TiltRange;
                float _ParallaxFactor;
                float _ZoomResponse;
                float _RenderBackground;
                float4 _BackgroundColor;
                float _Seed;

                float _FabricNebulaDensity;
                float _FabricAsteroidDensity;
                float _FabricVoidFactor;
                float _FabricAnomalyStrength;
                float _FabricVoidStarFade;
                float _FabricVoidBgDarken;
                float4 _FabricNebulaTint;
                float _FabricNebulaTintStrength;
                float _FabricAnomalyShift;
            CBUFFER_END

            // Global camera properties
            float2 _CameraWorldPos; // Local Unity camera pos (near origin)
            float _ScreenAspect;
            float _CameraOrthoSize;
            float _ReferenceZoom;

            // Per-material parallax offset (computed in double precision on CPU)
            float2 _ParallaxOffset;

            // Warp effect globals
            float _WarpIntensity;
            float _WarpNebulaStretch;
            float _WarpNebulaFade;
            float2 _WarpDirection;

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

            float3 hash3(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac(float3(
                    (p3.x + p3.y) * p3.z,
                    (p3.x + p3.z) * p3.y,
                    (p3.y + p3.z) * p3.x));
            }

            float4 hash4(float2 p)
            {
                float4 p4 = frac(float4(p.xyxy) * float4(0.1031, 0.1030, 0.0973, 0.1099));
                p4 += dot(p4, p4.wzxy + 33.33);
                return frac((p4.xxyz + p4.yzzw) * p4.zywx);
            }

            // ============================================
            // 2D Perlin Noise
            // ============================================

            float2 grad2(float2 p, float seed)
            {
                float angle = hash1(p + seed) * 6.28318530718;
                return float2(cos(angle), sin(angle));
            }

            float perlin2D(float2 p, float seed)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float2 g00 = grad2(i + float2(0, 0), seed);
                float2 g10 = grad2(i + float2(1, 0), seed);
                float2 g01 = grad2(i + float2(0, 1), seed);
                float2 g11 = grad2(i + float2(1, 1), seed);

                float n00 = dot(g00, f - float2(0, 0));
                float n10 = dot(g10, f - float2(1, 0));
                float n01 = dot(g01, f - float2(0, 1));
                float n11 = dot(g11, f - float2(1, 1));

                return lerp(lerp(n00, n10, u.x), lerp(n01, n11, u.x), u.y) * 0.5 + 0.5;
            }

            // Simple FBM for dust
            float fbm(float2 p, float seed)
            {
                float value = 0.0;
                float amp = 1.0;
                float freq = 1.0;
                float maxVal = 0.0;

                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    value += perlin2D(p * freq, seed + float(i) * 100.0) * amp;
                    maxVal += amp;
                    amp *= 0.5;
                    freq *= 2.0;
                }
                return value / maxVal;
            }

            // ============================================
            // 2D Rotation Matrix
            // ============================================

            float2x2 rot2D(float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float2x2(c, -s, s, c);
            }

            // ============================================
            // Spiral Arm Density
            // ============================================

            float spiralArmDensity(float r, float theta, float armCount, float winding, float spread, float rotation)
            {
                float density = 0.0;
                float logR = log(max(r, 0.001));

                for (int arm = 0; arm < 6; arm++)
                {
                    if (arm >= (int)armCount) break;

                    float armOffset = (float(arm) / armCount) * 6.28318530718;
                    float spiralAngle = winding * logR + armOffset + rotation;

                    float angleDiff = theta - spiralAngle;
                    angleDiff = angleDiff - 6.28318530718 * round(angleDiff / 6.28318530718);

                    float localSpread = spread * (1.0 + r * 0.5);
                    float armStrength = exp(-0.5 * (angleDiff * angleDiff) / (localSpread * localSpread));

                    float radialEnvelope = r * exp(-r * _ArmFalloff);

                    density += armStrength * radialEnvelope;
                }

                return density;
            }

            // ============================================
            // Star Color from hash
            // ============================================

            float3 starColor(float colorHash)
            {
                // Classify star by spectral type using cumulative probability
                float cumBlue = _BlueStarChance;
                float cumRed = cumBlue + _RedStarChance;
                float cumYellow = cumRed + _YellowStarChance;

                if (colorHash < cumBlue)
                    return _ColorBlue.rgb;
                else if (colorHash < cumRed)
                    return _ColorRed.rgb;
                else if (colorHash < cumYellow)
                    return _ColorYellow.rgb;
                else
                    return _ColorWhite.rgb;
            }

            // ============================================
            // Diffraction Spikes for bright stars
            // ============================================

            float diffractionSpikes(float2 offset, float spikeLen, float spikeW, float rotation)
            {
                float spikes = 0.0;

                // 4 spikes at 45-degree angles (rotated per-star)
                float2 rotOffset = mul(rot2D(rotation), offset);

                // Horizontal spike
                float h = exp(-abs(rotOffset.y) / spikeW) * exp(-abs(rotOffset.x) / spikeLen);
                // Vertical spike
                float v = exp(-abs(rotOffset.x) / spikeW) * exp(-abs(rotOffset.y) / spikeLen);

                // Diagonal spikes (45 degrees)
                float2 diagOffset = mul(rot2D(0.7854), rotOffset); // PI/4
                float d1 = exp(-abs(diagOffset.y) / (spikeW * 0.7)) * exp(-abs(diagOffset.x) / (spikeLen * 0.6));
                float d2 = exp(-abs(diagOffset.x) / (spikeW * 0.7)) * exp(-abs(diagOffset.y) / (spikeLen * 0.6));

                spikes = h + v + (d1 + d2) * 0.5;
                return spikes;
            }

            // ============================================
            // Render Galaxy
            // ============================================

            float3 renderGalaxy(float2 localPos, float2 cellID)
            {
                // Per-galaxy random properties
                float3 rnd = hash3(cellID + _Seed);
                float3 rnd2 = hash3(cellID + _Seed + 50.0);

                float rotation = rnd.x * 6.28318530718;
                float ellipticity = 1.0 - rnd.y * _EllipticityRange;
                float tilt = (rnd.z - 0.5) * 6.28318530718 * _TiltRange;
                int armCount = clamp((int)(_ArmCount + (rnd2.x - 0.5) * 2.0), 1, 6);
                float windingVar = _ArmWinding * (0.7 + rnd2.y * 0.6);

                // Apply tilt (perspective foreshortening)
                float2 tiltedPos = mul(rot2D(tilt), localPos);
                tiltedPos.y *= ellipticity;

                // Polar coordinates
                float r = length(tiltedPos);
                float theta = atan2(tiltedPos.y, tiltedPos.x);

                // === Core bulge ===
                float coreProfile = exp(-pow(r / _CoreSize, _CoreFalloff));
                float3 core = _ColorCore.rgb * coreProfile * _CoreBrightness;

                // Add subtle core glow gradient (warm center fading to blue)
                float3 coreGlow = lerp(_ColorCore.rgb, _ColorMid.rgb * 0.5, saturate(r / (_CoreSize * 3.0)));
                float bulgeProfile = exp(-pow(r / (_CoreSize * 2.5), 2.0)) * 0.4;
                core += coreGlow * bulgeProfile;

                // === Spiral arm dust/gas ===
                float armDensity = spiralArmDensity(r, theta, armCount, windingVar, _ArmSpread, rotation);

                float2 dustUV = tiltedPos * _DustNoiseScale;
                float2 warp = float2(
                    perlin2D(dustUV * 0.5, _Seed + cellID.x),
                    perlin2D(dustUV * 0.5 + 100.0, _Seed + cellID.y)
                ) * _DustWarpStrength;
                float dustNoise = fbm(dustUV + warp, _Seed + cellID.x * 7.0 + cellID.y * 13.0);

                // Clumpy arm dust
                float modulatedArm = armDensity * (0.4 + dustNoise * 0.6);
                // Dark dust lanes: subtract some dust in certain noise bands
                float darkLane = smoothstep(0.45, 0.55, fbm(dustUV * 1.5 + 30.0, _Seed + 300.0));
                modulatedArm *= lerp(0.3, 1.0, darkLane);

                float armColorT = saturate(r * 1.5);
                float3 armColor = lerp(_ColorMid.rgb, _ColorOuter.rgb, armColorT);
                float3 arms = armColor * modulatedArm * _DustIntensity;

                // === Dense star population ===
                // We render stars at multiple density scales for richness
                float3 totalStars = float3(0, 0, 0);
                float galaxySeed = _Seed + cellID.x * 7.0 + cellID.y * 13.0;

                // --- Layer 1: Dense faint background stars ---
                {
                    float scale1 = _StarDensity;
                    float2 sUV = tiltedPos * scale1;
                    float2 sCell = floor(sUV);
                    float2 sFrac = frac(sUV);

                    for (int sy = -1; sy <= 1; sy++)
                    {
                        for (int sx = -1; sx <= 1; sx++)
                        {
                            float2 nb = float2(sx, sy);
                            float2 nbCell = sCell + nb;
                            float4 sRnd = hash4(nbCell + galaxySeed);

                            float2 sPos = nb + sRnd.xy - sFrac;
                            float sDist = length(sPos);

                            // World position of this star within galaxy
                            float2 starWorldPos = (nbCell + sRnd.xy) / scale1;
                            float starR = length(starWorldPos);
                            float starTheta = atan2(starWorldPos.y, starWorldPos.x);

                            // Spawn probability: strongly arm + core driven
                            float armProb = spiralArmDensity(starR, starTheta, armCount, windingVar, _ArmSpread * 1.3, rotation);
                            float coreProb = exp(-pow(starR / (_CoreSize * 3.0), 2.0));
                            float radialFade = exp(-starR * starR * _StarFalloff);

                            // Stars live in arms and core — very few between arms
                            // armProb is typically 0-0.3, so concentration of 5+ makes this dominant
                            float structureProb = armProb * _StarConcentration + coreProb * 3.0;
                            // Tiny inter-arm scatter (exponentially suppressed)
                            float interArmScatter = 0.02 * exp(-starR * 4.0);
                            float spawnProb = saturate(structureProb + interArmScatter) * radialFade;

                            if (sRnd.z > spawnProb) continue;

                            // Size variation: power-law (most small, few large)
                            float sizeFactor = pow(sRnd.w, 3.0);
                            float starSize = lerp(_StarSizeMin, _StarSizeMax, sizeFactor);

                            // Stars in core region are slightly larger on average
                            starSize *= lerp(1.0, 1.4, coreProb);

                            // Sharp core + soft glow for each star
                            float profile = smoothstep(starSize, starSize * 0.15, sDist);
                            float glow = exp(-sDist * sDist / (starSize * starSize * 3.0)) * 0.4;

                            // Brightness varies — brighter stars are rarer (correlated with size)
                            float brightness = (0.3 + sizeFactor * 1.5) * _StarBrightness;

                            // Color based on a separate hash channel
                            float colorSeed = hash1(nbCell + galaxySeed + 300.0);
                            float3 sColor = starColor(colorSeed);

                            // Stars near core trend warmer
                            float warmBlend = saturate(1.0 - starR * 3.0);
                            sColor = lerp(sColor, _ColorCore.rgb, warmBlend * 0.5);

                            totalStars += sColor * (profile + glow) * brightness;
                        }
                    }
                }

                // --- Layer 2: Supergiants with diffraction spikes ---
                {
                    float scale2 = _StarDensity * 0.25; // much sparser grid
                    float2 sUV2 = tiltedPos * scale2;
                    float2 sCell2 = floor(sUV2);
                    float2 sFrac2 = frac(sUV2);

                    for (int sy2 = -1; sy2 <= 1; sy2++)
                    {
                        for (int sx2 = -1; sx2 <= 1; sx2++)
                        {
                            float2 nb2 = float2(sx2, sy2);
                            float2 nbCell2 = sCell2 + nb2;
                            float4 sRnd2 = hash4(nbCell2 + galaxySeed + 500.0);

                            // Only spawn if supergiant check passes
                            if (sRnd2.z > _SupergiantChance) continue;

                            float2 sPos2 = nb2 + sRnd2.xy - sFrac2;
                            float sDist2 = length(sPos2);

                            // World position check
                            float2 sgWorldPos = (nbCell2 + sRnd2.xy) / scale2;
                            float sgR = length(sgWorldPos);
                            float sgTheta = atan2(sgWorldPos.y, sgWorldPos.x);
                            float sgArmProb = spiralArmDensity(sgR, sgTheta, armCount, windingVar, _ArmSpread * 1.5, rotation);
                            float sgRadial = exp(-sgR * sgR * _StarFalloff * 0.5);

                            // Supergiants strongly prefer arms and core — almost never between arms
                            float sgSpawnProb = saturate(sgArmProb * _StarConcentration * 0.8 + 0.05) * sgRadial;
                            if (hash1(nbCell2 + galaxySeed + 700.0) > sgSpawnProb) continue;

                            float sgSize = _SupergiantSize * (0.7 + sRnd2.w * 0.6);

                            // Bright core with tight falloff
                            float sgProfile = exp(-sDist2 * sDist2 / (sgSize * sgSize * 0.3));
                            // Wide glow
                            float sgGlow = exp(-sDist2 / (sgSize * 1.5)) * 0.6;

                            // Diffraction spikes — use cell-space offset directly
                            float spikeRot = sRnd2.x * 3.14159;
                            float spikes = diffractionSpikes(sPos2, _SpikeLength, _SpikeWidth, spikeRot);

                            // Color: supergiants are often blue or red
                            float3 sgColor;
                            if (sRnd2.w < 0.4)
                                sgColor = _ColorBlue.rgb * 1.2; // blue supergiant
                            else if (sRnd2.w < 0.6)
                                sgColor = _ColorRed.rgb * 1.1; // red supergiant
                            else
                                sgColor = _ColorWhite.rgb; // white

                            float sgBright = _SupergiantBrightness * (0.7 + sRnd2.w * 0.6);
                            totalStars += sgColor * (sgProfile + sgGlow + spikes * 0.4) * sgBright;
                        }
                    }
                }

                // === Halo ===
                float haloProfile = exp(-pow(r / _HaloSize, _HaloFalloff));
                // Halo also gets some scattered faint stars via noise
                float haloNoise = fbm(tiltedPos * 3.0, _Seed + 800.0);
                float3 halo = _ColorOuter.rgb * haloProfile * _HaloIntensity * (0.8 + haloNoise * 0.4);

                // === Combine ===
                float3 galaxy = core + arms + totalStars + halo;
                galaxy *= _OverallBrightness;

                // Soft outer edge fade — nothing beyond ~1.5 radius
                float edgeFade = 1.0 - smoothstep(1.2, 1.8, r);
                galaxy *= edgeFade;

                return galaxy;
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

                // Zoom factor
                // Use _ZoomResponse instead of parallax-derived value, since very low parallax
                // (e.g. 0.001) would make saturate(parallax * 10) ≈ 0 and ignore zoom entirely
                float zoomFactor = _CameraOrthoSize / max(_ReferenceZoom, 0.001);
                float depthZoomFactor = lerp(1.0, zoomFactor, _ZoomResponse);

                // Scale UVs around center
                float2 scaledUV = (uv - 0.5) * depthZoomFactor + 0.5;

                // Apply parallax offset (pre-computed on CPU in double precision)
                float2 parallaxUV = scaledUV + _ParallaxOffset;

                // Aspect ratio correction
                float2 worldUV = float2(parallaxUV.x * _ScreenAspect, parallaxUV.y);

                // Apply warp stretching
                [branch] if (_WarpIntensity > 0.001 && _WarpNebulaStretch > 0.001)
                {
                    float parallel = dot(worldUV, _WarpDirection);
                    float2 perp = worldUV - _WarpDirection * parallel;
                    parallel /= _WarpNebulaStretch;
                    worldUV = perp + _WarpDirection * parallel;
                }

                // Grid-based galaxy placement
                float cellSize = _GalaxyCellSize;
                float2 cellUV = worldUV / cellSize;
                float2 cellID = floor(cellUV);

                float3 totalColor = float3(0, 0, 0);

                // Check 3x3 neighborhood for galaxies
                for (int cy = -1; cy <= 1; cy++)
                {
                    for (int cx = -1; cx <= 1; cx++)
                    {
                        float2 neighborID = cellID + float2(cx, cy);
                        float2 neighborRnd = hash2(neighborID + _Seed * 0.1);

                        // Spawn check
                        float spawnHash = hash1(neighborID + _Seed * 0.7);
                        if (spawnHash > _GalaxySpawnChance) continue;

                        // Galaxy center within cell (jittered)
                        float2 galaxyCenter = neighborID + 0.3 + neighborRnd * 0.4;

                        // Galaxy size
                        float sizeHash = hash1(neighborID + _Seed * 1.3);
                        float galaxySize = lerp(_GalaxySizeMin, _GalaxySizeMax, sizeHash) / cellSize;

                        // Local position relative to galaxy center
                        float2 localPos = (cellUV - galaxyCenter) / galaxySize;

                        // Early out if too far from galaxy
                        float dist = length(localPos);
                        if (dist > 2.0) continue;

                        float3 galaxyColor = renderGalaxy(localPos, neighborID);
                        totalColor += galaxyColor;
                    }
                }

                // Apply warp fade
                totalColor *= lerp(1.0, 1.0 - _WarpNebulaFade, _WarpIntensity);

                // World Fabric: dim in void regions
                totalColor *= lerp(1.0, 0.3, _FabricVoidStarFade * _FabricVoidFactor);

                // Apply opacity for distance appearance control
                totalColor *= _Opacity;

                // Calculate alpha from luminance
                float alpha = saturate(dot(totalColor, float3(0.299, 0.587, 0.114)));

                // Background handling
                if (_RenderBackground > 0.5)
                {
                    float3 finalColor = _BackgroundColor.rgb + totalColor;
                    return half4(finalColor, 1.0);
                }
                else
                {
                    return half4(totalColor, alpha);
                }
            }

            ENDHLSL
        }
    }
}
