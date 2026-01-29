Shader "Starfire/ProjectileTrail"
{
    Properties
    {
        [Header(Color)]
        _Color ("Trail Color", Color) = (1, 1, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0.5, 5.0)) = 1.5

        [Header(Fade)]
        _FalloffPower ("Falloff Power", Range(0.5, 4.0)) = 2.0
        _Softness ("Edge Softness", Range(0.0, 1.0)) = 0.3

        [Header(Shape)]
        _TrailLength ("Trail Length", Float) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "IgnoreProjector" = "True"
        }

        Pass
        {
            Name "ProjectileTrail"

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One One // Additive blending for glow effect

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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
                float4 _Color;
                float _GlowIntensity;
                float _FalloffPower;
                float _Softness;
                float _TrailLength;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                // Stretch Y by trail length (quad goes from y=0 to y=1)
                // This allows trail length to be controlled via shader uniform
                // instead of transform scale, avoiding transform hierarchy sync
                float3 pos = IN.positionOS.xyz;
                pos.y *= _TrailLength;

                OUT.positionHCS = TransformObjectToHClip(pos);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // UV.y represents distance from head (0) to tail (1)
                // UV.x represents horizontal position (-0.5 to 0.5 centered)
                float distFromHead = uv.y;
                float distFromCenter = abs(uv.x - 0.5) * 2.0; // 0 at center, 1 at edges

                // Length-based fade (head to tail)
                // pow() creates non-linear falloff for more natural look
                float lengthFade = pow(1.0 - distFromHead, _FalloffPower);

                // Edge softness - creates smooth edges
                float edgeFade = 1.0 - smoothstep(1.0 - _Softness, 1.0, distFromCenter);

                // Combine fades
                float alpha = lengthFade * edgeFade * _Color.a;

                // Apply glow intensity to color
                float3 color = _Color.rgb * _GlowIntensity;

                // Final output with additive blending
                return half4(color * alpha, alpha);
            }

            ENDHLSL
        }
    }

    // Fallback for non-URP
    FallBack "Sprites/Default"
}
