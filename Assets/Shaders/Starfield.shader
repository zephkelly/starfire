Shader "Starfire/Starfield"
{
    Properties
    {
        [Header(Star Field)]
        _StarDensity ("Star Density", Range(1, 100)) = 20
        _SpawnChance ("Spawn Chance", Range(0, 1)) = 0.8
        _StarBrightness ("Star Brightness", Range(0.1, 2.0)) = 1.0

        [Header(Star Size)]
        _StarSizeMin ("Size Min", Range(0.01, 0.3)) = 0.02
        _StarSizeMax ("Size Max", Range(0.02, 0.5)) = 0.15
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
        _ParallaxFactor ("Parallax Factor", Range(0.001, 0.1)) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "Queue" = "Background"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Starfield"

            Cull Off
            ZWrite Off
            ZTest Always

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
            CBUFFER_END

            // Set from script
            float2 _CameraWorldPos;
            float _ScreenAspect;

            // Hash function for pseudo-random values
            float2 hash2(float2 p)
            {
                p = float2(dot(p, float2(127.1, 311.7)),
                           dot(p, float2(269.5, 183.3)));
                return frac(sin(p) * 43758.5453);
            }

            // Single value hash
            float hash1(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
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
            float3 stars(float2 uv, float density, float sizeMin, float sizeMax, float sizeDistrib, float spawnChance, float twinkleSpeed, float twinkleAmount, float time, float aspect, float3 baseColor, float colorVariation, float warmCool, float edgeSharpness)
            {
                float3 result = float3(0, 0, 0);

                // Correct for aspect ratio to make grid cells square
                float2 aspectCorrectedUV = float2(uv.x * aspect, uv.y);

                // Scale UV by density to create grid
                float2 gridUV = aspectCorrectedUV * density;

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

                        // Spawn chance - skip some cells entirely
                        float spawnRoll = hash1(neighborCell + 200.0);
                        if (spawnRoll > spawnChance) continue;

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

                // Generate starfield with parallax, twinkling, color, and sharpness
                float3 starValue = stars(parallaxUV, _StarDensity, _StarSizeMin, _StarSizeMax, _SizeDistribution, _SpawnChance, _TwinkleSpeed, _TwinkleAmount, _Time.y, _ScreenAspect, _StarColor.rgb, _ColorVariation, _WarmCoolMix, _EdgeSharpness);

                // Apply brightness
                starValue *= _StarBrightness;

                // Combine with background
                float3 finalColor = _BackgroundColor.rgb + starValue;

                return half4(finalColor, 1.0);
            }

            ENDHLSL
        }
    }
}
