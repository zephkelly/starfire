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

            float4 _EdgeLightPositions[MAX_EDGE_LIGHTS];
            float4 _EdgeLightColors[MAX_EDGE_LIGHTS];
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
                return SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;
            }

            float DetectEdge(float2 uv, float2 texelSize, float centerAlpha)
            {
                if (centerAlpha < 0.1)
                {
                    return 0;
                }

                float left   = SampleAlpha(uv + float2(-texelSize.x, 0));
                float right  = SampleAlpha(uv + float2( texelSize.x, 0));
                float up     = SampleAlpha(uv + float2(0,  texelSize.y));
                float down   = SampleAlpha(uv + float2(0, -texelSize.y));
                float tl     = SampleAlpha(uv + float2(-texelSize.x,  texelSize.y));
                float tr     = SampleAlpha(uv + float2( texelSize.x,  texelSize.y));
                float bl     = SampleAlpha(uv + float2(-texelSize.x, -texelSize.y));
                float br     = SampleAlpha(uv + float2( texelSize.x, -texelSize.y));

                float minCardinal = min(min(left, right), min(up, down));
                float minDiagonal = min(min(tl, tr), min(bl, br));
                float minNeighbor = min(minCardinal, minDiagonal);

                float alphaThreshold = 0.5;
                float isOpaque = step(alphaThreshold, centerAlpha);
                float hasTransparentNeighbor = 1.0 - step(alphaThreshold, minNeighbor);

                float edge = isOpaque * hasTransparentNeighbor;

                float alphaGradient = saturate((centerAlpha - minNeighbor) / max(_EdgeSoftness, 0.01));
                edge = lerp(edge, edge * alphaGradient, _EdgeSoftness);

                return edge;
            }

            float2 CalculateEdgeNormal(float2 uv, float2 texelSize)
            {
                float left  = SampleAlpha(uv + float2(-texelSize.x, 0));
                float right = SampleAlpha(uv + float2( texelSize.x, 0));
                float up    = SampleAlpha(uv + float2(0,  texelSize.y));
                float down  = SampleAlpha(uv + float2(0, -texelSize.y));

                float2 gradient = float2(right - left, up - down);
                float2 normal = -gradient;

                float len = length(normal);
                return len > 0.001 ? normal / len : float2(0, 0);
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

                    if (lightRange <= 0.001 || lightIntensity <= 0.001)
                    {
                        continue;
                    }

                    float2 toLight = lightPos.xy - worldPos;
                    float dist = length(toLight);
                    float2 toLightDir = dist > 0.001 ? toLight / dist : float2(0, 1);

                    float normalizedDist = saturate(dist / lightRange);
                    float attenuation = pow(1.0 - normalizedDist, _FalloffExponent);

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

                return half4(edgeLight, 0);
            }
            ENDHLSL
        }
    }

    Fallback Off
}
