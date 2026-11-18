Shader "Dark Brine/Second Island/Ground Transition"
{
    Properties
    {
        _BaseMap("Ground detail",2D)="white"{}
        _BaseColor("Tint",Color)=(.8,.72,.66,1)
        _Opacity("Feathered deposit",Range(0,1))=.3
        _ChocolateBank("Chocolate shoreline", Range(0,1))=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent-25"}
        Pass
        {
            ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/EnvironmentVisualStyle.hlsl"
            TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;float _Opacity,_ChocolateBank;
            CBUFFER_END
            struct A{float4 p:POSITION;float3 n:NORMAL;float2 uv:TEXCOORD0;float2 edge:TEXCOORD1;};
            struct V{float4 p:SV_POSITION;half3 n:TEXCOORD0;float2 uv:TEXCOORD1;float2 edge:TEXCOORD2;half fog:TEXCOORD3;float3 world:TEXCOORD4;};
            V Vert(A i){V o;o.world=TransformObjectToWorld(i.p.xyz);o.p=TransformWorldToHClip(o.world);o.n=TransformObjectToWorldNormal(i.n);o.uv=i.uv;o.edge=i.edge;o.fog=ComputeFogFactor(o.p.z);return o;}
            half4 Frag(V i):SV_Target
            {
                float2 uv=_ChocolateBank>.5?i.world.xz*.22:i.uv;
                half3 tex=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,uv).rgb;half luma=dot(tex,half3(.2126,.7152,.0722));
                Light sun=GetMainLight();half3 illumination=max(SampleSH(normalize(i.n)),.35)+sun.color*saturate(dot(normalize(i.n),sun.direction))*.65;
                half3 c=lerp(luma.xxx,tex,.16)*_BaseColor.rgb*illumination;
                half fade=1-smoothstep(.1+luma*.12,.5,abs(i.edge.x-.5));
                if (_ChocolateBank>.5)
                {
                    half coarse=dot(SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.world.xz*.035).rgb,half3(.2126,.7152,.0722));
                    half span=.28+coarse*.66;
                    fade=(1-smoothstep(.07,span,saturate(i.uv.x)))*lerp(.52,1,luma);
                    c=_BaseColor.rgb*illumination*lerp(.54,1.1,luma);
                }
                c=StyleEnvironmentColor(MixFog(c,i.fog),GetNormalizedScreenSpaceUV(i.p),0);return half4(c,fade*_Opacity);
            }
            ENDHLSL
        }
    }
}
