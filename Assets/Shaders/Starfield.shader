Shader "Starfire/Starfield"
{
    Properties
    {
        [Header(Star Field)]
        _StarDensity ("Star Density", Range(1, 100)) = 20
        _SpawnChance ("Spawn Chance", Range(0, 1)) = 0.8
        _StarBrightness ("Star Brightness", Range(0.1, 2.0)) = 1.0

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

        [Header(Shape)]
        _EdgeSharpness ("Edge Sharpness", Range(0, 1)) = 0.0

        [Header(Background)]
        _BackgroundColor ("Background Color", Color) = (0, 0, 0.02, 1)
        [Min(0)] _ParallaxFactor ("Parallax Factor", Float) = 0.02

        [Header(Layer Mode)]
        [Toggle] _RenderBackground ("Render Background", Float) = 1

        [Header(Distribution)]
        _LayerSeed ("Layer Seed", Float) = 0
        _ClusterAmount ("Cluster Amount", Range(0, 1)) = 0.3
        _ClusterScale ("Cluster Scale", Float) = 0.05
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
            Name "Starfield"

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
                float _StarDensity;
                float _SpawnChance;
                float _StarBrightness;
                float _StarSizeMin;
                float _StarSizeMax;
                float _SizeDistribution;
                float _TwinkleSpeed;
                float _TwinkleAmount;
                float4 _StarColor;
                float _ColorVariation;
                float _WarmCoolMix;
                float _EdgeSharpness;
                float4 _BackgroundColor;
                float _ParallaxFactor;
                float _RenderBackground;
                float _LayerSeed;
                float _ClusterAmount;
                float _ClusterScale;
            CBUFFER_END

            // Set from script
            float2 _CameraWorldPos;
            float _ScreenAspect;

            // PCG-style hash functions - much longer period, no sin() periodicity issues
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

                // Quintic interpolation curve
                float2 u = f * f * f * (f * (f * 6.0 - 15.0) + 10.0);

                // Get gradients at corners
                float2 g00 = grad2(i + float2(0.0, 0.0));
                float2 g10 = grad2(i + float2(1.0, 0.0));
                float2 g01 = grad2(i + float2(0.0, 1.0));
                float2 g11 = grad2(i + float2(1.0, 1.0));

                // Compute dot products
                float n00 = dot(g00, f - float2(0.0, 0.0));
                float n10 = dot(g10, f - float2(1.0, 0.0));
                float n01 = dot(g01, f - float2(0.0, 1.0));
                float n11 = dot(g11, f - float2(1.0, 1.0));

                // Bilinear interpolation
                float nx0 = lerp(n00, n10, u.x);
                float nx1 = lerp(n01, n11, u.x);
                return lerp(nx0, nx1, u.y) * 0.5 + 0.5; // Normalize to 0-1
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

                // Each star is either warm OR cool based on warmCool probability
                // warmCool 0 = all warm, 1 = all cool, 0.5 = 50/50 mix
                float warmHue = lerp(0.02, 0.12, colorRandom);  // Red-orange to yellow-orange
                float coolHue = lerp(0.55, 0.68, colorRandom);  // Blue to blue-violet
                float hue = (colorChoice > warmCool) ? warmHue : coolHue;

                // Saturation varies per star
                float saturation = variation * lerp(0.3, 0.8, hash1(cellID + 350.0));

                float3 tintColor = hsv2rgb(float3(hue, saturation, 1.0));
                return baseColor * tintColor;
            }

            // Generate stars for a single layer
            float3 stars(float2 uv, float density, float sizeMin, float sizeMax, float sizeDistrib, float spawnChance, float twinkleSpeed, float twinkleAmount, float time, float aspect, float3 baseColor, float colorVariation, float warmCool, float edgeSharpness, float layerSeed, float clusterAmount, float clusterScale)
            {
                float3 result = float3(0, 0, 0);

                // Correct for aspect ratio to make grid cells square
                float2 aspectCorrectedUV = float2(uv.x * aspect, uv.y);

                // Apply layer seed offset to break up alignment between layers
                // Use hash to generate a unique large offset per layer seed
                float2 layerOffset = hash2(float2(layerSeed * 127.1, layerSeed * 311.7)) * 1000.0;
                float2 offsetUV = aspectCorrectedUV + layerOffset;

                // Scale UV by density to create grid
                float2 gridUV = offsetUV * density;

                // Get grid cell ID
                float2 cellID = floor(gridUV);

                // Get position within cell (0-1)
                float2 cellUV = frac(gridUV);

                // Check current cell and neighbors (for stars near edges)
                for (int x = -1; x <= 1; x++)
                {
                    for (int y = -1; y <= 1; y++)
                    {
                        float2 neighborCell = cellID + float2(x, y);

                        // Calculate cluster noise for natural density variation
                        float clusterNoise = perlin2D(neighborCell * clusterScale);
                        // Modulate spawn chance: clusterAmount controls how much noise affects distribution
                        // At clusterAmount=0: uniform distribution (original spawnChance)
                        // At clusterAmount=1: heavily clustered (spawnChance varies 0.2x to 1.0x based on noise)
                        float clusterModifier = lerp(1.0, 0.2 + clusterNoise * 0.8, clusterAmount);
                        float adjustedSpawnChance = spawnChance * clusterModifier;

                        // Spawn chance - skip some cells entirely
                        float spawnRoll = hash1(neighborCell + 200.0);
                        if (spawnRoll > adjustedSpawnChance) continue;

                        // Random position for star within this cell
                        float2 starPos = hash2(neighborCell);

                        // Random brightness variation
                        float brightness = hash1(neighborCell + 100.0);
                        brightness = 0.3 + brightness * 0.7; // Range 0.3 to 1.0

                        // Twinkle - per-star phase and intensity for varied flickering
                        float twinklePhase = hash1(neighborCell + 150.0) * 6.28318;
                        float twinkleIntensity = hash1(neighborCell + 175.0); // Some stars twinkle more
                        float twinkleWave = sin(time * twinkleSpeed + twinklePhase);
                        // Range from 0.1 to 1.0 so stars don't fully disappear
                        float twinkleBrightness = 0.1 + (twinkleWave * 0.5 + 0.5) * 0.9;
                        brightness *= lerp(1.0, twinkleBrightness, twinkleAmount * twinkleIntensity);

                        // Distance from current UV to star position
                        float2 toStar = (cellUV - float2(x, y)) - starPos;
                        float dist = length(toStar);

                        // Size with distribution curve (higher distribution = more small stars)
                        float sizeRandom = hash1(neighborCell + 50.0);
                        float curvedRandom = pow(sizeRandom, 1.0 + sizeDistrib * 3.0);
                        float starRadius = lerp(sizeMin, sizeMax, curvedRandom);

                        // Create star with adjustable edge sharpness
                        // sharpness 0 = soft glow (full falloff), 1 = crisp edge (minimal falloff)
                        float falloffWidth = starRadius * lerp(1.0, 0.1, edgeSharpness);
                        float falloffStart = starRadius - falloffWidth;
                        float star = 1.0 - smoothstep(falloffStart, starRadius, dist);

                        // Get per-star color
                        float3 starColor = getStarColor(neighborCell, baseColor, colorVariation, warmCool);

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

                // Apply parallax offset based on camera position
                float2 parallaxOffset = _CameraWorldPos * _ParallaxFactor;
                float2 parallaxUV = uv + parallaxOffset;

                // Generate starfield with parallax, twinkling, color, sharpness, and natural distribution
                float3 starValue = stars(parallaxUV, _StarDensity, _StarSizeMin, _StarSizeMax, _SizeDistribution, _SpawnChance, _TwinkleSpeed, _TwinkleAmount, _Time.y, _ScreenAspect, _StarColor.rgb, _ColorVariation, _WarmCoolMix, _EdgeSharpness, _LayerSeed, _ClusterAmount, _ClusterScale);

                // Apply brightness
                starValue *= _StarBrightness;

                // Calculate star luminance for alpha
                float starAlpha = saturate(dot(starValue, float3(0.299, 0.587, 0.114)) * 2.0);

                // Combine with background or output transparent
                if (_RenderBackground > 0.5)
                {
                    // First layer: render background + stars, fully opaque
                    float3 finalColor = _BackgroundColor.rgb + starValue;
                    return half4(finalColor, 1.0);
                }
                else
                {
                    // Additional layers: stars only, additive with alpha
                    return half4(starValue, starAlpha);
                }
            }

            ENDHLSL
        }
    }
}
