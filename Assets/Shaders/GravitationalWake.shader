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

        [Header(Debug Visualization)]
        [Toggle] _ShowDebugOverlay ("Show Center Marker", Float) = 0
        _DebugMarkerColor ("Marker Color", Color) = (1, 0, 1, 1)
        _DebugMarkerSize ("Marker Size", Range(0.001, 0.05)) = 0.01
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

            // Animation globals
            float _WakePulseSpeed;
            float _WakePulseAmount;
            float _WakeRippleCount;
            float _WakeRippleSpeed;
            float _WakeRippleStrength;
            float _WakeNoiseScale;
            float _WakeNoiseSpeed;
            float _WakeNoiseStrength;
            float _WakeBowWaveStrength;

            // Debug properties
            float _UseDebugValues;
            float _DebugIntensity;
            float _DebugDirectionX;
            float _DebugDirectionY;

            // Debug visualization
            float _ShowDebugOverlay;
            float4 _DebugMarkerColor;
            float _DebugMarkerSize;

            // Constants
            #define PI 3.14159265359
            #define TAU 6.28318530718

            // Simple 2D hash-based noise for wobble effect
            float2 hash22(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * float3(0.1031, 0.1030, 0.0973));
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.xx + p3.yz) * p3.zy) * 2.0 - 1.0;
            }

            float noise2D(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);
                float2 u = f * f * (3.0 - 2.0 * f); // smoothstep interpolation

                return lerp(
                    lerp(dot(hash22(i + float2(0, 0)), f - float2(0, 0)),
                         dot(hash22(i + float2(1, 0)), f - float2(1, 0)), u.x),
                    lerp(dot(hash22(i + float2(0, 1)), f - float2(0, 1)),
                         dot(hash22(i + float2(1, 1)), f - float2(1, 1)), u.x),
                    u.y
                );
            }

            half4 Frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // Get intensity and direction (use debug values if enabled)
                float intensity = _UseDebugValues > 0.5 ? _DebugIntensity : _WarpIntensity;
                float2 warpDir = _UseDebugValues > 0.5
                    ? normalize(float2(_DebugDirectionX, _DebugDirectionY))
                    : _WarpDirection;

                // Early exit if no warp effect (but not if debug overlay is enabled)
                [branch]
                if (intensity < 0.001 && _ShowDebugOverlay < 0.5)
                {
                    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                }

                // Get wake parameters (use defaults if globals not set)
                // === BREATHING/PULSE ANIMATION ===
                // Animate bubble radius with a breathing effect
                float pulsePhase = sin(_Time.y * _WakePulseSpeed);
                float pulseOffset = pulsePhase * _WakePulseAmount;
                float bubbleRadius = max(_WakeBubbleRadius, 0.05) * (1.0 + pulseOffset);
                // Ring width pulses inversely (slightly) for more organic feel
                float ringWidth = max(_WakeRingWidth, 0.1) * (1.0 - pulseOffset * 0.3);
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

                // === BOW WAVE (PIERCING EFFECT) ===
                // Compression effect at front of ship - space being pushed aside
                // bowFactor is inverse of wakeFactor: strongest ahead, weakest behind
                float bowFactor = 1.0 - wakeFactor;
                bowFactor = pow(bowFactor, 2.0); // Concentrate at front
                float bowStrength = _WakeBowWaveStrength;

                // Trail length falloff (wake fades with distance behind ship)
                float trailMask = 1.0 - smoothstep(0.0, trailLength, distFromCenter * (1.0 - wakeFactor * 0.5));
                trailMask = pow(trailMask, trailFalloff);

                // === COMBINE MASKS ===
                float distortionMask = bubbleMask * outerMask * lerp(0.3, 1.0, wakeFactor) * trailMask * intensity;

                // === GRAVITATIONAL LENSING DISTORTION ===
                // Ring-shaped distortion profile (like light bending around a mass)
                float ringDist = abs(distFromCenter - bubbleRadius - ringWidth * 0.5);
                float ringProfile = 1.0 - smoothstep(0.0, ringWidth, ringDist);

                // === RIPPLE ANIMATION ===
                // Concentric waves flowing outward from bubble edge
                float ripplePhase = (distFromCenter * _WakeRippleCount - _Time.y * _WakeRippleSpeed) * TAU;
                float rippleWave = sin(ripplePhase) * _WakeRippleStrength;
                // Ripples are strongest in the ring zone, fade outside
                float rippleMask = ringProfile * (1.0 - smoothstep(0.0, trailLength * 0.5, distFromCenter - bubbleRadius));
                ringProfile *= (1.0 + rippleWave * rippleMask);

                // Radial distortion - pulls pixels toward center (compression effect)
                float2 radialOffset = -dirFromCenter * ringProfile * distortStrength;

                // Tangential swirl for wake turbulence (subtle)
                float2 tangentDir = float2(-dirFromCenter.y, dirFromCenter.x);
                float2 tangentialOffset = tangentDir * ringProfile * distortStrength * 0.2 * wakeFactor;

                // === NOISE WOBBLE ===
                // Organic turbulence using procedural noise
                float2 noiseCoord = aspectCorrected * _WakeNoiseScale + _Time.y * _WakeNoiseSpeed;
                float2 noiseOffset = float2(
                    noise2D(noiseCoord),
                    noise2D(noiseCoord + float2(100.0, 50.0))
                ) * _WakeNoiseStrength * ringProfile * distortStrength * 0.5;

                // Wake-direction bias - additional pull in wake direction
                float2 wakeOffset = -warpDir * wakeFactor * distortStrength * 0.4;

                // === BOW WAVE OFFSET ===
                // Pulls space inward at front of ship (piercing through spacetime)
                float2 bowOffset = dirFromCenter * bowFactor * bowStrength * distortStrength * ringProfile;

                // Final UV offset with all components
                float2 totalOffset = (radialOffset + tangentialOffset + wakeOffset + bowOffset + noiseOffset) * distortionMask;

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

                // === DEBUG VISUALIZATION ===
                [branch]
                if (_ShowDebugOverlay > 0.5)
                {
                    float markerSize = _DebugMarkerSize;
                    float lineWidth = markerSize * 0.15;

                    // Crosshair at center
                    float crossH = step(abs(centerUV.y), lineWidth) * step(abs(centerUV.x), markerSize * 3.0);
                    float crossV = step(abs(centerUV.x / aspectRatio), lineWidth) * step(abs(centerUV.y), markerSize * 3.0);
                    float crosshair = max(crossH, crossV);

                    // Circle showing bubble radius
                    float bubbleRing = abs(distFromCenter - bubbleRadius);
                    float bubbleCircle = 1.0 - smoothstep(0.0, lineWidth * 0.5, bubbleRing);

                    // Direction arrow (line from center in warp direction)
                    float2 arrowEnd = shipCenter + warpDir * markerSize * 5.0 / float2(aspectRatio, 1.0);
                    float2 toArrow = uv - shipCenter;
                    float alongArrow = dot(toArrow * float2(aspectRatio, 1.0), warpDir);
                    float perpArrow = abs(dot(toArrow * float2(aspectRatio, 1.0), float2(-warpDir.y, warpDir.x)));
                    float arrowLine = step(perpArrow, lineWidth) * step(0.0, alongArrow) * step(alongArrow, markerSize * 4.0);

                    // Combine debug elements
                    float debugMask = saturate(crosshair + bubbleCircle * 0.7 + arrowLine * 0.5);
                    color.rgb = lerp(color.rgb, _DebugMarkerColor.rgb, debugMask * _DebugMarkerColor.a);
                }

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
