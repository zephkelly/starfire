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
        _VisibilityMode ("Visibility Mode (0=Always, 1=OnlyOnHit, 2=Both)", Int) = 0
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

        [Header(Dome Curvature)]
        _DomeCurvature ("Dome Curvature", Range(0, 1)) = 0.5
        _DomeHighlight ("Dome Highlight", Range(0, 1)) = 0.3
        _DomeShadow ("Dome Shadow", Range(0, 1)) = 0.2
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
            // xy = local position, z = start time, w = unused
            // All 8 slots are always checked - impacts use a circular buffer
            float4 _ImpactPositions[MAX_IMPACTS];

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
                int _VisibilityMode;
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
                float _DomeCurvature;
                float _DomeHighlight;
                float _DomeShadow;
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

            // Hash function for pseudo-random values
            float hash21(float2 p)
            {
                p = frac(p * float2(234.34, 435.345));
                p += dot(p, p + 34.23);
                return frac(p.x * p.y);
            }

            // Smooth value noise
            float ValueNoise(float2 p)
            {
                float2 i = floor(p);
                float2 f = frac(p);

                // Smooth interpolation curve
                float2 u = f * f * (3.0 - 2.0 * f);

                // Four corners
                float a = hash21(i);
                float b = hash21(i + float2(1.0, 0.0));
                float c = hash21(i + float2(0.0, 1.0));
                float d = hash21(i + float2(1.0, 1.0));

                // Bilinear interpolation
                return lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y);
            }

            // Fractal Brownian Motion - layers of noise for organic look
            float FBM(float2 p, float time)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float frequency = 1.0;

                // Add flowing motion
                float2 flow = float2(time * 0.3, time * 0.2);

                // 4 octaves of noise
                for (int i = 0; i < 4; i++)
                {
                    value += amplitude * ValueNoise(p * frequency + flow);
                    flow *= 1.3; // Different flow rate per octave
                    amplitude *= 0.5;
                    frequency *= 2.0;
                }

                return value;
            }

            float NoisePattern(float2 uv, float scale, float time)
            {
                float2 p = uv * scale;

                // Layer 1: Base turbulent noise
                float noise1 = FBM(p, time);

                // Layer 2: Offset noise for more complexity
                float noise2 = FBM(p + float2(5.2, 1.3) + noise1 * 0.5, time * 0.7);

                // Layer 3: Fine detail with faster animation
                float noise3 = ValueNoise(p * 3.0 + time * 0.5) * 0.3;

                // Combine layers with domain warping effect
                float result = noise1 * 0.5 + noise2 * 0.35 + noise3;

                // Add electric crackling effect - sharp bright lines
                float crackle = pow(ValueNoise(p * 8.0 + time * 2.0), 3.0);
                result = max(result, crackle * 0.8);

                // Enhance contrast for more striking appearance
                result = smoothstep(0.2, 0.8, result);

                return saturate(result);
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

                // Always iterate all slots - impacts are stored in circular buffer
                // Each impact has its own timing check inside the loop
                for (int i = 0; i < MAX_IMPACTS; i++)
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
            // Each impact stores its own radius in _ImpactPositions[i].w
            float CalculateImpactProximity(float2 localPos, float time)
            {
                float maxProximity = 0.0;

                for (int i = 0; i < MAX_IMPACTS; i++)
                {
                    float2 impactPos = _ImpactPositions[i].xy;
                    float impactTime = _ImpactPositions[i].z;
                    float impactRadius = _ImpactPositions[i].w;

                    float elapsed = time - impactTime;
                    if (elapsed < 0 || elapsed > _RippleDuration)
                        continue;

                    // Use per-impact radius, fall back to global if zero
                    float radius = impactRadius > 0.001 ? impactRadius : _ImpactVisibilityRadius;

                    float normalizedTime = elapsed / _RippleDuration;
                    float dist = length(localPos - impactPos);

                    float proximity = 1.0 - saturate(dist / radius);
                    proximity = pow(proximity, _ImpactVisibilityFalloff);

                    float fadeOut = 1.0 - normalizedTime * _ImpactVisibilitySpeed;
                    float timeFade = saturate(fadeOut * fadeOut);

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

                // Pattern modulates edge brightness, but doesn't make it invisible
                // Base visibility is always present, pattern adds variation
                float patternMin = 0.3; // Minimum visibility even in pattern gaps
                float patternEffect = lerp(patternMin, 1.0, patternRaw * _PatternIntensity);

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

                // === DOME CURVATURE EFFECT ===
                // Simulate a 3D dome/bubble by calculating fake depth and lighting
                if (_DomeCurvature > 0.001)
                {
                    // Calculate position on a hemisphere (0,0 = center, 1 = edge)
                    float2 domePos = (IN.uv - float2(0.5, 0.5)) * 2.0;
                    float domeRadius = length(domePos);

                    // Calculate simulated Z height on hemisphere (1 at center, 0 at edge)
                    float domeZ = sqrt(max(0.0, 1.0 - domeRadius * domeRadius));

                    // Calculate surface normal of the dome
                    float3 domeNormal = normalize(float3(domePos.x, domePos.y, domeZ * _DomeCurvature));

                    // Virtual light direction (coming from upper-left, slightly in front)
                    float3 lightDir = normalize(float3(-0.4, 0.5, 0.7));

                    // Lambertian diffuse lighting
                    float NdotL = dot(domeNormal, lightDir);

                    // Soften the lighting for energy shield look
                    float diffuse = NdotL * 0.5 + 0.5; // Remap from [-1,1] to [0,1]
                    diffuse = lerp(1.0, diffuse, _DomeCurvature);

                    // Apply shadow (darken areas facing away from light)
                    float shadow = lerp(1.0, diffuse, _DomeShadow);
                    color *= shadow;

                    // Calculate specular/highlight for shiny energy look
                    float3 viewDir = float3(0, 0, 1); // Looking straight at shield
                    float3 halfDir = normalize(lightDir + viewDir);
                    float NdotH = max(0.0, dot(domeNormal, halfDir));
                    float specular = pow(NdotH, 16.0) * _DomeHighlight;

                    // Add rim lighting at edges (fresnel-like, enhanced by dome)
                    float rimFresnel = 1.0 - domeZ;
                    float rim = pow(rimFresnel, 2.0) * _DomeCurvature * 0.5;

                    // Add highlights
                    color += edgeColor * (specular + rim);
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
                // Visibility mode behavior:
                // Mode 0 (AlwaysSubtle): Full shield visible at idle opacity
                // Mode 1 (OnlyOnHit): Only visible near impacts
                // Mode 2 (Both): Subtle always + bright near impacts

                if (_VisibilityMode == 1) // OnlyOnHit
                {
                    // Shield only visible inside per-impact proximity circles
                    alpha = alpha * impactProximity;
                    // Ripples render independently with their own fade
                    alpha = max(alpha, ripples * _RippleOpacity);
                }
                else if (_VisibilityMode == 2) // Both
                {
                    // alpha already includes _CurrentOpacity (idleOpacity normally, activeOpacity->idleOpacity on hit)
                    // Enhance visibility near impacts
                    alpha = alpha * (1.0 + impactProximity);
                    // Ripples always visible, enhanced near impacts
                    alpha = max(alpha, ripples * lerp(0.5, 1.0, impactProximity));
                }
                else // AlwaysSubtle (Mode 0)
                {
                    // Full visibility, ripples everywhere
                    alpha = max(alpha, ripples * 0.4);
                }

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
