Shader "Starfire/FluidVisualization"
{
    Properties
    {
        [Header(Color Gradient)]
        _Color1 ("Low Density Color", Color) = (0, 0, 0, 0)
        _Color2 ("Mid Density Color", Color) = (0.2, 0.4, 0.8, 0.3)
        _Color3 ("High Density Color", Color) = (0.5, 0.8, 1.0, 0.6)
        _EmissionIntensity ("Emission Intensity", Range(0, 3)) = 1.0

        [Header(Velocity Visualization)]
        [Toggle] _ShowVelocity ("Show Velocity", Float) = 0
        _VelocityScale ("Velocity Color Scale", Range(0, 10)) = 1.0

        [Header(Blending)]
        _DensityMultiplier ("Density Multiplier", Range(0, 5)) = 1.0
        _AlphaMultiplier ("Alpha Multiplier", Range(0, 2)) = 1.0
        _AlphaThreshold ("Alpha Threshold", Range(0, 0.5)) = 0.15

        [Header(Textures)]
        _DensityTexture ("Density Texture", 2D) = "black" {}
        _VelocityTexture ("Velocity Texture", 2D) = "black" {}

        [Header(Visual Noise)]
        _VisualNoiseScale ("Visual Noise Scale", Range(0.01, 1)) = 0.15
        _VisualNoiseContrast ("Visual Noise Contrast", Range(0.1, 3)) = 1.5
        _VisualNoiseOctaves ("Visual Noise Octaves", Range(1, 6)) = 4
        _VisualNoisePersistence ("Visual Noise Persistence", Range(0.3, 0.7)) = 0.5
        _VisualNoiseSeed ("Visual Noise Seed", Vector) = (0, 0, 0, 0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "FluidVisualization"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Textures
            TEXTURE2D(_DensityTexture);
            TEXTURE2D(_VelocityTexture);
            SAMPLER(sampler_DensityTexture);
            SAMPLER(sampler_VelocityTexture);

            CBUFFER_START(UnityPerMaterial)
                float4 _Color1;
                float4 _Color2;
                float4 _Color3;
                float _EmissionIntensity;
                float _ShowVelocity;
                float _VelocityScale;
                float _DensityMultiplier;
                float _AlphaMultiplier;
                float _AlphaThreshold;
                float _VisualNoiseScale;
                float _VisualNoiseContrast;
                float _VisualNoiseOctaves;
                float _VisualNoisePersistence;
                float2 _VisualNoiseSeed;
            CBUFFER_END

            // Simulation bounds (set from C#)
            float2 _SimulationCenter;
            float2 _SimulationSize;

            // Global camera properties (set by StarfieldManager or similar)
            float2 _CameraWorldPos;
            float _CameraOrthoSize;
            float _ScreenAspect;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // ============================================================================
            // Noise Functions for Per-Pixel Visual Detail
            // ============================================================================

            // Hash function for noise generation
            float hash(float2 p)
            {
                p = frac(p * float2(443.897, 441.423));
                p += dot(p, p.yx + 19.19);
                return frac((p.x + p.y) * p.x);
            }

            // Value noise with smooth interpolation
            float valueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // Smooth interpolation (smoothstep)
                float2 u = f * f * (3.0 - 2.0 * f);

                // Sample corners
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));

                // Bilinear interpolation
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal Brownian Motion noise - multiple octaves for natural appearance
            float fbmNoise(float2 p, int octaves, float persistence)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;
                float maxValue = 0.0;

                for (int i = 0; i < octaves; i++)
                {
                    value += amplitude * valueNoise(p * frequency);
                    maxValue += amplitude;
                    amplitude *= persistence;
                    frequency *= 2.0;
                }

                return value / maxValue;  // Normalize to 0-1
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_TRANSFER_INSTANCE_ID(IN, OUT);

                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;

                // Calculate world position from actual quad vertex (works for both screen-space and world-space modes)
                float3 worldPos3 = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldPos = worldPos3.xy;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);

                // Convert world position to simulation UV
                float2 simUV = (IN.worldPos - _SimulationCenter) / _SimulationSize + 0.5;

                // Check if within simulation bounds
                if (any(simUV < 0.0) || any(simUV > 1.0))
                {
                    return half4(0, 0, 0, 0);
                }

                // Sample simulation density (the "mask" showing where gas exists)
                float simDensity = SAMPLE_TEXTURE2D(_DensityTexture, sampler_DensityTexture, simUV).r;

                // Generate per-pixel visual noise at world position (infinite detail!)
                float2 noisePos = (IN.worldPos + _VisualNoiseSeed) * _VisualNoiseScale;
                float visualNoise = fbmNoise(noisePos, (int)_VisualNoiseOctaves, _VisualNoisePersistence);

                // Apply contrast to noise (controls how "holey" the gas looks)
                // Higher contrast = more pronounced holes and wisps
                visualNoise = pow(visualNoise, _VisualNoiseContrast);

                // Combine simulation density (mask) with visual noise
                // simDensity controls WHERE gas exists, visualNoise adds organic variation
                float density = simDensity * visualNoise * _DensityMultiplier;
                density = saturate(density);

                // Three-color gradient
                float3 color;
                if (density < 0.5)
                {
                    color = lerp(_Color1.rgb, _Color2.rgb, density * 2.0);
                }
                else
                {
                    color = lerp(_Color2.rgb, _Color3.rgb, (density - 0.5) * 2.0);
                }

                // Alpha from gradient colors with threshold for transparency
                // Densities below threshold become fully transparent
                float alpha;
                float remappedDensity = saturate((density - _AlphaThreshold) / (1.0 - _AlphaThreshold));

                if (remappedDensity <= 0)
                {
                    alpha = 0;
                }
                else if (remappedDensity < 0.5)
                {
                    alpha = lerp(_Color1.a, _Color2.a, remappedDensity * 2.0);
                }
                else
                {
                    alpha = lerp(_Color2.a, _Color3.a, (remappedDensity - 0.5) * 2.0);
                }

                // Optional velocity visualization overlay
                if (_ShowVelocity > 0.5)
                {
                    float2 vel = SAMPLE_TEXTURE2D(_VelocityTexture, sampler_VelocityTexture, simUV).rg;
                    float velMag = length(vel) * _VelocityScale;

                    // Color-code velocity (red = x, green = y, blue = magnitude)
                    float3 velColor = float3(
                        abs(vel.x) * _VelocityScale,
                        abs(vel.y) * _VelocityScale,
                        velMag
                    );
                    velColor = saturate(velColor);

                    color = lerp(color, velColor, saturate(velMag * 0.5));
                    alpha = max(alpha, velMag * 0.3);
                }

                // Apply emission
                color *= _EmissionIntensity;

                // Apply alpha multiplier
                alpha *= _AlphaMultiplier;

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
