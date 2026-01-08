Shader "Starfire/Comet"
{
    Properties
    {
        [Header(Appearance)]
        _CometColor ("Comet Color", Color) = (0.8, 0.9, 1, 1)
        _Brightness ("Brightness", Range(0.5, 5.0)) = 2.0

        [Header(Nucleus)]
        _NucleusSize ("Nucleus Size", Float) = 0.15
        _ComaSize ("Coma Size", Float) = 0.6
        _ComaSoftness ("Coma Softness", Range(0.5, 5.0)) = 2.0

        [Header(Particle Trail)]
        _ParticleCount ("Particle Count", Int) = 24
        _ParticleSizeMin ("Particle Size Min", Float) = 0.02
        _ParticleSizeMax ("Particle Size Max", Float) = 0.08
        _ParticleSpread ("Particle Spread", Float) = 0.5
        _ParticleFadeRate ("Particle Fade Rate", Range(0.5, 5.0)) = 2.0
        _ReferenceSpeed ("Reference Speed", Float) = 8.0

        [Header(Background)]
        _ParallaxFactor ("Parallax Factor", Float) = 0.02
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
            Name "Comet"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend One One // Additive blending

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Maximum number of simultaneous comets
            #define MAX_COMETS 4
            #define MAX_PARTICLES 48

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
                float4 _CometColor;
                float _Brightness;
                float _NucleusSize;
                float _ComaSize;
                float _ComaSoftness;
                int _ParticleCount;
                float _ParticleSizeMin;
                float _ParticleSizeMax;
                float _ParticleSpread;
                float _ParticleFadeRate;
                float _ReferenceSpeed;
                float _ParallaxFactor;
            CBUFFER_END

            // Set from script - camera data
            float2 _CameraWorldPos;
            float _ScreenAspect;
            float _CameraOrthoSize;
            float _ReferenceZoom;

            // Set from script - comet data
            int _ActiveCometCount;
            float4 _CometPositions[MAX_COMETS];    // xy = head position, zw = tail position (world space)
            float4 _CometParams1[MAX_COMETS];      // x = brightness, y = progress (0-1), z = nucleusSize, w = comaSize
            float4 _CometParams2[MAX_COMETS];      // x = particleSeed, y = speed, zw = unused

            // Hash functions for deterministic pseudo-random values
            float hash1(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float hash1v2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
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
                // Convert UV to world space with parallax
                float2 uv = IN.uv;

                // Calculate world position from UV
                float2 centeredUV = (uv - 0.5) * 2.0; // -1 to 1

                // Depth-aware zoom: distant layers (low parallax) zoom less, nearby layers zoom more
                // This matches the Starfield shader's behavior
                float zoomFactor = _CameraOrthoSize / _ReferenceZoom;
                float depthZoomFactor = lerp(1.0, zoomFactor, saturate(_ParallaxFactor * 10.0));
                float effectiveOrthoSize = _ReferenceZoom * depthZoomFactor;

                // Parallax coordinate space with depth-aware zoom
                // - UV scaled by effectiveOrthoSize gives screen-space coordinates
                // - Camera position scaled by parallax creates parallax movement effect
                // - Low parallax = camera offset barely changes = distant layer effect
                float2 worldPos;
                worldPos.x = centeredUV.x * effectiveOrthoSize * _ScreenAspect + _CameraWorldPos.x * _ParallaxFactor;
                worldPos.y = centeredUV.y * effectiveOrthoSize + _CameraWorldPos.y * _ParallaxFactor;

                float3 result = float3(0, 0, 0);

                // Check each active comet
                for (int i = 0; i < _ActiveCometCount && i < MAX_COMETS; i++)
                {
                    // Positions scaled by parallax (same coordinate space as worldPos)
                    float2 headPos = _CometPositions[i].xy * _ParallaxFactor;
                    float2 tailPos = _CometPositions[i].zw * _ParallaxFactor;
                    float cometBrightness = _CometParams1[i].x;
                    float progress = _CometParams1[i].y;
                    // Sizes are screen-relative: percentage of effectiveOrthoSize (matches coordinate space)
                    float nucleusSize = _CometParams1[i].z * effectiveOrthoSize;
                    float comaSize = _CometParams1[i].w * effectiveOrthoSize;
                    float particleSeed = _CometParams2[i].x;
                    float cometSpeed = _CometParams2[i].y;

                    // Calculate trail direction
                    float2 trailDir = normalize(headPos - tailPos);
                    float trailLength = length(headPos - tailPos);

                    // Speed-based particle spread scaling (faster = wider spread)
                    float speedSpreadFactor = cometSpeed / _ReferenceSpeed;

                    // ===== NUCLEUS (bright core) =====
                    float nucleusDist = length(worldPos - headPos);
                    float nucleus = 1.0 - smoothstep(0.0, nucleusSize, nucleusDist);
                    nucleus = pow(nucleus, 1.5) * 3.0; // Intensify the core

                    // ===== COMA (fuzzy halo) =====
                    float coma = 1.0 - smoothstep(0.0, comaSize, nucleusDist);
                    coma = pow(coma, _ComaSoftness) * 0.5;

                    float headIntensity = nucleus + coma;

                    // ===== PARTICLE TRAIL =====
                    float particleIntensity = 0.0;
                    int particleCountClamped = min(_ParticleCount, MAX_PARTICLES);

                    for (int p = 0; p < particleCountClamped; p++)
                    {
                        // t goes from 0 (at tail) to 1 (at head)
                        float t = float(p) / float(particleCountClamped);

                        // Base position along trail line
                        float2 basePos = lerp(tailPos, headPos, t);

                        // Offset using hash for spread (wider near head, tighter at tail)
                        float spreadFactor = t; // More spread near head
                        float hashX = hash1(particleSeed + float(p) * 17.0) - 0.5;
                        float hashY = hash1(particleSeed + float(p) * 31.0) - 0.5;

                        // Dynamic spread based on speed (faster = wider spread)
                        // Spread is screen-relative (matches coordinate space)
                        float dynamicSpread = _ParticleSpread * speedSpreadFactor * effectiveOrthoSize;

                        // Perpendicular offset for spread
                        float2 perpDir = float2(-trailDir.y, trailDir.x);
                        float2 offset = perpDir * hashX * dynamicSpread * spreadFactor;
                        // Also add some along-trail variation
                        offset += trailDir * hashY * dynamicSpread * 0.3 * spreadFactor;

                        float2 particlePos = basePos + offset;

                        // Particle size (varies randomly, smaller further from head)
                        // Size is screen-relative (matches coordinate space)
                        float sizeRand = hash1(particleSeed + float(p) * 7.0);
                        float size = lerp(_ParticleSizeMin, _ParticleSizeMax, sizeRand * t) * effectiveOrthoSize;

                        // Particle brightness (dimmer further from head, using pow for curve)
                        float particleBrightness = pow(t, _ParticleFadeRate);

                        // Render particle as soft circle
                        float dist = length(worldPos - particlePos);
                        float particle = 1.0 - smoothstep(0.0, size, dist);

                        particleIntensity += particle * particleBrightness;
                    }

                    // Combine nucleus/coma and particles
                    float totalIntensity = (headIntensity + particleIntensity * 0.6) * cometBrightness;

                    // Fade in at start, fade out at end
                    float fadeIn = smoothstep(0.0, 0.1, progress);
                    float fadeOut = 1.0 - smoothstep(0.9, 1.0, progress);
                    totalIntensity *= fadeIn * fadeOut;

                    result += totalIntensity * _CometColor.rgb;
                }

                result *= _Brightness;

                // Calculate alpha from luminance
                float alpha = saturate(dot(result, float3(0.299, 0.587, 0.114)));

                return half4(result, alpha);
            }

            ENDHLSL
        }
    }
}
