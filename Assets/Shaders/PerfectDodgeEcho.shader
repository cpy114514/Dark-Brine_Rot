Shader "Mavis/PerfectDodgeEcho"
{
    Properties { _Fade("Fade",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Fade;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION;float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 normal:TEXCOORD1;float fog:TEXCOORD2; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);
                o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(Varyings i):SV_Target
            {
                float edge=pow(1-saturate(abs(dot(normalize(i.normal),GetWorldSpaceNormalizeViewDir(i.world)))),1.4);
                float variation=.7+.3*sin(dot(i.world,float3(9,13,11)));
                half3 color=lerp(half3(.11,.18,.12),half3(.68,.51,.23),edge);
                return half4(MixFog(color,i.fog),_Fade*(.16+.84*edge)*variation);
            }
            ENDHLSL
        }
    }
}
