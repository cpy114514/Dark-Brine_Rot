Shader "DarkBrine/Underwater Bubble"
{
    Properties
    {
        _Tint ("Bubble Tint", Color) = (0.78, 0.96, 1.0, 0.72)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "UnderwaterBubble"
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Tint;
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float radius = length(input.uv * 2.0 - 1.0);
                float rim = smoothstep(0.58, 0.76, radius) * (1.0 - smoothstep(0.82, 1.0, radius));
                float highlight = 1.0 - smoothstep(0.02, 0.28, length(input.uv - float2(0.34, 0.68)));
                half alpha = input.color.a * saturate(rim + highlight * 0.58);
                return half4(input.color.rgb + highlight * 0.22, alpha);
            }
            ENDHLSL
        }
    }
}
