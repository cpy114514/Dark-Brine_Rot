Shader "DarkBrine/Underwater Overlay"
{
    Properties
    {
        _Tint ("Water Tint", Color) = (0.025, 0.24, 0.32, 1)
        _Intensity ("Intensity", Range(0, 1)) = 0.68
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "UnderwaterOverlay"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Intensity;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float time = _Time.y;
                float2 centred = uv * 2.0 - 1.0;
                float vignette = saturate(1.0 - dot(centred, centred) * 0.36);

                // Two moving interference patterns make restrained caustics
                // without requiring a texture or a screen-space post effect.
                float causticA = sin((uv.x * 17.0 + uv.y * 8.0) + time * 1.25);
                float causticB = sin((uv.x * -10.0 + uv.y * 19.0) - time * 0.85);
                float caustics = pow(saturate((causticA + causticB) * 0.5), 5.0) * vignette;
                float surfaceDrift = sin(uv.y * 34.0 + time * 2.0 + sin(uv.x * 8.0)) * 0.018;

                half alpha = saturate(_Intensity * (0.43 + (1.0 - vignette) * 0.20 + surfaceDrift));
                half3 color = _Tint.rgb + half3(0.08, 0.18, 0.16) * caustics;
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
