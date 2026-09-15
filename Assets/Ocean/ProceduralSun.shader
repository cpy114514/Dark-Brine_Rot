Shader "DarkBrine/Procedural Sun"
{
    Properties
    {
        _CoreColor ("Core colour", Color) = (1, 0.58, 0.16, 1)
        _GlowColor ("Glow colour", Color) = (1, 0.16, 0.015, 1)
        _Intensity ("HDR intensity", Range(1, 12)) = 5
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend One One
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _CoreColor;
                half4 _GlowColor;
                half _Intensity;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings { float4 positionCS : SV_POSITION; half3 normalWS : TEXCOORD0; half3 viewDirWS : TEXCOORD1; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half facing = saturate(dot(normalize(input.normalWS), normalize(input.viewDirWS)));
                half core = pow(facing, 0.3h);
                half edgeGlow = pow(1.0h - facing, 2.0h);
                half3 colour = (_CoreColor.rgb * core + _GlowColor.rgb * edgeGlow * 0.45h) * _Intensity;
                return half4(colour, 1);
            }
            ENDHLSL
        }
    }
}
