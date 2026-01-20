Shader "Starfire/EdgeLightingOverlay"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}

        [Header(Edge Lighting)]
        _EdgeBrightness ("Edge Brightness", Range(0, 3)) = 1.0
        _EdgeTint ("Edge Tint", Color) = (1,1,1,1)
        _EdgeWidth ("Edge Width (pixels)", Range(0.5, 3)) = 1.0
        _EdgeSoftness ("Edge Softness", Range(0, 1)) = 0.1
        _MinEdgeGlow ("Minimum Edge Glow", Range(0, 0.5)) = 0.0

        [Header(Light Falloff)]
        _FalloffExponent ("Falloff Exponent", Range(0.5, 4)) = 2.0

        [Header(Emissive)]
        _EmissiveIntensity ("Emissive Intensity", Range(0, 5)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+1"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "EdgeOverlay"

            Blend One One
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            #define MAX_EDGE_LIGHTS 8
            #define PI 3.14159265359

            float4 _EdgeLightPositions[MAX_EDGE_LIGHTS];  // xyz = position, w = range
            float4 _EdgeLightColors[MAX_EDGE_LIGHTS];     // rgb = color, a = intensity
            float4 _EdgeLightParams[MAX_EDGE_LIGHTS];     // xy = direction, z = inner angle, w = outer angle
            int _EdgeLightCount;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            CBUFFER_START(UnityPerMaterial)
                float _EdgeBrightness;
                float4 _EdgeTint;
                float _EdgeWidth;
                float _EdgeSoftness;
                float _MinEdgeGlow;
                float _FalloffExponent;
                float _EmissiveIntensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float SampleAlpha(float2 uv)
            {
                // Treat out-of-bounds UV samples as transparent
                // This fixes edge detection at texture boundaries
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return 0.0;
                }
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            float DetectEdgeAtRadius(float2 uv, float2 ts)
            {
                // Sobel-style 8-sample gradient calculation
                float tl = SampleAlpha(uv + float2(-ts.x, ts.y));
                float t  = SampleAlpha(uv + float2(0, ts.y));
                float tr = SampleAlpha(uv + float2(ts.x, ts.y));
                float l  = SampleAlpha(uv + float2(-ts.x, 0));
                float r  = SampleAlpha(uv + float2(ts.x, 0));
                float bl = SampleAlpha(uv + float2(-ts.x, -ts.y));
                float b  = SampleAlpha(uv + float2(0, -ts.y));
                float br = SampleAlpha(uv + float2(ts.x, -ts.y));

                // Sobel gradients (weighted for better diagonal handling)
                float gx = (tr + 2.0 * r + br) - (tl + 2.0 * l + bl);
                float gy = (tl + 2.0 * t + tr) - (bl + 2.0 * b + br);

                return length(float2(gx, gy));
            }

            float DetectEdge(float2 uv, float2 texelSize, float centerAlpha)
            {
                if (centerAlpha < 0.01) return 0;

                // Sample at multiple radii for robustness (1/3, 2/3, and full width)
                float edge1 = DetectEdgeAtRadius(uv, texelSize * 0.333);
                float edge2 = DetectEdgeAtRadius(uv, texelSize * 0.666);
                float edge3 = DetectEdgeAtRadius(uv, texelSize);

                // Take maximum edge strength across all radii
                float maxEdge = max(max(edge1, edge2), edge3);

                // Normalize gradient magnitude to 0-1 range
                // Sobel max theoretical output is ~4 for a perfect edge, but typically ~2-3
                float edge = saturate(maxEdge * 0.4);

                // Apply softness - blend based on center alpha proximity to edge
                edge = lerp(edge, edge * centerAlpha, _EdgeSoftness);

                return edge;
            }

            float2 CalculateEdgeNormal(float2 uv, float2 texelSize)
            {
                // 8-sample Sobel for better gradient accuracy
                float tl = SampleAlpha(uv + float2(-texelSize.x, texelSize.y));
                float t  = SampleAlpha(uv + float2(0, texelSize.y));
                float tr = SampleAlpha(uv + float2(texelSize.x, texelSize.y));
                float l  = SampleAlpha(uv + float2(-texelSize.x, 0));
                float r  = SampleAlpha(uv + float2(texelSize.x, 0));
                float bl = SampleAlpha(uv + float2(-texelSize.x, -texelSize.y));
                float b  = SampleAlpha(uv + float2(0, -texelSize.y));
                float br = SampleAlpha(uv + float2(texelSize.x, -texelSize.y));

                // Sobel operator for gradient
                float gx = (tr + 2.0 * r + br) - (tl + 2.0 * l + bl);
                float gy = (tl + 2.0 * t + tr) - (bl + 2.0 * b + br);

                float gradLen = length(float2(gx, gy));

                // Normal points outward (opposite of gradient direction toward opaque)
                if (gradLen > 0.001)
                {
                    return float2(-gx, -gy) / gradLen;
                }

                // Fallback: find direction to nearest transparent pixel
                float2 toTrans = float2(0, 0);
                if (l < 0.5) toTrans.x -= 1.0;
                if (r < 0.5) toTrans.x += 1.0;
                if (b < 0.5) toTrans.y -= 1.0;
                if (t < 0.5) toTrans.y += 1.0;
                if (tl < 0.5) toTrans += float2(-0.707, 0.707);
                if (tr < 0.5) toTrans += float2(0.707, 0.707);
                if (bl < 0.5) toTrans += float2(-0.707, -0.707);
                if (br < 0.5) toTrans += float2(0.707, -0.707);

                float transLen = length(toTrans);
                if (transLen > 0.001)
                {
                    return toTrans / transLen;
                }

                // Last resort fallback - point upward
                return float2(0, 1);
            }

            float3 CalculateEdgeLighting(float2 worldPos, float2 edgeNormal)
            {
                float3 totalLight = float3(0, 0, 0);

                for (int i = 0; i < _EdgeLightCount && i < MAX_EDGE_LIGHTS; i++)
                {
                    float3 lightPos = _EdgeLightPositions[i].xyz;
                    float lightRange = _EdgeLightPositions[i].w;
                    float3 lightColor = _EdgeLightColors[i].rgb;
                    float lightIntensity = _EdgeLightColors[i].a;

                    // Light direction and angle parameters
                    float2 lightDir = _EdgeLightParams[i].xy;
                    float innerAngle = _EdgeLightParams[i].z;
                    float outerAngle = _EdgeLightParams[i].w;

                    if (lightRange <= 0.001 || lightIntensity <= 0.001)
                    {
                        continue;
                    }

                    float2 toLight = lightPos.xy - worldPos;
                    float dist = length(toLight);
                    float2 toLightDir = dist > 0.001 ? toLight / dist : float2(0, 1);

                    // Distance attenuation
                    float normalizedDist = saturate(dist / lightRange);
                    float attenuation = pow(1.0 - normalizedDist, _FalloffExponent);

                    // Apply spot light angle falloff (if not omnidirectional)
                    // outerAngle >= 360 means omnidirectional (point light)
                    if (outerAngle < 359.0)
                    {
                        // Direction from light to pixel (opposite of toLightDir)
                        float2 toPixelDir = -toLightDir;

                        // Angle between light's forward direction and direction to pixel
                        float cosAngle = dot(lightDir, toPixelDir);
                        float angle = acos(clamp(cosAngle, -1.0, 1.0)) * (180.0 / PI);

                        // Smooth falloff between inner and outer angle
                        float halfInner = innerAngle * 0.5;
                        float halfOuter = outerAngle * 0.5;
                        float angleFalloff = 1.0 - saturate((angle - halfInner) / max(halfOuter - halfInner, 0.001));

                        attenuation *= angleFalloff;
                    }

                    // Edge normal directional factor
                    float directional = saturate(dot(edgeNormal, toLightDir));
                    directional = lerp(_MinEdgeGlow, 1.0, directional);

                    totalLight += lightColor * lightIntensity * attenuation * directional;
                }

                return totalLight;
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.worldPos = worldPos.xy;

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float centerAlpha = SampleAlpha(IN.uv);

                if (centerAlpha < 0.01)
                {
                    return half4(0, 0, 0, 0);
                }

                float2 texelSize = _MainTex_TexelSize.xy * _EdgeWidth;

                float edge = DetectEdge(IN.uv, texelSize, centerAlpha);

                if (edge < 0.001 || _EdgeLightCount <= 0)
                {
                    return half4(0, 0, 0, 0);
                }

                float2 edgeNormal = CalculateEdgeNormal(IN.uv, texelSize);
                float3 edgeLight = CalculateEdgeLighting(IN.worldPos, edgeNormal);
                edgeLight *= _EdgeTint.rgb * _EdgeBrightness * edge;

                // Apply emissive intensity for HDR/bloom support
                edgeLight *= _EmissiveIntensity;

                return half4(edgeLight, 0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
