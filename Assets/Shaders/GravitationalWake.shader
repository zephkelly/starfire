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
            float _WakeShipExclusionRadius; // Screen-space radius for ship exclusion (0-1), set by CameraController

            // Ellipse shape globals
            float _WakeEllipseRatio;
            float _WakeNeedleSharpness;

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

            // Edge distortion globals (event horizon effect)
            float _WakeEdgeStrength;
            float _WakeEdgeSharpness;

            // Wake zone turbulence globals
            float _WakeAngle;
            float _WakeTurbulence;
            float _WakeTurbulenceScale;
            float _WakeTurbulenceSpeed;
            float _WakeSpread;

            // Camera zoom scaling
            float _WakeOrthoSize; // Camera orthographic size for zoom scaling
            float _WakeReferenceOrthoSize; // Reference ortho size where config values are calibrated

            // Bubble interior distortion
            float _WakeBubbleInteriorDistortion;

            // Energy glow globals
            float _WakeEnergyGlowEnabled;
            float4 _WakeEnergyGlowColor;
            float _WakeEnergyGlowIntensity;
            float _WakeEnergyGlowWidth;
            float _WakeEnergyFlowSpeed;
            float _WakeEnergyFlowBands;

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

            // Calculate elliptical distance aligned with warp direction
            // majorAxis = direction of travel (stretched), minorAxis = perpendicular (compressed)
            float ellipticalDistance(float2 pos, float2 majorDir, float ellipseRatio, float needleSharpness)
            {
                // Get perpendicular direction for minor axis
                float2 minorDir = float2(-majorDir.y, majorDir.x);

                // Project position onto major and minor axes
                float majorComponent = dot(pos, majorDir);
                float minorComponent = dot(pos, minorDir);

                // Apply ellipse ratio to minor axis (squashes perpendicular to movement)
                float adjustedMinor = minorComponent / max(ellipseRatio, 0.2);

                // Needle sharpening: elongate the FRONT more than the back
                // Front is where majorComponent > 0 (ahead in warp direction)
                float frontFactor = saturate(majorComponent / max(length(pos), 0.001));
                float needleStretch = 1.0 + needleSharpness * frontFactor * 2.0;
                float adjustedMajor = majorComponent / needleStretch;

                return length(float2(adjustedMajor, adjustedMinor));
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

                // === ZOOM SCALING ===
                // Scale effect size based on camera zoom
                // Smaller ortho = zoomed in = effect should be larger on screen
                float refOrtho = max(_WakeReferenceOrthoSize, 1.0);
                float zoomScale = refOrtho / max(_WakeOrthoSize, 1.0);

                // Apply zoom scale to bubble/ring (ship-relative elements)
                bubbleRadius *= zoomScale;
                ringWidth *= zoomScale;
                // NOTE: trailLength NOT scaled - wake zone should always fill entire screen

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

                // Elliptical distance aligned with movement direction
                float ellipseRatio = max(_WakeEllipseRatio, 0.2);
                float needleSharp = _WakeNeedleSharpness;
                float distFromCenter = ellipticalDistance(aspectCorrected, warpDir, ellipseRatio, needleSharp);

                // Avoid division by zero
                float2 dirFromCenter = distFromCenter > 0.001
                    ? aspectCorrected / distFromCenter
                    : float2(0, 1);

                // === SHIP EXCLUSION ZONE ===
                // Skip distortion within the ship's screen-space footprint
                // Uses smooth transition to avoid hard edges
                float shipExclusionMask = smoothstep(
                    _WakeShipExclusionRadius * 0.9,  // Inner edge (full exclusion)
                    _WakeShipExclusionRadius * 1.1,  // Outer edge (blend to distortion)
                    distFromCenter
                );

                // === BUBBLE ZONE MASK ===
                // Inner zone is undistorted (ship cockpit area) unless bubbleInteriorDistortion is set
                float baseBubbleMask = smoothstep(bubbleRadius, bubbleRadius + ringWidth * 0.5, distFromCenter);
                float bubbleMask = lerp(baseBubbleMask, 1.0, _WakeBubbleInteriorDistortion);

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
                float distortionMask = bubbleMask * outerMask * lerp(0.3, 1.0, wakeFactor) * trailMask * intensity * shipExclusionMask;

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

                // === EXTREME EDGE DISTORTION (Event Horizon) ===
                // Sharp, intense distortion exactly at bubble boundary - like light bending around a black hole
                float edgeDist = abs(distFromCenter - bubbleRadius);
                float edgeProfile = exp(-edgeDist * _WakeEdgeSharpness / ringWidth);
                float2 edgeOffset = -dirFromCenter * edgeProfile * _WakeEdgeStrength * distortStrength * 3.0;

                // === WAKE ZONE (Full Cone Behind Ship) ===
                // Calculate if pixel is within wake cone angle
                float wakeAngleRad = _WakeAngle * PI / 180.0;
                float wakeConeMask = smoothstep(cos(wakeAngleRad), cos(wakeAngleRad * 0.5), -directionDot);

                // Wake only behind ship (where directionDot < 0)
                float behindShip = saturate(-directionDot);
                wakeConeMask *= behindShip;

                // Wake spreads and intensifies with distance from bubble
                float wakeDistance = max(distFromCenter - bubbleRadius, 0.0);
                float wakeDistanceFactor = saturate(wakeDistance / trailLength);
                float wakeZoneIntensity = wakeConeMask * (1.0 + wakeDistanceFactor * _WakeSpread);

                // === SWIRLING VORTEX TURBULENCE ===
                // Create chaotic swirling distortion filling the wake cone
                float2 wakeNoiseCoord = aspectCorrected * _WakeTurbulenceScale;
                float turbTime = _Time.y * _WakeTurbulenceSpeed;

                // Create swirling vortex pattern using curl noise approach
                // Calculate noise gradients for curl (perpendicular flow creates swirls)
                float noiseCenter = noise2D(wakeNoiseCoord + turbTime);
                float noiseRight = noise2D(wakeNoiseCoord + float2(0.01, 0) + turbTime);
                float noiseUp = noise2D(wakeNoiseCoord + float2(0, 0.01) + turbTime);

                // Curl gives perpendicular flow (creates swirling vortices)
                float2 curl = float2(noiseUp - noiseCenter, -(noiseRight - noiseCenter)) * 100.0;

                // Add second octave of vortices for more complex turbulence
                float2 curl2 = float2(
                    noise2D(wakeNoiseCoord * 2.0 + float2(0, 0.01) + turbTime * 1.5) - noise2D(wakeNoiseCoord * 2.0 + turbTime * 1.5),
                    -(noise2D(wakeNoiseCoord * 2.0 + float2(0.01, 0) + turbTime * 1.5) - noise2D(wakeNoiseCoord * 2.0 + turbTime * 1.5))
                ) * 50.0;

                // Combined swirling turbulence
                float2 vortexTurbulence = (curl + curl2 * 0.5);

                // Turbulence increases with distance from ship (wake spreads out)
                float spreadFactor = 1.0 + wakeDistanceFactor * _WakeSpread;
                float2 wakeDistortOffset = vortexTurbulence * _WakeTurbulence * wakeZoneIntensity * distortStrength * spreadFactor;

                // Add outward flow component in wake direction
                wakeDistortOffset += -warpDir * wakeZoneIntensity * distortStrength * 0.2;

                // Final UV offset combining all effects:
                // - Edge distortion (event horizon)
                // - Ring distortion (existing gravitational lensing)
                // - Wake zone turbulence (chaotic swirling in entire wake cone)
                // - Bow wave (front compression)
                // - Noise wobble (organic movement)
                float2 totalOffset = (edgeOffset + radialOffset + tangentialOffset + wakeOffset + bowOffset + noiseOffset + wakeDistortOffset) * distortionMask;

                // Allow stronger distortion for dramatic effect
                totalOffset = clamp(totalOffset, -0.15, 0.15);

                float2 distortedUV = uv + totalOffset;

                // === SHIP SAMPLE EXCLUSION ===
                // Check if the distorted UV would sample from the ship's area
                // If so, use the original UV to prevent ship from being refracted
                float2 distortedCenterUV = distortedUV - shipCenter;
                float2 distortedAspectCorrected = distortedCenterUV * float2(aspectRatio, 1.0);
                float distortedDistFromShip = length(distortedAspectCorrected);

                // If distorted UV falls within ship exclusion zone, blend back to original UV
                float sampleExclusionMask = smoothstep(
                    _WakeShipExclusionRadius * 0.85,  // Inner edge (full exclusion)
                    _WakeShipExclusionRadius * 1.0,   // Outer edge (allow distortion)
                    distortedDistFromShip
                );

                // Blend between original UV (ship area) and distorted UV (outside ship)
                distortedUV = lerp(uv, distortedUV, sampleExclusionMask);

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

                    float2 uvR = distortedUV + chromaDir * chromaOffset;
                    float2 uvB = distortedUV - chromaDir * chromaOffset;

                    // Check if chromatic UVs would sample from ship exclusion zone
                    float2 uvRCenter = uvR - shipCenter;
                    float uvRDist = length(uvRCenter * float2(aspectRatio, 1.0));
                    uvR = lerp(uv, uvR, smoothstep(_WakeShipExclusionRadius * 0.85, _WakeShipExclusionRadius, uvRDist));

                    float2 uvBCenter = uvB - shipCenter;
                    float uvBDist = length(uvBCenter * float2(aspectRatio, 1.0));
                    uvB = lerp(uv, uvB, smoothstep(_WakeShipExclusionRadius * 0.85, _WakeShipExclusionRadius, uvBDist));

                    uvR = clamp(uvR, 0.001, 0.999);
                    uvB = clamp(uvB, 0.001, 0.999);

                    color.r = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvR).r;
                    color.b = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uvB).b;
                }

                // === ENERGY GLOW ===
                // Add colored flowing glow at bubble edge
                [branch]
                if (_WakeEnergyGlowEnabled > 0.5 && intensity > 0.01)
                {
                    // Angle around bubble center for flow animation
                    float angle = atan2(dirFromCenter.y, dirFromCenter.x);

                    // Flowing energy bands - animate based on angle
                    float flowPhase = angle * _WakeEnergyFlowBands - _Time.y * _WakeEnergyFlowSpeed * TAU;
                    float energyFlow = sin(flowPhase) * 0.5 + 0.5;

                    // Add secondary wave for more organic feel
                    float secondaryPhase = angle * (_WakeEnergyFlowBands * 0.7) + _Time.y * _WakeEnergyFlowSpeed * TAU * 0.3;
                    energyFlow = energyFlow * 0.7 + (sin(secondaryPhase) * 0.5 + 0.5) * 0.3;

                    // Glow profile - strongest at bubble edge, fades outward
                    float glowDist = abs(distFromCenter - bubbleRadius);
                    float glowWidth = _WakeEnergyGlowWidth * ringWidth;
                    float glowProfile = exp(-glowDist * glowDist / (glowWidth * glowWidth * 0.5));

                    // Also add glow along the ring
                    float ringGlow = ringProfile * 0.5;

                    // Combine edge glow and ring glow
                    float totalGlow = max(glowProfile, ringGlow);

                    // Stronger glow in wake direction (behind ship)
                    float wakeGlow = lerp(0.4, 1.0, wakeFactor);

                    // Final glow color - mask out ship exclusion zone
                    float glowIntensity = totalGlow * energyFlow * wakeGlow * _WakeEnergyGlowIntensity * intensity * shipExclusionMask;
                    float3 glowColor = _WakeEnergyGlowColor.rgb * glowIntensity;

                    // Add glow to color (additive blending for HDR bloom support)
                    color.rgb += glowColor;
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
