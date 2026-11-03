Shader "DarkBrine/Quest Guiding Thread"
{
    Properties{_Fade("Visibility",Float)=1 _Clock("Flow time",Float)=0}
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+12" "RenderType"="Transparent"}
        Pass
        {
            Tags{"LightMode"="UniversalForward"}
            Blend One OneMinusSrcAlpha ZWrite Off Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Fade,_Clock;
            CBUFFER_END
            struct A{float4 position:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;half4 color:COLOR;};
            struct V{float4 position:SV_POSITION;float2 uv:TEXCOORD0;half4 color:COLOR;half fog:TEXCOORD1;};
            V Vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.uv=i.uv;o.color=i.color;o.fog=ComputeFogFactor(o.position.z);return o;}
            half4 Frag(V i):SV_Target
            {
                float flow=frac(i.uv.x*.11-_Clock*.32);
                float head=exp(-pow((flow-.45)*10,2));
                float detail=.7+.3*sin(i.uv.x*3.7-_Clock*3);
                float alpha=i.color.a*_Fade*(.74+head*.26)*detail;
                half3 color=i.color.rgb*(1.05+head*.65);
                color=MixFog(color,i.fog);return half4(color*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
