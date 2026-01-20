Shader "Starfire/PostProcess/DirectionalChromaticAberration"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _ChromaticIntensity ("Chromatic Intensity", Range(0, 1)) = 0.5
        _PixelOffset ("Pixel Offset", Range(0, 50)) = 10
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "DirectionalChromaticAberration"

            ZWrite Off
            ZTest Always
            Cull Off

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

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            float4 _MainTex_TexelSize;

            float _ChromaticIntensity;
            float _PixelOffset;

            // Warp effect globals (set by WarpEffectController)
            float _WarpIntensity;
            float2 _WarpDirection;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;

                // Calculate offset along warp direction
                // _WarpDirection is normalized, already set by WarpEffectController
                float2 dir = _WarpDirection;

                // Total intensity combines global warp intensity with local chromatic intensity
                float intensity = _WarpIntensity * _ChromaticIntensity;

                // Calculate UV offset in texel space
                // _MainTex_TexelSize.xy = (1/width, 1/height)
                float2 texelOffset = dir * intensity * _PixelOffset * _MainTex_TexelSize.xy;

                // Sample RGB at different positions along velocity direction
                // Red shifts forward (in direction of travel)
                // Green stays centered
                // Blue shifts backward (opposite direction of travel)
                float r = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + texelOffset).r;
                float g = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).g;
                float b = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - texelOffset).b;

                // Also sample alpha from center (shouldn't matter for fullscreen, but be safe)
                float a = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv).a;

                return half4(r, g, b, a);
            }

            ENDHLSL
        }
    }
}
