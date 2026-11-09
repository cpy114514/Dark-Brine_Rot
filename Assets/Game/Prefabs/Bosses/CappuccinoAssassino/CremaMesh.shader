Shader "DarkBrine/Crema Mesh"
{
    Properties { _Tint("Tint",Color)=(1,.7,.3,1) _Radius("Radius",Float)=1 _Ring("Annulus",Float)=0 _Mode("Surface mode",Float)=0 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; half4 color:COLOR; float3 normalOS:NORMAL;float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; half4 color:COLOR; float3 world:TEXCOORD0;float2 uv:TEXCOORD1;float3 normal:TEXCOORD2; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint; float _Radius; float _Ring;float _Mode;
            CBUFFER_END
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p)
            {
                float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);
            }
            V Vert(A input)
            {
                V o;float3 p=input.positionOS.xyz;
                if(_Ring>.5){p=float3(p.x*(_Radius+p.y),0,p.z*(_Radius+p.y));}
                if(_Mode>1.5&&_Mode<2.5)p.xz+=sin(input.uv.x*17+_Time.y*1.8+p.zx)*.13;
                o.world=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.world);o.color=input.color*_Tint;
                o.uv=input.uv;o.normal=TransformObjectToWorldNormal(input.normalOS);return o;
            }
            half4 Frag(V input):SV_Target
            {
                float alpha=input.color.a;float3 color=input.color.rgb;
                if(_Mode<.5)
                {
                    float rim=pow(1-saturate(abs(dot(normalize(input.normal),GetWorldSpaceNormalizeViewDir(input.world)))),2);
                    float dissolve=smoothstep(.18,.65,Noise(input.world.xz*3+float2(0,_Time.y*1.4)));
                    alpha*=lerp(.045,1,rim)*dissolve;color*=lerp(.4,1.5,rim);
                }
                else if(_Mode<1.5)
                {
                    float flow=Noise(float2(input.uv.x*40-_Time.y*2,input.uv.y*4+_Time.y*1.5));
                    float edge=pow(saturate(sin(input.uv.y*3.14159)),2);
                    alpha*=edge*lerp(.4,1,flow);color=lerp(color*.45,float3(1.7,1.32,.78),pow(flow,3));
                }
                else if(_Mode<2.5)
                {
                    float n=Noise(float2(input.uv.x*11-_Time.y*.9,input.uv.y*3+_Time.y*.3));
                    float wisps=Noise(float2(input.uv.x*24-_Time.y*1.5,input.uv.y*7));
                    alpha*=pow(saturate(sin(input.uv.y*3.14159)),1.6)*smoothstep(.2,.75,n)*lerp(.5,1,wisps);
                    color=lerp(float3(.28,.14,.075),color,n);
                }
                else
                {
                    float edge=pow(saturate(sin(input.uv.y*3.14159)),.65);
                    float streak=lerp(.7,1,Noise(float2(input.uv.x*15-_Time.y*4,input.uv.y*2)));
                    alpha*=edge*streak;color=lerp(float3(.23,.075,.025),color,pow(input.uv.y,.45));
                }
                return half4(color,alpha);
            }
            ENDHLSL
        }
    }
}
