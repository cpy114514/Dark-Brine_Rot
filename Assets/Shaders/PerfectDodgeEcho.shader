Shader "Mavis/PerfectDodgeEcho"
{
    Properties { _Fade("Fade",Float)=0 _Life("Dissolve progress",Range(0,1))=0 }
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
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
            float _Fade,_Life;
            CBUFFER_END
            struct Attributes { float4 positionOS:POSITION;float3 normalOS:NORMAL; };
            struct Varyings { float4 positionCS:SV_POSITION;float3 world:TEXCOORD0;float3 local:TEXCOORD1;float3 normal:TEXCOORD2;float fog:TEXCOORD3; };
            Varyings Vert(Attributes v)
            {
                Varyings o;o.world=TransformObjectToWorld(v.positionOS.xyz);o.local=v.positionOS.xyz;
                o.positionCS=TransformWorldToHClip(o.world);o.normal=TransformObjectToWorldNormal(v.normalOS);o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            float Hash(float3 p){p=frac(p*.1031);p+=dot(p,p.yzx+33.33);return frac((p.x+p.y)*p.z);}
            float Noise(float3 p)
            {
                float3 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(lerp(Hash(i),Hash(i+float3(1,0,0)),f.x),lerp(Hash(i+float3(0,1,0)),Hash(i+float3(1,1,0)),f.x),f.y),
                    lerp(lerp(Hash(i+float3(0,0,1)),Hash(i+float3(1,0,1)),f.x),lerp(Hash(i+float3(0,1,1)),Hash(i+float3(1,1,1)),f.x),f.y),f.z);
            }
            half4 Frag(Varyings i):SV_Target
            {
                float3 normal=normalize(i.normal);
                float edge=pow(1-saturate(abs(dot(normal,GetWorldSpaceNormalizeViewDir(i.world)))),1.5);
                float grain=Noise(i.local*10)*.65+Noise(i.local*26)*.35;
                float cut=saturate((_Life-.18)/.82);
                float intact=1-smoothstep(grain-.09,grain+.09,cut);
                float light=.35+.65*saturate(dot(normal,normalize(float3(.3,.8,-.2))));
                half3 body=lerp(half3(.14,.17,.18),half3(.48,.52,.52),light);
                half3 color=lerp(body,half3(.92,.85,.66),edge);
                return half4(MixFog(color,i.fog),_Fade*(.34+.66*edge)*intact);
            }
            ENDHLSL
        }
    }
}
