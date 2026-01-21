Shader "Starfire/Comet"
{
    Properties
    {
        [Header(Pixelization)]
        _Pixels ("Pixels", Range(100, 1000)) = 400.0

        [Header(Gradient)]
        _GradientTex ("Gradient Texture", 2D) = "white" {}

        [Header(Appearance)]
        _CometColor ("Comet Color", Color) = (0.8, 0.9, 1, 1)
        _Brightness ("Brightness", Range(0.5, 8.0)) = 3.0

        [Header(Nucleus)]
        _NucleusSize ("Nucleus Size", Float) = 0.15
        _ComaSize ("Coma Size", Float) = 0.6
        _ComaSoftness ("Coma Softness", Range(0.5, 5.0)) = 2.0
        _CorePulseSpeed ("Core Pulse Speed", Range(0.5, 5.0)) = 1.5
        _CorePulseAmount ("Core Pulse Amount", Range(0, 0.5)) = 0.15

        [Header(Boiling Front)]
        _BoilCellScale ("Boil Cell Scale", Range(2, 12)) = 6.0
        _BoilSpeed ("Boil Speed", Range(0.5, 5.0)) = 2.0
        _BoilIntensity ("Boil Intensity", Range(0, 1)) = 0.6

        [Header(Dust Tail)]
        _DustTailWidth ("Dust Tail Width", Range(0.1, 2.0)) = 0.8
        _DustTailCurve ("Dust Tail Curvature", Range(0, 1)) = 0.3
        _DustFalloff ("Dust Tail Falloff", Range(1, 5)) = 2.0

        [Header(Ion Tail)]
        _IonTailWidth ("Ion Tail Width", Range(0.05, 0.5)) = 0.2
        _IonTailLength ("Ion Tail Length Multiplier", Range(0.5, 2.0)) = 1.5
        _IonFalloff ("Ion Tail Falloff", Range(1, 5)) = 1.5

        [Header(Tail Noise)]
        _TailNoiseScale ("Tail Noise Scale", Float) = 50.0
        _TailNoiseOctaves ("Tail FBM Octaves", Range(1, 5)) = 3

        [Header(Sparkles)]
        _SparkleCount ("Sparkle Count", Range(8, 32)) = 16
        _SparkleSize ("Sparkle Size", Range(0.005, 0.03)) = 0.015
        _SparkleSpeed ("Sparkle Twinkle Speed", Range(2, 15)) = 8.0
        _SparkleBrightness ("Sparkle Brightness", Range(0.5, 3.0)) = 1.5

        [Header(Particle Trail)]
        _ParticleCount ("Particle Count", Int) = 24
        _ParticleSizeMin ("Particle Size Min", Float) = 0.02
        _ParticleSizeMax ("Particle Size Max", Float) = 0.08
        _ParticleSpread ("Particle Spread", Float) = 0.5
        _ParticleFadeRate ("Particle Fade Rate", Range(0.5, 5.0)) = 2.0
        _ReferenceSpeed ("Reference Speed", Float) = 8.0

        [Header(Seed)]
        _Seed ("Seed", Range(1, 10)) = 1

        [Header(Background)]
        _ParallaxFactor ("Parallax Factor", Float) = 0.02

        [Header(Debug)]
        [Toggle] _DebugMode ("Debug Mode (ignore gradient)", Float) = 0
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
            #define MAX_SPARKLES 32

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
                float _Pixels;
                float4 _CometColor;
                float _Brightness;
                float _NucleusSize;
                float _ComaSize;
                float _ComaSoftness;
                float _CorePulseSpeed;
                float _CorePulseAmount;
                float _BoilCellScale;
                float _BoilSpeed;
                float _BoilIntensity;
                float _DustTailWidth;
                float _DustTailCurve;
                float _DustFalloff;
                float _IonTailWidth;
                float _IonTailLength;
                float _IonFalloff;
                float _TailNoiseScale;
                int _TailNoiseOctaves;
                int _SparkleCount;
                float _SparkleSize;
                float _SparkleSpeed;
                float _SparkleBrightness;
                int _ParticleCount;
                float _ParticleSizeMin;
                float _ParticleSizeMax;
                float _ParticleSpread;
                float _ParticleFadeRate;
                float _ReferenceSpeed;
                float _Seed;
                float _ParallaxFactor;
                float _DebugMode;
            CBUFFER_END

            // Gradient texture
            TEXTURE2D(_GradientTex);
            SAMPLER(sampler_GradientTex);

            // Set from script - camera data
            float2 _CameraWorldPos;
            float _ScreenAspect;
            float _CameraOrthoSize;
            float _ReferenceZoom;

            // Set from script - sun direction for ion tail
            float2 _SunDirection;

            // Set from script - comet data
            int _ActiveCometCount;
            float4 _CometPositions[MAX_COMETS];    // xy = head position, zw = tail position (world space)
            float4 _CometParams1[MAX_COMETS];      // x = brightness, y = progress (0-1), z = nucleusSize, w = comaSize
            float4 _CometParams2[MAX_COMETS];      // x = particleSeed, y = speed, z = sunOverrideX, w = sunOverrideY
            float4 _CometParams3[MAX_COMETS];      // x = pulsePhase, y = dustCurveAmount, z = ionLengthMult, w = unused

            // =============================================================================
            // PIXELIZATION & DITHERING
            // =============================================================================

            float2 pixelize(float2 uv)
            {
                return floor(uv * _Pixels) / _Pixels;
            }

            float hlmod(float x, float y)
            {
                return x - y * floor(x / y);
            }

            float2 hlmod2(float2 x, float2 y)
            {
                return x - y * floor(x / y);
            }

            bool dither(float2 uv1, float2 uv2)
            {
                return hlmod(uv1.x + uv2.y, 2.0 / _Pixels) <= 1.0 / _Pixels;
            }

            // =============================================================================
            // NOISE FUNCTIONS (adapted from reference shaders)
            // =============================================================================

            // Seeded random hash
            float rand(float2 coord)
            {
                return frac(sin(dot(coord.xy, float2(12.9898, 78.233))) * 43758.5453 * _Seed);
            }

            float rand2(float2 coord, float seed)
            {
                return frac(sin(dot(coord.xy, float2(12.9898, 78.233))) * 43758.5453 * seed);
            }

            // Hash function for deterministic pseudo-random values (original)
            float hash1(float n)
            {
                return frac(sin(n) * 43758.5453123);
            }

            float hash1v2(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            // Value noise with cubic interpolation
            float noise(float2 coord)
            {
                float2 i = floor(coord);
                float2 f = frac(coord);

                float a = rand(i);
                float b = rand(i + float2(1.0, 0.0));
                float c = rand(i + float2(0.0, 1.0));
                float d = rand(i + float2(1.0, 1.0));

                float2 cubic = f * f * (3.0 - 2.0 * f);

                return lerp(a, b, cubic.x) + (c - a) * cubic.y * (1.0 - cubic.x) + (d - b) * cubic.x * cubic.y;
            }

            // Fractal Brownian Motion
            float fbm(float2 coord, int octaves)
            {
                float value = 0.0;
                float scale = 0.5;

                for (int i = 0; i < octaves && i < 5; i++)
                {
                    value += noise(coord) * scale;
                    coord *= 2.0;
                    scale *= 0.5;
                }
                return value;
            }

            // Animated hash for Cells noise
            float2 Hash2(float2 p, float time)
            {
                float t = (time + 10.0) * 0.3;
                return float2(noise(p), noise(p * float2(0.3135 + sin(t), 0.5813 - cos(t))));
            }

            // Cells/Worley noise (from Star.shader)
            float Cells(float2 p, float numCells, float time, float tiles)
            {
                p *= numCells;
                float d = 1.0e10;

                for (int xo = -1; xo <= 1; xo++)
                {
                    for (int yo = -1; yo <= 1; yo++)
                    {
                        float2 tp = floor(p) + float2(float(xo), float(yo));
                        float2 cellOffset = Hash2(hlmod2(tp, float2(numCells / tiles, numCells / tiles)), time);
                        tp = p - tp - cellOffset;
                        d = min(d, dot(tp, tp));
                    }
                }
                return sqrt(d);
            }

            // =============================================================================
            // GRADIENT SAMPLING
            // =============================================================================

            float3 sampleGradient(float t)
            {
                t = saturate(t);

                // Debug mode: use hardcoded gradient (white->yellow->orange->blue->purple)
                if (_DebugMode > 0.5)
                {
                    if (t < 0.2) return lerp(float3(1.0, 1.0, 1.0), float3(1.0, 1.0, 0.5), t / 0.2);
                    if (t < 0.4) return lerp(float3(1.0, 1.0, 0.5), float3(1.0, 0.7, 0.2), (t - 0.2) / 0.2);
                    if (t < 0.6) return lerp(float3(1.0, 0.7, 0.2), float3(0.5, 0.7, 1.0), (t - 0.4) / 0.2);
                    return lerp(float3(0.5, 0.7, 1.0), float3(0.5, 0.3, 0.8), (t - 0.6) / 0.4);
                }

                return SAMPLE_TEXTURE2D_LOD(_GradientTex, sampler_GradientTex, float2(t, 0.5), 0).rgb;
            }

            // =============================================================================
            // RENDERING FUNCTIONS
            // =============================================================================

            // Distance to line segment
            float distToSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
            }

            // Position along segment (0 = a, 1 = b)
            float positionOnSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                return saturate(dot(pa, ba) / dot(ba, ba));
            }

            // Distance to quadratic bezier curve (sampled approximation)
            float distToBezier(float2 p, float2 a, float2 control, float2 b)
            {
                float minDist = 1e10;

                [unroll(9)]
                for (int i = 0; i <= 8; i++)
                {
                    float t = float(i) / 8.0;
                    float2 q1 = lerp(a, control, t);
                    float2 q2 = lerp(control, b, t);
                    float2 bezierPoint = lerp(q1, q2, t);
                    minDist = min(minDist, length(p - bezierPoint));
                }
                return minDist;
            }

            // Position along bezier (approximate)
            float positionOnBezier(float2 p, float2 a, float2 control, float2 b)
            {
                float minDist = 1e10;
                float bestT = 0.0;

                [unroll(9)]
                for (int i = 0; i <= 8; i++)
                {
                    float t = float(i) / 8.0;
                    float2 q1 = lerp(a, control, t);
                    float2 q2 = lerp(control, b, t);
                    float2 bezierPoint = lerp(q1, q2, t);
                    float d = length(p - bezierPoint);
                    if (d < minDist)
                    {
                        minDist = d;
                        bestT = t;
                    }
                }
                return bestT;
            }

            // Render nucleus with pulsing
            float renderNucleus(float2 worldPos, float2 headPos, float nucleusSize,
                float pulseAmount, float pulseSpeed, float pulsePhase, float time, bool dith)
            {
                float dist = length(worldPos - headPos);

                // Pulsing size modulation
                float pulse = sin(time * pulseSpeed + pulsePhase) * 0.5 + 0.5;
                float animatedSize = nucleusSize * (1.0 + pulse * pulseAmount);

                // Brightness also pulses
                float brightnessPulse = 1.0 + pulse * pulseAmount * 0.5;

                // Core with dithered edge
                float nucleus = 1.0 - smoothstep(0.0, animatedSize, dist);

                // Dithered transition at edge
                float ditherZone = animatedSize * 0.3;
                if (dist > animatedSize - ditherZone && dist < animatedSize + ditherZone)
                {
                    if (dith) nucleus *= 1.2;
                }

                nucleus = pow(nucleus, 1.5) * 3.0 * brightnessPulse;

                return nucleus;
            }

            // Render coma with gradient
            float3 renderComa(float2 worldPos, float2 headPos, float comaSize, float softness, bool dith)
            {
                float dist = length(worldPos - headPos);
                float coma = 1.0 - smoothstep(0.0, comaSize, dist);

                // Dithered edge
                float ditherZone = comaSize * 0.2;
                if (dist > comaSize - ditherZone && dist < comaSize + ditherZone)
                {
                    if (dith) coma *= 1.15;
                }

                coma = pow(coma, softness) * 0.5;

                // Sample gradient based on distance (0 = center = hot, 1 = edge = cooler)
                float gradientT = saturate(dist / comaSize) * 0.4; // Stay in warm range
                float3 color = sampleGradient(gradientT);

                return coma * color;
            }

            // Render boiling gas front using Cells noise
            float3 renderBoilingFront(float2 worldPos, float2 headPos, float comaSize,
                float cellScale, float boilSpeed, float intensity, float time, float seed, bool dith)
            {
                float dist = length(worldPos - headPos);

                // Only render in ring around nucleus
                float ringRadius = comaSize * 0.5;
                float ringWidth = comaSize * 0.4;
                float ringDist = abs(dist - ringRadius);

                if (ringDist > ringWidth) return float3(0, 0, 0);

                // Convert to radial coordinates for cells
                float2 dir = (worldPos - headPos) / max(dist, 0.001);
                float angle = atan2(dir.y, dir.x);
                float2 cellUV = float2(angle / 6.28318 + 0.5, dist / comaSize);

                // Animated cells noise - multiple scales multiplied together
                float n = Cells(cellUV - float2(time * boilSpeed * 0.5, 0), cellScale, time, 3.0);
                n *= Cells(cellUV - float2(time * boilSpeed * 0.7, 0), cellScale * 1.5, time, 3.0);

                // Adjust cell value
                n *= 2.4;
                n = saturate(n);

                // Dithering
                if (dith) n *= 1.2;

                // Ring falloff
                float ringFalloff = 1.0 - smoothstep(0.0, ringWidth, ringDist);
                n *= ringFalloff * intensity;

                // Warm color from gradient
                float3 color = sampleGradient(0.2); // Orange-yellow range

                return n * color;
            }

            // Render ion tail (straight line toward sun-opposite)
            float3 renderIonTail(float2 worldPos, float2 headPos, float2 sunDir, float tailLength,
                float width, float falloff, float time, float seed, float effectiveOrthoSize, bool dith)
            {
                // Ion tail points opposite to sun direction
                float2 ionTailEnd = headPos + sunDir * tailLength;

                // Distance to line
                float dist = distToSegment(worldPos, headPos, ionTailEnd);
                float posOnLine = positionOnSegment(worldPos, headPos, ionTailEnd);

                // Early out
                if (posOnLine < 0.0 || posOnLine > 1.0 || dist > width * 2.0) return float3(0, 0, 0);

                // Add FBM noise to edges
                float2 noiseCoord = worldPos * _TailNoiseScale / effectiveOrthoSize;
                float edgeNoise = fbm(noiseCoord + time * 0.5, _TailNoiseOctaves);
                float noisyWidth = width * (0.7 + edgeNoise * 0.6);

                // Width tapers along tail
                float effectiveWidth = noisyWidth * lerp(0.5, 1.0, 1.0 - posOnLine);

                // Falloff from center
                float intensity = 1.0 - smoothstep(0.0, effectiveWidth, dist);
                intensity = pow(intensity, falloff);

                // Fade along tail
                intensity *= pow(1.0 - posOnLine, 0.5);

                // Dithered edge
                if (dith && dist > effectiveWidth * 0.7) intensity *= 1.2;

                // Cool blue color from gradient
                float gradientT = 0.6 + posOnLine * 0.3; // Blue to purple range
                float3 color = sampleGradient(gradientT);

                return intensity * color * 0.4; // Subtle ion tail
            }

            // Render dust tail (curved bezier path)
            float3 renderDustTail(float2 worldPos, float2 headPos, float2 tailPos, float curveAmount,
                float width, float falloff, float time, float seed, float effectiveOrthoSize, bool dith)
            {
                // Calculate control point for curved tail (perpendicular offset)
                float2 trailDir = normalize(headPos - tailPos);
                float2 perpDir = float2(-trailDir.y, trailDir.x);
                float2 midPoint = (headPos + tailPos) * 0.5;
                float2 controlPoint = midPoint + perpDir * curveAmount * length(headPos - tailPos);

                // Distance to bezier curve
                float dist = distToBezier(worldPos, tailPos, controlPoint, headPos);
                float posOnCurve = positionOnBezier(worldPos, tailPos, controlPoint, headPos);

                // Early out
                if (dist > width * 2.0) return float3(0, 0, 0);

                // Add FBM noise to edges
                float2 noiseCoord = worldPos * _TailNoiseScale / effectiveOrthoSize;
                float edgeNoise = fbm(noiseCoord + time * 0.3, _TailNoiseOctaves);
                float noisyWidth = width * (0.7 + edgeNoise * 0.6);

                // Width varies along trail (wider near head for curved dust)
                float effectiveWidth = noisyWidth * lerp(0.3, 1.0, posOnCurve);

                // Falloff from center
                float intensity = 1.0 - smoothstep(0.0, effectiveWidth, dist);
                intensity = pow(intensity, falloff);

                // Fade along tail (brighter near head)
                intensity *= pow(posOnCurve, 0.7);

                // Dithered edge
                if (dith && dist > effectiveWidth * 0.6) intensity *= 1.15;

                // Warm yellow-orange color from gradient
                float gradientT = 0.15 + (1.0 - posOnCurve) * 0.25; // Yellow to orange range
                float3 color = sampleGradient(gradientT);

                return intensity * color * 0.7;
            }

            // Render sparkle particles
            float3 renderSparkles(float2 worldPos, float2 headPos, float2 tailPos,
                int sparkleCount, float sparkleSize, float sparkleSpeed, float sparkleBrightness,
                float time, float seed, float effectiveOrthoSize, bool dith)
            {
                float result = 0.0;
                float2 dir = normalize(tailPos - headPos);
                float pathLength = length(tailPos - headPos);
                float2 perpDir = float2(-dir.y, dir.x);

                int count = min(sparkleCount, MAX_SPARKLES);

                for (int s = 0; s < count; s++)
                {
                    // Position along path
                    float t = hash1(seed + float(s));
                    float2 basePos = lerp(headPos, tailPos, t);

                    // Perpendicular offset (wider spread further from head)
                    float spread = t * pathLength * 0.15;
                    float offset = (hash1(seed + float(s) + 50.0) - 0.5) * spread;
                    float2 sparklePos = basePos + perpDir * offset;

                    // Snap to pixel grid
                    sparklePos = floor(sparklePos * _Pixels / effectiveOrthoSize) * effectiveOrthoSize / _Pixels;

                    // Animated brightness (twinkling)
                    float phase = hash1(seed + float(s) + 100.0) * 6.28318;
                    float twinkle = sin(time * sparkleSpeed + phase);
                    twinkle = twinkle * 0.5 + 0.5;  // 0 to 1
                    twinkle = pow(twinkle, 3.0);     // Sharper peaks

                    // Only render when bright enough
                    if (twinkle > 0.1)
                    {
                        float dist = length(worldPos - sparklePos);
                        float pxSize = sparkleSize * effectiveOrthoSize;
                        float sparkle = 1.0 - smoothstep(0.0, pxSize, dist);

                        // Dithered edge
                        if (dith && dist > pxSize * 0.5) sparkle *= 1.3;

                        result += sparkle * twinkle * (1.0 - t * 0.5) * sparkleBrightness;
                    }
                }

                // White-ish sparkle color
                float3 color = sampleGradient(0.05);
                return result * color;
            }

            // Render particle trail (adapted from original)
            float3 renderParticleTrail(float2 worldPos, float2 headPos, float2 tailPos,
                int particleCount, float sizeMin, float sizeMax, float spread, float fadeRate,
                float cometSpeed, float referenceSpeed, float effectiveOrthoSize, float seed, bool dith)
            {
                float2 trailDir = normalize(headPos - tailPos);
                float trailLength = length(headPos - tailPos);
                float speedSpreadFactor = cometSpeed / referenceSpeed;

                float particleIntensity = 0.0;
                int count = min(particleCount, MAX_PARTICLES);

                for (int p = 0; p < count; p++)
                {
                    float t = float(p) / float(count);
                    float2 basePos = lerp(tailPos, headPos, t);

                    float spreadFactor = t;
                    float hashX = hash1(seed + float(p) * 17.0) - 0.5;
                    float hashY = hash1(seed + float(p) * 31.0) - 0.5;

                    float dynamicSpread = spread * speedSpreadFactor * effectiveOrthoSize;

                    float2 perpDir = float2(-trailDir.y, trailDir.x);
                    float2 offset = perpDir * hashX * dynamicSpread * spreadFactor;
                    offset += trailDir * hashY * dynamicSpread * 0.3 * spreadFactor;

                    float2 particlePos = basePos + offset;

                    float sizeRand = hash1(seed + float(p) * 7.0);
                    float size = lerp(sizeMin, sizeMax, sizeRand * t) * effectiveOrthoSize;

                    float particleBrightness = pow(t, fadeRate);

                    float dist = length(worldPos - particlePos);
                    float particle = 1.0 - smoothstep(0.0, size, dist);

                    // Dithered particles
                    if (dith && dist > size * 0.6) particle *= 1.15;

                    particleIntensity += particle * particleBrightness;
                }

                // Warm color for particles
                float3 color = sampleGradient(0.25);
                return particleIntensity * 0.6 * color;
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
                // Pixelize UV coordinates
                float2 uv = IN.uv;
                float2 pixelizedUV = pixelize(uv);
                bool dith = dither(uv, pixelizedUV);

                // Calculate world position from UV
                float2 centeredUV = (pixelizedUV - 0.5) * 2.0;

                // Zoom factor - scale uniformly with camera
                float effectiveOrthoSize = _CameraOrthoSize;

                // Parallax coordinate space
                float2 worldPos;
                worldPos.x = centeredUV.x * effectiveOrthoSize * _ScreenAspect + _CameraWorldPos.x * _ParallaxFactor;
                worldPos.y = centeredUV.y * effectiveOrthoSize + _CameraWorldPos.y * _ParallaxFactor;

                float3 result = float3(0, 0, 0);

                // Process each active comet
                for (int i = 0; i < _ActiveCometCount && i < MAX_COMETS; i++)
                {
                    // Extract comet data
                    float2 headPos = _CometPositions[i].xy * _ParallaxFactor;
                    float2 dustTailPos = _CometPositions[i].zw * _ParallaxFactor;
                    float cometBrightness = _CometParams1[i].x;
                    float progress = _CometParams1[i].y;
                    float nucleusSize = _CometParams1[i].z * effectiveOrthoSize;
                    float comaSize = _CometParams1[i].w * effectiveOrthoSize;
                    float particleSeed = _CometParams2[i].x;
                    float cometSpeed = _CometParams2[i].y;

                    // Get sun direction (per-comet override or global)
                    float2 sunDirOverride = _CometParams2[i].zw;
                    float2 sunDir = (length(sunDirOverride) > 0.01) ? normalize(sunDirOverride) : normalize(_SunDirection);

                    // Per-comet parameters
                    float pulsePhase = _CometParams3[i].x;
                    float dustCurveAmount = _CometParams3[i].y;
                    float ionLengthMult = _CometParams3[i].z;

                    // Calculate trail properties
                    float trailLength = length(headPos - dustTailPos);
                    float ionTailLength = trailLength * _IonTailLength * ionLengthMult;

                    // Early out if too far from comet
                    float distToHead = length(worldPos - headPos);

                    // Debug mode: draw bright circles at comet positions
                    if (_DebugMode > 0.5)
                    {
                        float distToTail = length(worldPos - dustTailPos);
                        float debugRadius = max(0.5, comaSize * 0.5);

                        // Head = bright yellow circle
                        if (distToHead < debugRadius)
                        {
                            float intensity = 1.0 - (distToHead / debugRadius);
                            result += float3(1.0, 1.0, 0.0) * intensity * 2.0;
                        }

                        // Tail = orange circle
                        if (distToTail < debugRadius * 0.7)
                        {
                            float intensity = 1.0 - (distToTail / (debugRadius * 0.7));
                            result += float3(1.0, 0.5, 0.0) * intensity * 1.5;
                        }

                        // Draw line between head and tail
                        float2 lineDir = normalize(dustTailPos - headPos);
                        float2 toPixel = worldPos - headPos;
                        float projLen = dot(toPixel, lineDir);
                        if (projLen > 0 && projLen < trailLength)
                        {
                            float2 closestPoint = headPos + lineDir * projLen;
                            float distToLine = length(worldPos - closestPoint);
                            if (distToLine < debugRadius * 0.15)
                            {
                                result += float3(0.5, 0.5, 0.0) * (1.0 - distToLine / (debugRadius * 0.15));
                            }
                        }
                    }
                    float maxRange = max(trailLength, ionTailLength) * 1.5 + comaSize;
                    if (distToHead > maxRange) continue;

                    float3 cometResult = float3(0, 0, 0);

                    // === RENDER LAYERS (back to front) ===

                    // Layer 1: Ion Tail
                    cometResult += renderIonTail(worldPos, headPos, sunDir, ionTailLength,
                        _IonTailWidth * effectiveOrthoSize, _IonFalloff, _Time.y, particleSeed,
                        effectiveOrthoSize, dith);

                    // Layer 2: Dust Tail
                    cometResult += renderDustTail(worldPos, headPos, dustTailPos,
                        dustCurveAmount * _DustTailCurve,
                        _DustTailWidth * effectiveOrthoSize, _DustFalloff, _Time.y, particleSeed,
                        effectiveOrthoSize, dith);

                    // Layer 3: Particle Trail (legacy particles for additional detail)
                    cometResult += renderParticleTrail(worldPos, headPos, dustTailPos,
                        _ParticleCount, _ParticleSizeMin, _ParticleSizeMax, _ParticleSpread,
                        _ParticleFadeRate, cometSpeed, _ReferenceSpeed, effectiveOrthoSize,
                        particleSeed, dith);

                    // Layer 4: Sparkles
                    cometResult += renderSparkles(worldPos, headPos, dustTailPos,
                        _SparkleCount, _SparkleSize, _SparkleSpeed, _SparkleBrightness,
                        _Time.y, particleSeed + 500.0, effectiveOrthoSize, dith);

                    // Layer 5: Boiling Front
                    cometResult += renderBoilingFront(worldPos, headPos, comaSize,
                        _BoilCellScale, _BoilSpeed, _BoilIntensity, _Time.y, particleSeed, dith);

                    // Layer 6: Coma
                    cometResult += renderComa(worldPos, headPos, comaSize, _ComaSoftness, dith);

                    // Layer 7: Nucleus with pulsing
                    float nucleus = renderNucleus(worldPos, headPos, nucleusSize,
                        _CorePulseAmount, _CorePulseSpeed, pulsePhase, _Time.y, dith);
                    float3 coreColor = sampleGradient(0.0); // Brightest/hottest
                    cometResult += nucleus * coreColor;

                    // Apply comet brightness and fade
                    float fadeIn = smoothstep(0.0, 0.1, progress);
                    float fadeOut = 1.0 - smoothstep(0.9, 1.0, progress);
                    cometResult *= cometBrightness * fadeIn * fadeOut;

                    result += cometResult;
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
