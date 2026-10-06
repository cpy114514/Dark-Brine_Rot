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
                float2 centred = uv * 2.0 - 1.0;
                float vignette = saturate(1.0 - dot(centred, centred) * 0.36);

                // Absorption remains subtle; caustics belong on submerged
                // geometry rather than being drawn over the entire camera image.
                half alpha = saturate(_Intensity * (0.35 + (1.0 - vignette) * 0.10));
                return half4(_Tint.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
