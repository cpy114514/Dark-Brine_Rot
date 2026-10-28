Shader "Mavis/SandFootprint"
{
    Properties
    {
        _Color ("Color", Color) = (0.33, 0.22, 0.11, 0.62)
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "Footprint"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                return OUT;
            }
            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 2.0 - 1.0;
                float oval = p.x * p.x + p.y * p.y * 2.1;
                float heel = (p.x + 0.15) * (p.x + 0.15) * 1.8 + (p.y + 0.55) * (p.y + 0.55) * 3.4;
                float shape = min(oval, heel);
                clip(1.0 - shape);
                float edge = saturate(1.0 - shape);
                edge *= edge;
                return half4(_Color.rgb, _Color.a * edge * IN.color.a);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
