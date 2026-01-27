Shader "Starfire/ShapedStarfield"
{
    Properties
    {
        [Header(Star Field)]
        _StarDensity ("Star Density", Range(1, 100)) = 20
        _SpawnChance ("Spawn Chance", Range(0, 1)) = 0.8

        [Header(Brightness)]
        _StarBrightnessMin ("Brightness Min", Range(0.1, 2)) = 0.3
        _StarBrightnessMax ("Brightness Max", Range(0.1, 2)) = 1.0
        _BrightnessDistribution ("Brightness Distribution", Range(0, 1)) = 0.5

        [Header(Star Size)]
        [Min(0)] _StarSizeMin ("Size Min", Float) = 0.02
        [Min(0)] _StarSizeMax ("Size Max", Float) = 0.15
        _SizeDistribution ("Size Distribution", Range(0, 1)) = 0.5

        [Header(Twinkle)]
        _TwinkleSpeed ("Twinkle Speed", Range(0, 5)) = 1.0
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.3

        [Header(Color)]
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        _ColorVariation ("Color Variation", Range(0, 1)) = 0.3
        _WarmCoolMix ("Warm/Cool Mix", Range(0, 1)) = 0.5

        [Header(Background)]
        _BackgroundColor ("Background Color", Color) = (0, 0, 0.02, 1)
        [Min(0)] _ParallaxFactor ("Parallax Factor", Float) = 0.02

        [Header(Layer Mode)]
        [Toggle] _RenderBackground ("Render Background", Float) = 1

        [Header(Distribution)]
        _LayerSeed ("Layer Seed", Float) = 0
        _ClusterAmount ("Cluster Amount", Range(0, 1)) = 0.3
        _ClusterScale ("Cluster Scale", Float) = 0.05

        [Header(Shape Configuration)]
        [IntRange] _ShapeCount ("Active Shape Count", Range(1, 4)) = 4
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ShapedStarfield"

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

            #define MAX_SHAPES 4

            CBUFFER_START(UnityPerMaterial)
                float _StarDensity;
                float _SpawnChance;
                float _StarBrightnessMin;
                float _StarBrightnessMax;
                float _BrightnessDistribution;
                float _StarSizeMin;
                float _StarSizeMax;
                float _SizeDistribution;
                float _TwinkleSpeed;
                float _TwinkleAmount;
                float4 _StarColor;
                float _ColorVariation;
                float _WarmCoolMix;
                float4 _BackgroundColor;
                float _ParallaxFactor;
                float _RenderBackground;
                float _LayerSeed;
                float _ClusterAmount;
                float _ClusterScale;
                int _ShapeCount;
            CBUFFER_END

            // Per-shape arrays (set from C#)
            // x = shapeType, y = spawnWeight, z = sizeMin, w = sizeMax
            float4 _ShapeParams[MAX_SHAPES];
            // x = edgeSharpness, y = colorVariation, z = brightnessMin, w = brightnessMax
            float4 _ShapeVisuals[MAX_SHAPES];
            // rgba = per-shape color
            float4 _ShapeColors[MAX_SHAPES];
            // Cumulative weights for shape selection (precomputed on CPU)
            float4 _CumulativeWeights;

            // Set from script
            float2 _CameraWorldPos;
            float _ScreenAspect;
            float _CameraOrthoSize;
            float _ReferenceZoom;

            // Warp effect globals (set by WarpEffectController)
            float _WarpIntensity;
            float _WarpStretch;
            float2 _WarpDirection;
            float _WarpBrightnessBoost;

            // Streak shape globals (set by WarpEffectController from config)
            float _WarpStreakWidth;
            float _WarpEdgeSoftnessMin;
            float _WarpEdgeSoftnessMax;
            float _WarpLeadingEdgeRatio;
            float _WarpTrailingFadeStart;
            float _WarpBlendTransition;
            float _WarpDistantMinEffect;
            float _WarpParallaxMultiplier;
            float _WarpTailTaperPower;

            // Wobble globals (set by WarpEffectController from config)
            float _WobbleIntensity;
            float _WobbleFrequency;
            float _WobbleSpeed;

            // World Fabric globals (set by WorldFabricBridge)
            float _FabricNebulaDensity;
            float _FabricAsteroidDensity;
            float _FabricVoidFactor;
            float _FabricAnomalyStrength;
            float _FabricVoidStarFade;
            float _FabricVoidBgDarken;
            float4 _FabricNebulaTint;
            float _FabricNebulaTintStrength;
            float _FabricAnomalyShift;

            // PCG-style hash functions
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

            // Gradient function for Perlin noise
            float2 grad2(float2 p)
            {
                float angle = hash1(p) * 6.28318530718;
                return float2(cos(angle), sin(angle));
            }

            // 2D Perlin noise for natural clustering
            float perlin2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                float2 g00 = grad2(i + float2(0.0, 0.0));
                float2 g10 = grad2(i + float2(1.0, 0.0));
                float2 g01 = grad2(i + float2(0.0, 1.0));
                float2 g11 = grad2(i + float2(1.0, 1.0));

                float n00 = dot(g00, f - float2(0.0, 0.0));
                float n10 = dot(g10, f - float2(1.0, 0.0));
                float n01 = dot(g01, f - float2(0.0, 1.0));
                float n11 = dot(g11, f - float2(1.0, 1.0));

                float nx0 = lerp(n00, n10, u.x);
                float nx1 = lerp(n01, n11, u.x);
                return lerp(nx0, nx1, u.y) * 0.5 + 0.5;
            }

            // HSV to RGB conversion
            float3 hsv2rgb(float3 c)
            {
                float3 p = abs(frac(c.xxx + float3(1.0, 2.0/3.0, 1.0/3.0)) * 6.0 - 3.0);
                return c.z * lerp(float3(1,1,1), saturate(p - 1.0), c.y);
            }

            // ============================================
            // SDF Functions for each shape type
            // All scaled to produce visually similar sizes
            // ============================================

            float sdfCircle(float2 p)
            {
                return length(p);
            }

            float sdfDiamond(float2 p)
            {
                // L1 norm (Manhattan distance), scaled to match circle visual size
                return (abs(p.x) + abs(p.y)) * 0.707;
            }

            float sdfFourPointStar(float2 p)
            {
                // Blend of circle and diamond creates concave 4-pointed star
                float circle = length(p);
                float diamond = (abs(p.x) + abs(p.y)) * 0.707;
                return min(circle, diamond * 0.8);
            }

            float sdfSquare(float2 p)
            {
                // L-infinity norm (Chebyshev distance)
                return max(abs(p.x), abs(p.y));
            }

            // Unified SDF selector
            float getShapeSDF(float2 p, int shapeType)
            {
                [branch]
                switch (shapeType)
                {
                    case 0: return sdfCircle(p);
                    case 1: return sdfDiamond(p);
                    case 2: return sdfFourPointStar(p);
                    case 3: return sdfSquare(p);
                    default: return sdfCircle(p);
                }
            }

            // Get star color based on warm/cool mix and per-star variation
            float3 getStarColor(float2 cellID, float3 baseColor, float variation, float warmCool)
            {
                float colorRandom = hash1(cellID + 300.0);
                float colorChoice = hash1(cellID + 400.0);

                float warmHue = lerp(0.02, 0.12, colorRandom);
                float coolHue = lerp(0.55, 0.68, colorRandom);
                float hue = (colorChoice > warmCool) ? warmHue : coolHue;

                float saturation = variation * lerp(0.3, 0.8, hash1(cellID + 350.0));

                float3 tintColor = hsv2rgb(float3(hue, saturation, 1.0));
                return baseColor * tintColor;
            }

            // Select shape index based on hash and cumulative weights
            int selectShapeIndex(float2 cellID, int shapeCount)
            {
                float shapeRoll = hash1(cellID + 500.0);
                int shapeIndex = 0;

                [branch]
                if (shapeCount > 1 && shapeRoll >= _CumulativeWeights.x) shapeIndex = 1;
                [branch]
                if (shapeCount > 2 && shapeRoll >= _CumulativeWeights.y) shapeIndex = 2;
                [branch]
                if (shapeCount > 3 && shapeRoll >= _CumulativeWeights.z) shapeIndex = 3;

                return shapeIndex;
            }

            // Generate stars with multiple shapes
            float3 shapedStars(float2 uv, float density, float spawnChance, float twinkleSpeed, float twinkleAmount, float time, float aspect, float warmCool, float layerSeed, float clusterAmount, float clusterScale, int shapeCount)
            {
                float3 result = float3(0, 0, 0);

                // Correct for aspect ratio
                float2 aspectCorrectedUV = float2(uv.x * aspect, uv.y);

                // Apply layer seed offset
                float2 layerOffset = hash2(float2(layerSeed * 127.1, layerSeed * 311.7)) * 1000.0;
                float2 offsetUV = aspectCorrectedUV + layerOffset;

                // Scale UV by density
                float2 gridUV = offsetUV * density;

                // Get grid cell ID
                float2 cellID = floor(gridUV);

                // Get position within cell
                float2 cellUV = frac(gridUV);

                // Check current cell and neighbors
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 neighborCell = cellID + float2(x, y);

                        // Calculate cluster noise for density variation
                        float adjustedSpawnChance = spawnChance;
                        [branch] if (clusterAmount > 0.01)
                        {
                            float clusterNoise = perlin2D(neighborCell * clusterScale);
                            float clusterModifier = lerp(1.0, 0.2 + clusterNoise * 0.8, clusterAmount);
                            adjustedSpawnChance = spawnChance * clusterModifier;
                        }

                        // Spawn chance check
                        float spawnRoll = hash1(neighborCell + 200.0);
                        if (spawnRoll > adjustedSpawnChance) continue;

                        // Select shape for this star
                        int shapeIndex = selectShapeIndex(neighborCell, shapeCount);

                        // Get per-shape parameters
                        float4 shapeParams = _ShapeParams[shapeIndex];
                        float4 shapeVisuals = _ShapeVisuals[shapeIndex];
                        float4 shapeColor = _ShapeColors[shapeIndex];

                        int shapeType = (int)shapeParams.x;
                        float sizeMin = shapeParams.z;
                        float sizeMax = shapeParams.w;
                        float edgeSharpness = shapeVisuals.x;
                        float colorVariation = shapeVisuals.y;
                        float brightnessMin = shapeVisuals.z;
                        float brightnessMax = shapeVisuals.w;

                        // Random position for star within cell
                        float2 starPos = hash2(neighborCell);

                        // Brightness with distribution curve
                        float brightnessRand = hash1(neighborCell + 100.0);
                        float distributionPower = lerp(2.0, 0.5, _BrightnessDistribution);
                        brightnessRand = pow(brightnessRand, distributionPower);
                        float brightness = lerp(brightnessMin, brightnessMax, brightnessRand);

                        // Twinkle
                        float twinklePhase = hash1(neighborCell + 150.0) * 6.28318;
                        float twinkleIntensity = hash1(neighborCell + 175.0);
                        float twinkleWave = sin(time * twinkleSpeed + twinklePhase);
                        float twinkleBrightness = 0.1 + (twinkleWave * 0.5 + 0.5) * 0.9;
                        brightness *= lerp(1.0, twinkleBrightness, twinkleAmount * twinkleIntensity);

                        // Distance from current UV to star position
                        float2 toStar = (cellUV - float2(x, y)) - starPos;

                        // Size with distribution curve
                        float sizeRandom = hash1(neighborCell + 50.0);
                        float curvedRandom = pow(sizeRandom, 1.0 + _SizeDistribution * 3.0);
                        float starRadius = lerp(sizeMin, sizeMax, curvedRandom);

                        // NORMAL SHAPED STAR (always calculated as base)
                        float normalDist = getShapeSDF(toStar, shapeType);
                        float falloffWidth = starRadius * lerp(1.0, 0.1, edgeSharpness);
                        float falloffStart = starRadius - falloffWidth;
                        float normalStar = 1.0 - smoothstep(falloffStart, starRadius, normalDist);

                        float star = normalStar;

                        // Blend to streak mode when warping (smooth transition, no pop)
                        [branch] if (_WarpIntensity > 0.001 && _WarpStretch > 1.001)
                        {
                            // COMET STREAK MODE - round head, pointed tail (comet shape)
                            // During warp, all shapes become streaks (SDF shapes don't make sense when stretched)

                            // Depth-based warp scaling: distant layers (low parallax) streak less
                            float depthWarpScale = lerp(_WarpDistantMinEffect, 1.0, saturate(_ParallaxFactor * _WarpParallaxMultiplier));
                            float effectiveStretch = lerp(1.0, _WarpStretch, depthWarpScale);
                            float effectiveIntensity = _WarpIntensity * depthWarpScale;

                            float parallelDist = dot(toStar, _WarpDirection);
                            float2 perpComponent = toStar - _WarpDirection * parallelDist;
                            float perpDist = length(perpComponent);

                            // Streak thins out as it stretches (configurable width at full warp)
                            float streakWidth = starRadius * lerp(1.0, _WarpStreakWidth, effectiveIntensity);
                            float streakLength = starRadius * effectiveStretch;

                            // Edge softness for perpendicular edges
                            float warpEdgeSoftness = lerp(_WarpEdgeSoftnessMin, _WarpEdgeSoftnessMax, effectiveIntensity);

                            float streakStar;

                            // parallelDist > 0 = ahead of star (in movement direction) = leading edge
                            // parallelDist < 0 = behind star (opposite to movement) = trailing edge / motion trail
                            if (parallelDist >= 0.0)
                            {
                                // LEADING EDGE (HEAD): Use original circular star falloff
                                // This preserves the round shape at the front
                                float headDist = length(toStar);
                                float headRadius = starRadius;
                                float headFalloff = headRadius * lerp(1.0, 0.1, warpEdgeSoftness);
                                streakStar = 1.0 - smoothstep(headRadius - headFalloff, headRadius, headDist);

                                // Also apply short leading cutoff (prevents star from rendering ahead)
                                float leadingLength = streakLength * _WarpLeadingEdgeRatio;
                                float leadingFade = 1.0 - smoothstep(leadingLength * 0.5, leadingLength, parallelDist);
                                streakStar *= leadingFade;
                            }
                            else
                            {
                                // TRAILING EDGE (TAIL): Tapered pointed shape
                                float trailDist = -parallelDist; // Make positive for comparison
                                float trailingLength = streakLength;

                                // How far along the tail (0 = at star, 1 = at tip)
                                float tailProgress = saturate(trailDist / trailingLength);

                                // Width tapers from full at head to pointed at tip
                                // tailTaperPower < 1 = slower taper at start (fatter tail)
                                // tailTaperPower > 1 = faster taper at start (sharper point)
                                float taperFactor = 1.0 - pow(tailProgress, _WarpTailTaperPower);
                                float taperedWidth = streakWidth * taperFactor;

                                // Apply wobble to perpendicular distance (only if wobble enabled)
                                float wobbledPerpDist = perpDist;
                                [branch] if (_WobbleIntensity > 0.001)
                                {
                                    // Per-star unique offset to prevent synchronization
                                    float starOffset = hash1(neighborCell) * 100.0;

                                    // Time flows through the noise field, creating animated waves
                                    // trailDist controls wave position along streak
                                    // Time + starOffset makes each star's wobble unique and animated
                                    float wobbleNoise = perlin2D(float2(
                                        trailDist * _WobbleFrequency,
                                        _Time.y * _WobbleSpeed + starOffset
                                    )) * 2.0 - 1.0; // Normalize to [-1, 1]

                                    // Scale wobble by taperedWidth (not starRadius) for visible effect
                                    // taperFactor ensures wobble fades toward tip
                                    float wobbleOffset = wobbleNoise * _WobbleIntensity * taperFactor * taperedWidth;

                                    // Apply wobble to perpendicular distance
                                    wobbledPerpDist = perpDist + wobbleOffset;
                                }

                                // Perpendicular edge with tapered width (using wobbled distance)
                                float perpEdge = 1.0 - smoothstep(taperedWidth * (1.0 - warpEdgeSoftness), taperedWidth, wobbledPerpDist);

                                // Parallel fade (brightness drops off toward tail)
                                float endTaper = 1.0 - smoothstep(trailingLength * _WarpTrailingFadeStart, trailingLength, trailDist);

                                streakStar = perpEdge * endTaper;
                            }

                            // Smooth blend from normal to streak (prevents brightness pop at threshold)
                            float blendFactor = smoothstep(0.0, _WarpBlendTransition, _WarpIntensity);
                            star = lerp(normalStar, streakStar, blendFactor);
                        }

                        // Get per-star color
                        float3 starColorFinal = getStarColor(neighborCell, shapeColor.rgb, colorVariation, warmCool);

                        // Minimal brightness boost during warp (keeps streaks clean)
                        float brightnessBoost = _WarpBrightnessBoost > 0.001 ? _WarpBrightnessBoost : 1.0;
                        brightness *= lerp(1.0, brightnessBoost, _WarpIntensity);

                        result += star * brightness * starColorFinal;
                    }
                }

                return saturate(result);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Calculate zoom factor
                float zoomFactor = _CameraOrthoSize / _ReferenceZoom;

                // Depth-aware zoom: distant layers (low parallax) zoom less, nearby layers zoom more
                float depthZoomFactor = lerp(1.0, zoomFactor, saturate(_ParallaxFactor * 10.0));

                // Scale UVs around center
                float2 scaledUV = (uv - 0.5) * depthZoomFactor + 0.5;

                // Apply parallax offset
                float2 parallaxOffset = _CameraWorldPos * _ParallaxFactor;
                float2 parallaxUV = scaledUV + parallaxOffset;

                // Generate shaped starfield
                float3 starValue = shapedStars(
                    parallaxUV,
                    _StarDensity,
                    _SpawnChance,
                    _TwinkleSpeed,
                    _TwinkleAmount,
                    _Time.y,
                    _ScreenAspect,
                    _WarmCoolMix,
                    _LayerSeed,
                    _ClusterAmount,
                    _ClusterScale,
                    _ShapeCount
                );

                // === World Fabric modulation ===
                // Void: fade stars
                float voidDim = 1.0 - _FabricVoidFactor * _FabricVoidStarFade;
                starValue *= voidDim;

                // Nebula: tint stars
                starValue = lerp(starValue, starValue * _FabricNebulaTint.rgb, _FabricNebulaDensity * _FabricNebulaTintStrength);

                // Anomaly: subtle color shift
                [branch] if (_FabricAnomalyStrength > 0.01)
                {
                    float anomalyT = _FabricAnomalyStrength * _FabricAnomalyShift;
                    float3 anomalyShift = float3(
                        starValue.r + starValue.g * anomalyT * 0.3,
                        starValue.g * (1.0 - anomalyT * 0.5),
                        starValue.b + starValue.r * anomalyT * 0.2
                    );
                    starValue = lerp(starValue, anomalyShift, anomalyT);
                }

                // Calculate alpha from luminance
                float starAlpha = saturate(dot(starValue, float3(0.299, 0.587, 0.114)) * 2.0);

                // Combine with background or output transparent
                if (_RenderBackground > 0.5)
                {
                    float bgDim = 1.0 - _FabricVoidFactor * _FabricVoidBgDarken;
                    float3 finalColor = _BackgroundColor.rgb * bgDim + starValue;
                    return half4(finalColor, 1.0);
                }
                else
                {
                    return half4(starValue, starAlpha);
                }
            }

            ENDHLSL
        }
    }
}
