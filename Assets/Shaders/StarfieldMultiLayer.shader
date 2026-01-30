Shader "Starfire/StarfieldMultiLayer"
{
    Properties
    {
        [Header(MultiLayer Configuration)]
        [IntRange] _DepthCount ("Active Depth Count", Range(1, 8)) = 4

        [Header(Shared Star Settings)]
        _SpawnChance ("Spawn Chance", Range(0, 1)) = 0.8

        [Header(Brightness)]
        _StarBrightnessMin ("Brightness Min", Range(0.1, 2)) = 0.3
        _StarBrightnessMax ("Brightness Max", Range(0.1, 2)) = 1.0
        _BrightnessDistribution ("Brightness Distribution", Range(0, 1)) = 0.5

        [Header(Twinkle)]
        _TwinkleSpeed ("Twinkle Speed", Range(0, 5)) = 1.0
        _TwinkleAmount ("Twinkle Amount", Range(0, 1)) = 0.3

        [Header(Color)]
        _ColorVariation ("Color Variation", Range(0, 1)) = 0.3
        _WarmCoolMix ("Warm Cool Mix", Range(0, 1)) = 0.5

        [Header(Shape)]
        _EdgeSharpness ("Edge Sharpness", Range(0, 1)) = 0.0
        _SizeDistribution ("Size Distribution", Range(0, 1)) = 0.5

        [Header(Background)]
        _BackgroundColor ("Background Color", Color) = (0, 0, 0.02, 1)
        [Toggle] _RenderBackground ("Render Background", Float) = 1

        [Header(Clustering)]
        _ClusterAmount ("Cluster Amount", Range(0, 1)) = 0.3
        _ClusterScale ("Cluster Scale", Float) = 0.05

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
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StarfieldMultiLayer"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend One OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_DEPTHS 8

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
                int _DepthCount;

                // Shared parameters
                float _SpawnChance;
                float _StarBrightnessMin;
                float _StarBrightnessMax;
                float _BrightnessDistribution;
                float _TwinkleSpeed;
                float _TwinkleAmount;
                float _ColorVariation;
                float _WarmCoolMix;
                float _EdgeSharpness;
                float _SizeDistribution;
                float4 _BackgroundColor;
                float _RenderBackground;
                float _ClusterAmount;
                float _ClusterScale;

                // World Fabric properties (set per-material by WorldFabricBridge)
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

            // Star density reduction globals (set by WarpEffectController)
            float _WarpStarFade;
            float _WarpStarFadeNearBias;
            float _WarpStarFadeMinDepth;
            float _WarpParallaxMultiplier;

            // Per-depth arrays (outside CBUFFER for better compatibility)
            // x = parallax, y = density, z = sizeMin, w = sizeMax
            float4 _DepthParams[MAX_DEPTHS];
            // Per-depth colors
            float4 _DepthColors[MAX_DEPTHS];
            // Per-depth seeds
            float _DepthSeeds[MAX_DEPTHS];

            // Set from script (globals)
            float2 _CameraWorldPos; // Local Unity camera pos (near origin)
            float _ScreenAspect;
            float _CameraOrthoSize;
            float _ReferenceZoom;

            // Per-depth parallax offsets (computed in double precision on CPU)
            float4 _DepthParallaxOffsets[MAX_DEPTHS];

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

            // Generate stars for a single depth layer
            float3 starsAtDepth(float2 uv, float density, float sizeMin, float sizeMax, float layerSeed, float3 baseColor, float spawnChance)
            {
                float3 result = float3(0, 0, 0);

                // Correct for aspect ratio
                float2 aspectCorrectedUV = float2(uv.x * _ScreenAspect, uv.y);

                // Apply layer seed offset
                float2 layerOffset = hash2(float2(layerSeed * 127.1, layerSeed * 311.7)) * 1000.0;
                float2 offsetUV = aspectCorrectedUV + layerOffset;

                // Scale UV by density
                float2 gridUV = offsetUV * density;

                // Get grid cell ID
                float2 cellID = floor(gridUV);
                float2 cellUV = frac(gridUV);

                // Check current cell and neighbors
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 neighborCell = cellID + float2(x, y);

                        // Calculate cluster noise (skip if disabled)
                        float adjustedSpawnChance = spawnChance;
                        [branch] if (_ClusterAmount > 0.01)
                        {
                            float clusterNoise = perlin2D(neighborCell * _ClusterScale);
                            float clusterModifier = lerp(1.0, 0.2 + clusterNoise * 0.8, _ClusterAmount);
                            adjustedSpawnChance = spawnChance * clusterModifier;
                        }

                        // Spawn chance check
                        float spawnRoll = hash1(neighborCell + 200.0);
                        if (spawnRoll > adjustedSpawnChance) continue;

                        // Random position for star within cell
                        float2 starPos = hash2(neighborCell);

                        // Random brightness variation
                        float brightnessRand = hash1(neighborCell + 100.0);
                        float distributionPower = lerp(2.0, 0.5, _BrightnessDistribution);
                        brightnessRand = pow(brightnessRand, distributionPower);
                        float brightness = lerp(_StarBrightnessMin, _StarBrightnessMax, brightnessRand);

                        // Twinkle
                        float twinklePhase = hash1(neighborCell + 150.0) * 6.28318;
                        float twinkleIntensity = hash1(neighborCell + 175.0);
                        float twinkleWave = sin(_Time.y * _TwinkleSpeed + twinklePhase);
                        float twinkleBrightness = 0.1 + (twinkleWave * 0.5 + 0.5) * 0.9;
                        brightness *= lerp(1.0, twinkleBrightness, _TwinkleAmount * twinkleIntensity);

                        // Distance from current UV to star position
                        float2 toStar = (cellUV - float2(x, y)) - starPos;
                        float dist = length(toStar);

                        // Size with distribution curve
                        float sizeRandom = hash1(neighborCell + 50.0);
                        float curvedRandom = pow(sizeRandom, 1.0 + _SizeDistribution * 3.0);
                        float starRadius = lerp(sizeMin, sizeMax, curvedRandom);

                        // Create star with adjustable edge sharpness
                        float falloffWidth = starRadius * lerp(1.0, 0.1, _EdgeSharpness);
                        float falloffStart = starRadius - falloffWidth;
                        float star = 1.0 - smoothstep(falloffStart, starRadius, dist);

                        // Get per-star color
                        float3 starColor = getStarColor(neighborCell, baseColor, _ColorVariation, _WarmCoolMix);

                        result += star * brightness * starColor;
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
                float3 result = float3(0, 0, 0);

                // Add background color first if enabled
                if (_RenderBackground > 0.5)
                {
                    result = _BackgroundColor.rgb;
                }

                // Calculate base zoom factor
                float zoomFactor = _CameraOrthoSize / _ReferenceZoom;

                // Loop through all active depths
                int depthCount = min(_DepthCount, MAX_DEPTHS);
                for (int d = 0; d < depthCount; d++)
                {
                    float parallax = _DepthParams[d].x;
                    float density = _DepthParams[d].y;
                    float sizeMin = _DepthParams[d].z;
                    float sizeMax = _DepthParams[d].w;
                    float3 depthColor = _DepthColors[d].rgb;
                    float seed = _DepthSeeds[d];

                    // Depth-aware zoom: distant layers (low parallax) zoom less
                    float depthZoomFactor = lerp(1.0, zoomFactor, saturate(parallax * 10.0));

                    // Scale UVs around center point
                    float2 scaledUV = (uv - 0.5) * depthZoomFactor + 0.5;

                    // Apply parallax offset (pre-computed on CPU in double precision)
                    float2 parallaxUV = scaledUV + _DepthParallaxOffsets[d].xy;

                    // Warp star density reduction — parallax-aware per depth
                    float depthFadeFactor = lerp(_WarpStarFadeMinDepth, 1.0,
                        lerp(saturate(1.0 - parallax * _WarpParallaxMultiplier),
                             saturate(parallax * _WarpParallaxMultiplier),
                             _WarpStarFadeNearBias));
                    float warpSpawnChance = _SpawnChance * (1.0 - _WarpStarFade * depthFadeFactor);

                    // Generate stars at this depth
                    float3 depthStars = starsAtDepth(parallaxUV, density, sizeMin, sizeMax, seed, depthColor, warpSpawnChance);

                    result += depthStars;
                }

                // === World Fabric modulation ===
                // Separate stars from background for fabric processing
                float3 bgColor = (_RenderBackground > 0.5) ? _BackgroundColor.rgb : float3(0, 0, 0);
                float3 starValue = result - bgColor;

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

                if (_RenderBackground > 0.5)
                {
                    // Void darkens background
                    float bgDim = 1.0 - _FabricVoidFactor * _FabricVoidBgDarken;
                    float3 finalColor = bgColor * bgDim + starValue;
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

    Fallback "Hidden/InternalErrorShader"
}
