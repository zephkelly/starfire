Shader "Starfire/GravitationalWake"
{
    Properties
    {
        _MainTex ("Source Texture", 2D) = "white" {}

        [Header(Debug Overrides)]
        [Toggle] _UseDebugValues ("Use Debug Values", Float) = 0
        _DebugIntensity ("Debug Intensity", Range(0, 1)) = 0.5
        _DebugDirectionX ("Debug Direction X", Range(-1, 1)) = 0
        _DebugDirectionY ("Debug Direction Y", Range(-1, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "GravitationalWake"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            // Global properties (set by WarpEffectController)
            float _WarpIntensity;
            float2 _WarpDirection;

            // Wake-specific globals
            float _WakeBubbleRadius;
            float _WakeRingWidth;
            float _WakeTrailLength;
            float _WakeDistortionStrength;
            float _WakeTrailFalloff;
            float _WakeDirectionalBias;
            float _WakeChromaStrength;
            float2 _WakeCenterPosition; // Ship's screen position (0-1), set by CameraController

            // Debug properties
            float _UseDebugValues;
            float _DebugIntensity;
            float _DebugDirectionX;
            float _DebugDirectionY;

            // Constants
            #define PI 3.14159265359

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // Get intensity and direction (use debug values if enabled)
                float intensity = _UseDebugValues > 0.5 ? _DebugIntensity : _WarpIntensity;
                float2 warpDir = _UseDebugValues > 0.5
                    ? normalize(float2(_DebugDirectionX, _DebugDirectionY))
                    : _WarpDirection;

                // Early exit if no warp effect
                [branch]
                if (intensity < 0.001)
                {
                    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                }

                // Get wake parameters (use defaults if globals not set)
                float bubbleRadius = max(_WakeBubbleRadius, 0.05);
                float ringWidth = max(_WakeRingWidth, 0.1);
                float trailLength = max(_WakeTrailLength, 0.2);
                float distortStrength = _WakeDistortionStrength;
                float trailFalloff = max(_WakeTrailFalloff, 0.5);
                float directionalBias = _WakeDirectionalBias;
                float chromaStrength = _WakeChromaStrength;

                // Get ship center position (default to screen center if not set)
                float2 shipCenter = _WakeCenterPosition;
                if (shipCenter.x == 0 && shipCenter.y == 0)
                {
                    shipCenter = float2(0.5, 0.5);
                }

                // Center-relative coordinates based on ship position
                float2 centerUV = uv - shipCenter;

                // Aspect ratio correction
                float aspectRatio = _ScreenParams.x / _ScreenParams.y;
                float2 aspectCorrected = centerUV * float2(aspectRatio, 1.0);

                // Distance from center
                float distFromCenter = length(aspectCorrected);

                // Avoid division by zero
                float2 dirFromCenter = distFromCenter > 0.001
                    ? aspectCorrected / distFromCenter
                    : float2(0, 1);

                // === BUBBLE ZONE MASK ===
                // Inner zone is undistorted (ship cockpit area)
                float bubbleMask = smoothstep(bubbleRadius, bubbleRadius + ringWidth * 0.5, distFromCenter);

                // Outer falloff (effect fades at screen edges)
                float outerMask = 1.0 - smoothstep(0.5, 0.8, distFromCenter);

                // === DIRECTIONAL WAKE ===
                // How much this pixel is "behind" the ship (in wake zone)
                // dot product: positive = ahead, negative = behind
                float directionDot = dot(dirFromCenter, warpDir);

                // Wake is strongest directly behind (-1), fades to sides (0), minimal ahead (+1)
                // Remap from [-1, 1] to [1, 0]
                float wakeFactor = saturate((1.0 - directionDot) * 0.5);

                // Apply directional bias (higher = more focused behind ship)
                wakeFactor = pow(wakeFactor, max(directionalBias, 0.1));

                // Trail length falloff (wake fades with distance behind ship)
                float trailMask = 1.0 - smoothstep(0.0, trailLength, distFromCenter * (1.0 - wakeFactor * 0.5));
                trailMask = pow(trailMask, trailFalloff);

                // === COMBINE MASKS ===
                float distortionMask = bubbleMask * outerMask * lerp(0.3, 1.0, wakeFactor) * trailMask * intensity;

                // === GRAVITATIONAL LENSING DISTORTION ===
                // Ring-shaped distortion profile (like light bending around a mass)
                float ringDist = abs(distFromCenter - bubbleRadius - ringWidth * 0.5);
                float ringProfile = 1.0 - smoothstep(0.0, ringWidth, ringDist);

                // Radial distortion - pulls pixels toward center (compression effect)
                float2 radialOffset = -dirFromCenter * ringProfile * distortStrength;

                // Tangential swirl for wake turbulence (subtle)
                float2 tangentDir = float2(-dirFromCenter.y, dirFromCenter.x);
                float2 tangentialOffset = tangentDir * ringProfile * distortStrength * 0.2 * wakeFactor;

                // Wake-direction bias - additional pull in wake direction
                float2 wakeOffset = -warpDir * wakeFactor * distortStrength * 0.4;

                // Final UV offset
                float2 totalOffset = (radialOffset + tangentialOffset + wakeOffset) * distortionMask;

                // Clamp offset to prevent sampling outside texture
                totalOffset = clamp(totalOffset, -0.1, 0.1);

                float2 distortedUV = uv + totalOffset;

                // Clamp UVs to valid range
                distortedUV = clamp(distortedUV, 0.001, 0.999);

                // Sample scene with distorted UVs
                half4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, distortedUV);

                // === CHROMATIC ABERRATION ===
                // Add subtle chromatic aberration in wake zone
                [branch]
                if (chromaStrength > 0.0001)
                {
                    float chromaOffset = distortionMask * chromaStrength;
                    float2 chromaDir = dirFromCenter;

                    float2 uvR = clamp(distortedUV + chromaDir * chromaOffset, 0.001, 0.999);
                    float2 uvB = clamp(distortedUV - chromaDir * chromaOffset, 0.001, 0.999);

                    color.r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvR).r;
                    color.b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvB).b;
                }

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
