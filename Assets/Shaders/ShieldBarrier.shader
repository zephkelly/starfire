Shader "Starfire/ShieldBarrier"
{
    Properties
    {
        [Header(Colors)]
        _ShieldColor ("Shield Color", Color) = (0.3, 0.6, 1, 0.7)
        _EdgeColor ("Edge Color", Color) = (0.5, 0.8, 1, 0.9)
        _HitFlashColor ("Hit Flash Color", Color) = (1, 1, 1, 1)
        _DamagedColor ("Damaged Color", Color) = (1, 0.3, 0.1, 1)

        [Header(Edge Glow)]
        _EdgeIntensity ("Edge Intensity", Range(0, 5)) = 2
        _EdgePower ("Edge Power", Range(0.5, 5)) = 2
        _EdgeThickness ("Edge Thickness", Range(0.05, 0.5)) = 0.12
        _EdgeDither ("Edge Dither", Range(0, 1)) = 0.0

        [Header(Pattern)]
        _PatternType ("Pattern Type", Int) = 2
        _PatternScale ("Pattern Scale", Range(0.5, 10)) = 3.0
        _PatternSpeed ("Pattern Speed", Float) = 0.3
        _PatternIntensity ("Pattern Intensity", Range(0, 1)) = 0.5

        [Header(Animation)]
        _PulseAmount ("Pulse Amount", Range(0, 1)) = 0.1
        _PulseSpeed ("Pulse Speed", Float) = 1

        [Header(Visibility)]
        _IdleOpacity ("Idle Opacity", Range(0, 1)) = 0.4
        _ActiveOpacity ("Active Opacity", Range(0, 1)) = 1.0
        _CurrentOpacity ("Current Opacity", Range(0, 1)) = 0.4

        [Header(Impact Ripples)]
        _RippleSpeed ("Ripple Speed", Float) = 3
        _RippleWidth ("Ripple Width", Range(0.01, 0.5)) = 0.1
        _RippleIntensity ("Ripple Intensity", Range(0, 5)) = 2
        _RippleDuration ("Ripple Duration", Float) = 0.5
        _RippleOpacity ("Ripple Opacity", Range(0, 1)) = 0.7
        _RippleSoftness ("Ripple Softness", Range(0, 1)) = 0.5
        _RipplePerspective ("Ripple Perspective", Range(0, 1)) = 0.4

        [Header(Impact Visibility)]
        _ImpactVisibilityRadius ("Impact Visibility Radius", Range(0.1, 3)) = 1.0
        _ImpactVisibilityFalloff ("Impact Visibility Falloff", Range(0.1, 5)) = 2.0
        _ImpactVisibilitySpeed ("Impact Visibility Speed", Range(0.2, 5)) = 1.0

        [Header(Shield State)]
        _ShieldHealth ("Shield Health (0-1)", Range(0, 1)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent-10"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }

        // Single pass for 2D shield barrier rendering
        Pass
        {
            Name "ShieldBarrier"

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_IMPACTS 8
            #define PI 3.14159265359
            #define SQRT3 1.7320508

            // Proper modulo that handles negative numbers (for seamless tiling)
            // HLSL fmod returns negative for negative inputs, breaking pattern tiling
            float2 mod2(float2 x, float2 y)
            {
                return x - y * floor(x / y);
            }

            // Impact data (set via MaterialPropertyBlock)
            float4 _ImpactPositions[MAX_IMPACTS]; // xy = local position, z = start time, w = unused
            int _ImpactCount;

            CBUFFER_START(UnityPerMaterial)
                float4 _ShieldColor;
                float4 _EdgeColor;
                float4 _HitFlashColor;
                float4 _DamagedColor;
                float _EdgeIntensity;
                float _EdgePower;
                float _EdgeThickness;
                float _EdgeDither;
                int _PatternType;
                float _PatternScale;
                float _PatternSpeed;
                float _PatternIntensity;
                float _PulseAmount;
                float _PulseSpeed;
                float _IdleOpacity;
                float _ActiveOpacity;
                float _CurrentOpacity;
                float _RippleSpeed;
                float _RippleWidth;
                float _RippleIntensity;
                float _RippleDuration;
                float _RippleOpacity;
                float _RippleSoftness;
                float _RipplePerspective;
                float _ImpactVisibilityRadius;
                float _ImpactVisibilityFalloff;
                float _ImpactVisibilitySpeed;
                float _ShieldHealth;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                float2 localPos : TEXCOORD3;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // Pattern functions
            float HexagonPattern(float2 uv, float scale)
            {
                float2 p = uv * scale;

                // Hexagon grid
                float2 r = float2(1, SQRT3);
                float2 h = r * 0.5;

                // Use mod2 instead of fmod to handle negative coordinates correctly
                float2 a = mod2(p, r) - h;
                float2 b = mod2(p - h, r) - h;

                float2 gv = length(a) < length(b) ? a : b;

                // Distance to hexagon edge
                float2 absGv = abs(gv);
                float hexDist = max(absGv.x * 0.5 + absGv.y * 0.866025, absGv.x);

                // INVERTED: Return 1 at hex edges (lines), 0 in centers (gaps)
                // This makes the hexagon grid lines glow bright
                return 1.0 - smoothstep(0.35, 0.5, hexDist);
            }

            float GridPattern(float2 uv, float scale)
            {
                float2 grid = abs(frac(uv * scale - 0.5) - 0.5);
                float lines = min(grid.x, grid.y);
                // INVERTED: Return 1 at grid lines, 0 in centers
                // This makes the grid lines glow bright
                return 1.0 - smoothstep(0.02, 0.08, lines);
            }

            float NoisePattern(float2 uv, float scale, float time)
            {
                float2 p = uv * scale + time * 0.1;
                float n = sin(p.x * 10.0 + sin(p.y * 10.0 + time));
                n += sin(p.y * 10.0 + sin(p.x * 10.0 - time * 0.7));
                return saturate(n * 0.5 + 0.5);
            }

            float CellsPattern(float2 uv, float scale, float time)
            {
                float2 p = uv * scale;

                // Animated cell centers
                float2 i = floor(p);
                float2 f = frac(p);

                float minDist = 1.0;
                for (int y = -1; y <= 1; y++)
                {
                    for (int x = -1; x <= 1; x++)
                    {
                        float2 neighbor = float2(x, y);
                        float2 cellCenter = neighbor + 0.5;
                        cellCenter += 0.3 * sin(time + (i + neighbor) * 1.7);

                        float dist = length(f - cellCenter);
                        minDist = min(minDist, dist);
                    }
                }

                return smoothstep(0.0, 0.3, minDist);
            }

            // Get pattern using LOCAL POSITION (not UV) for consistent world-scale patterns
            float GetPattern(float2 localPos, int patternType, float scale, float speed, float time)
            {
                float animatedTime = time * speed;

                // Use local position directly - this gives world-scale coordinates
                // that work regardless of edge thickness
                float2 p = localPos * scale;

                // 0 = None, 1 = Edge only, 2 = Hexagon, 3 = Grid, 4 = Noise, 5 = Cells
                if (patternType == 0 || patternType == 1)
                    return 0.0;
                else if (patternType == 2)
                    return HexagonPattern(p + float2(animatedTime * 0.1, 0), 1.0);
                else if (patternType == 3)
                    return GridPattern(p + float2(animatedTime * 0.1, 0), 1.0);
                else if (patternType == 4)
                    return NoisePattern(p, 1.0, animatedTime);
                else if (patternType == 5)
                    return CellsPattern(p, 1.0, animatedTime);

                return 0.0;
            }

            // Apply perspective distortion to make ripples appear to curve around the shield
            // This compresses the Y axis based on position to simulate 3D curvature
            float2 ApplyPerspective(float2 pos, float2 impactPos, float perspective)
            {
                // Direction from impact to current position
                float2 delta = pos - impactPos;

                // Calculate "depth" based on distance from shield center
                // Points at edges appear to curve away
                float edgeFactor = length(pos);

                // Compress Y axis more at edges to simulate curvature
                // This makes ripples appear elliptical/wrapped around the shield
                float yCompression = lerp(1.0, 0.5, edgeFactor * perspective);

                return float2(delta.x, delta.y * yCompression);
            }

            float CalculateImpactRipples(float2 localPos, float time)
            {
                float rippleContribution = 0.0;

                for (int i = 0; i < _ImpactCount && i < MAX_IMPACTS; i++)
                {
                    float2 impactPos = _ImpactPositions[i].xy;
                    float impactTime = _ImpactPositions[i].z;

                    float elapsed = time - impactTime;
                    if (elapsed < 0 || elapsed > _RippleDuration)
                        continue;

                    float normalizedTime = elapsed / _RippleDuration;
                    float rippleRadius = elapsed * _RippleSpeed;

                    // Apply perspective distortion for faux 3D effect
                    float2 perspectiveDelta = ApplyPerspective(localPos, impactPos, _RipplePerspective);
                    float dist = length(perspectiveDelta);

                    float rippleDelta = abs(dist - rippleRadius);

                    // === Multi-layer ripple for more natural look ===
                    // Primary wave - main visible ripple
                    float primaryWidth = _RippleWidth;
                    float primaryRipple = 1.0 - smoothstep(0.0, primaryWidth, rippleDelta);

                    // Soften edges based on softness parameter
                    // Higher softness = more gradient, less hard edge
                    float softEdge = lerp(0.02, _RippleWidth * 0.8, _RippleSoftness);
                    primaryRipple = smoothstep(primaryWidth + softEdge, primaryWidth * 0.3, rippleDelta);

                    // Secondary wave - trailing echo (softer, wider)
                    float secondaryRadius = rippleRadius * 0.7;
                    float secondaryDelta = abs(dist - secondaryRadius);
                    float secondaryWidth = _RippleWidth * 1.5;
                    float secondaryRipple = smoothstep(secondaryWidth, 0.0, secondaryDelta) * 0.3;

                    // Combine waves
                    float ripple = max(primaryRipple, secondaryRipple);

                    // Time-based fade - starts strong, fades out
                    float fade = 1.0 - normalizedTime;
                    fade = fade * fade; // Quadratic falloff for natural decay

                    // Perspective fade - ripples fade more at compressed edges
                    float perspectiveFade = lerp(1.0, 0.6, length(localPos) * _RipplePerspective);

                    rippleContribution += ripple * fade * perspectiveFade * _RippleIntensity * _RippleOpacity;
                }

                return saturate(rippleContribution);
            }

            // Calculate how visible this pixel should be based on proximity to active impacts
            // Creates an expanding shockwave effect - bright at impact, expands and fades quickly
            float CalculateImpactProximity(float2 localPos, float time)
            {
                float maxProximity = 0.0;

                for (int i = 0; i < _ImpactCount && i < MAX_IMPACTS; i++)
                {
                    float2 impactPos = _ImpactPositions[i].xy;
                    float impactTime = _ImpactPositions[i].z;

                    float elapsed = time - impactTime;
                    if (elapsed < 0 || elapsed > _RippleDuration)
                        continue;

                    // Speed multiplier affects how fast the effect progresses
                    // >1 = faster fade, <1 = slower fade
                    float normalizedTime = (elapsed / _RippleDuration) * _ImpactVisibilitySpeed;
                    normalizedTime = saturate(normalizedTime); // Clamp to 0-1 range

                    // Expanding radius - starts at 0, expands to ImpactVisibilityRadius
                    float expandingRadius = normalizedTime * _ImpactVisibilityRadius;

                    // Distance from impact point
                    float dist = length(localPos - impactPos);

                    // Visibility is high inside the expanding radius, fades at the edge
                    // The "shockwave" expands outward from the impact point
                    float insideWave = 1.0 - saturate((dist - expandingRadius * 0.3) / (expandingRadius * 0.7 + 0.01));

                    // Leading edge boost - brighter at the expanding front
                    float atLeadingEdge = 1.0 - saturate(abs(dist - expandingRadius) / (_RippleWidth * 2.0));

                    // Combine: inside the wave + extra brightness at leading edge
                    float proximity = max(insideWave * 0.5, atLeadingEdge);

                    // Aggressive time-based fade - cubic falloff for quick dissipation
                    float timeFade = 1.0 - normalizedTime;
                    timeFade = timeFade * timeFade * timeFade; // Cubic falloff (faster than quadratic)

                    // Apply falloff power for edge sharpness
                    proximity = pow(saturate(proximity), _ImpactVisibilityFalloff * 0.5);

                    maxProximity = max(maxProximity, proximity * timeFade);
                }

                return saturate(maxProximity);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.uv = IN.uv;
                OUT.localPos = IN.positionOS.xy;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float time = _Time.y;

                // Edge glow based on UV distance from center (works for 2D top-down)
                // UV center is (0.5, 0.5), edge is at distance 0.5 from center
                float distFromCenter = length(IN.uv - float2(0.5, 0.5)) * 2.0;

                // Calculate edge visibility - only outer portion is visible
                // edgeStart is where the visible edge begins (e.g., 0.88 for 12% thickness)
                float edgeStart = 1.0 - _EdgeThickness;

                // Dithering: add noise to the edge threshold for a scattered/stippled look
                // Uses screen-space position so pattern is stable during rotation
                float ditherNoise = 0.0;
                if (_EdgeDither > 0.001)
                {
                    // Screen-space position for stable dithering (doesn't rotate with ship)
                    float2 ditherPos = IN.positionCS.xy * 0.5;
                    ditherNoise = frac(sin(dot(ditherPos, float2(12.9898, 78.233))) * 43758.5453);
                    ditherNoise = (ditherNoise - 0.5) * 2.0; // Range -1 to 1
                    ditherNoise *= _EdgeDither * _EdgeThickness; // Scale by dither amount and edge size
                }

                // Apply dither to the edge start threshold
                float ditheredEdgeStart = edgeStart - ditherNoise;

                // Raw edge visibility before power adjustment
                float rawEdgeVisibility = smoothstep(ditheredEdgeStart, 1.0, distFromCenter);

                // Apply EdgePower to control the gradient sharpness
                // Lower power = wider gradient, Higher power = sharper edge
                float edgeVisibility = pow(rawEdgeVisibility, _EdgePower);

                // Edge glow intensity (for color blending)
                float edgeGlow = edgeVisibility * _EdgeIntensity;

                // Energy pattern - use LOCAL POSITION for world-scale patterns
                // This makes patterns visible regardless of edge thickness
                float patternRaw = GetPattern(IN.localPos, _PatternType, _PatternScale, _PatternSpeed, time);

                // Pattern creates visible structure in the edge
                // Pattern value 0 = gap/transparent, Pattern value 1 = solid/visible
                // Use stronger contrast so pattern is clearly visible
                float patternMin = 1.0 - _PatternIntensity * 2.0; // Stronger gaps
                patternMin = max(0.0, patternMin); // Clamp to avoid negative
                float patternEffect = lerp(patternMin, 1.0, patternRaw);

                // Pulse animation
                float pulse = sin(time * _PulseSpeed * 2.0 * PI) * 0.5 + 0.5;
                float pulseEffect = 1.0 + pulse * _PulseAmount;

                // Impact ripples
                float ripples = CalculateImpactRipples(IN.localPos, time);

                // Impact proximity for localized visibility (used in OnlyOnHit/Both modes)
                float impactProximity = CalculateImpactProximity(IN.localPos, time);

                // Color blending
                float3 baseColor = _ShieldColor.rgb;
                float3 edgeColor = _EdgeColor.rgb;
                float3 hitColor = _HitFlashColor.rgb;

                // Blend from base to edge color based on edge visibility
                float colorBlend = saturate(edgeVisibility);
                float3 color = lerp(baseColor, edgeColor, colorBlend);

                // Pattern affects color more strongly - pattern lines glow brighter
                // patternRaw 1.0 = pattern line (bright), patternRaw 0.0 = gap (dim)
                float3 patternGlowColor = edgeColor * (1.0 + _EdgeIntensity * 0.5);
                color = lerp(color * (1.0 - _PatternIntensity * 0.5), patternGlowColor, patternRaw * _PatternIntensity);

                // Ripple color effect - blend gently instead of harsh flash to white
                // Creates a bright pulse that enhances the edge color rather than replacing it
                float rippleBlendStrength = saturate(ripples * 0.5); // Reduced from 0.7
                float3 rippleColor = lerp(edgeColor * 1.5, hitColor, rippleBlendStrength * 0.5);
                color = lerp(color, rippleColor, rippleBlendStrength);

                // Shield health affects color (damaged shields transition to damaged color)
                if (_ShieldHealth < 0.5)
                {
                    float3 damagedColorBlend = lerp(_DamagedColor.rgb, color, _ShieldHealth * 2.0);
                    color = damagedColorBlend;
                }

                // === ALPHA CALCULATION ===
                // Base alpha from shield color opacity
                float baseAlpha = _ShieldColor.a;

                // Edge color opacity boosts the edge brightness
                float edgeAlphaBoost = _EdgeColor.a;

                // Combine: edge visibility * pattern * pulse * base opacity
                float alpha = edgeVisibility * patternEffect * pulseEffect * baseAlpha;

                // Edge color alpha adds extra brightness at the edge
                alpha = lerp(alpha, alpha * edgeAlphaBoost * 1.5, edgeVisibility);

                // CurrentOpacity directly controls overall visibility
                // This value is set by ShieldVisual based on visibility mode and hit state
                alpha *= _CurrentOpacity;
                alpha = saturate(alpha);

                // === LOCALIZED IMPACT VISIBILITY ===
                // For OnlyOnHit/Both modes: localize visibility around impact points
                // When idleOpacity is low, we want visibility mainly near impacts
                // When idleOpacity is high, we want the shield always visible
                float idleVisibility = alpha * saturate(_IdleOpacity * 2.5); // Base visibility from idle opacity
                float impactVisibility = alpha * impactProximity; // Visibility near impacts

                // Blend: low idle opacity = more localized, high idle opacity = always visible
                // isImpactDriven: 1 = OnlyOnHit (fully localized), 0 = AlwaysSubtle (always visible)
                float isImpactDriven = 1.0 - saturate(_IdleOpacity * 2.5);
                alpha = lerp(alpha, max(idleVisibility, impactVisibility), isImpactDriven);

                // Ensure minimum visibility in edge region (for AlwaysSubtle mode)
                float minAlpha = edgeVisibility * _IdleOpacity * 0.3;
                alpha = max(alpha, minAlpha);

                // Ripples can show through even in center area (impact effect)
                alpha = max(alpha, ripples * 0.6 * impactProximity);

                // Ripple rings always visible regardless of proximity
                alpha = max(alpha, ripples * 0.4);

                // Low health flicker
                if (_ShieldHealth < 0.3)
                {
                    float flicker = sin(time * 30.0) * sin(time * 23.0);
                    alpha *= lerp(0.5, 1.0, saturate(flicker * 0.5 + 0.5));
                }

                return half4(color, alpha);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
