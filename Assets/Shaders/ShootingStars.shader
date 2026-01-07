Shader "Starfire/ShootingStars"
{
    Properties
    {
        [Header(Appearance)]
        _StarColor ("Star Color", Color) = (1, 1, 1, 1)
        _Brightness ("Brightness", Range(0.5, 5.0)) = 1.5
        _StarWidth ("Star Width", Range(0.001, 0.05)) = 0.01
        _CoreSharpness ("Core Sharpness", Range(0.1, 1.0)) = 0.7

        [Header(Trail)]
        _TrailFalloff ("Trail Falloff", Range(0.5, 3.0)) = 1.5

        [Header(Background)]
        _ParallaxFactor ("Parallax Factor", Float) = 0.02
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Background+1"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ShootingStars"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend One One // Additive blending

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            // Maximum number of simultaneous shooting stars
            #define MAX_STARS 8

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
                float4 _StarColor;
                float _Brightness;
                float _StarWidth;
                float _CoreSharpness;
                float _TrailFalloff;
                float _ParallaxFactor;
            CBUFFER_END

            // Set from script - camera data
            float2 _CameraWorldPos;
            float _ScreenAspect;
            float _CameraOrthoSize;

            // Set from script - shooting star data
            int _ActiveStarCount;
            float4 _StarPositions[MAX_STARS];    // xy = head position, zw = tail position (world space)
            float4 _StarParams[MAX_STARS];       // x = brightness, y = progress (0-1), z = width, w = unused

            // Distance from point to line segment
            float distToSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                return length(pa - ba * h);
            }

            // Get position along segment (0 = a, 1 = b)
            float positionOnSegment(float2 p, float2 a, float2 b)
            {
                float2 pa = p - a;
                float2 ba = b - a;
                return saturate(dot(pa, ba) / dot(ba, ba));
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
                // Convert UV to world space
                float2 uv = IN.uv;

                // Calculate world position from UV
                // UV 0-1 maps to camera view
                float2 centeredUV = (uv - 0.5) * 2.0; // -1 to 1
                float2 worldPos;
                worldPos.x = centeredUV.x * _CameraOrthoSize * _ScreenAspect + _CameraWorldPos.x;
                worldPos.y = centeredUV.y * _CameraOrthoSize + _CameraWorldPos.y;

                // Apply parallax
                worldPos += _CameraWorldPos * _ParallaxFactor;

                float3 result = float3(0, 0, 0);

                // Check each active shooting star
                for (int i = 0; i < _ActiveStarCount && i < MAX_STARS; i++)
                {
                    float2 headPos = _StarPositions[i].xy;
                    float2 tailPos = _StarPositions[i].zw;
                    float starBrightness = _StarParams[i].x;
                    float progress = _StarParams[i].y;
                    float width = _StarParams[i].z;

                    // Distance from pixel to the shooting star line
                    float dist = distToSegment(worldPos, tailPos, headPos);

                    // Position along the trail (0 = tail, 1 = head)
                    float posOnLine = positionOnSegment(worldPos, tailPos, headPos);

                    // Calculate star intensity
                    // Core is brightest at head, fades along trail
                    float trailGradient = pow(posOnLine, _TrailFalloff);

                    // Width varies along trail (thinner at tail)
                    float effectiveWidth = width * lerp(0.3, 1.0, posOnLine);

                    // Soft falloff from center of line
                    float coreFalloff = 1.0 - smoothstep(0.0, effectiveWidth, dist);

                    // Sharp bright core at the head
                    float headDist = length(worldPos - headPos);
                    float headCore = 1.0 - smoothstep(0.0, width * _CoreSharpness, headDist);
                    headCore = pow(headCore, 2.0) * 2.0;

                    // Combine trail and head
                    float intensity = (coreFalloff * trailGradient + headCore) * starBrightness;

                    // Fade in at start, fade out at end
                    float fadeIn = smoothstep(0.0, 0.1, progress);
                    float fadeOut = 1.0 - smoothstep(0.9, 1.0, progress);
                    intensity *= fadeIn * fadeOut;

                    result += intensity * _StarColor.rgb;
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
