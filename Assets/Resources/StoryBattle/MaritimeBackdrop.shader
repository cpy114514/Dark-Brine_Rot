Shader "Dark Brine/Maritime Battle"
{
    Properties { _Sky("Sky", Float)=0 _BattleTime("Battle clock", Float)=0 }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Tags { "LightMode"="UniversalForward" }
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; float3 world:TEXCOORD0; float2 uv:TEXCOORD1; };
            CBUFFER_START(UnityPerMaterial)
                float _Sky; float _BattleTime;
            CBUFFER_END
            V vert(A v) { V o; o.world=TransformObjectToWorld(v.positionOS.xyz); o.positionCS=TransformWorldToHClip(o.world);o.uv=v.uv;return o; }
            float hash(float2 p){ return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453); }
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float t=_BattleTime;
                if(_Sky>.5)
                {
                    float2 uv=i.uv;float h=saturate((uv.y-.08)*3);
                    float3 sky=lerp(float3(.50,.68,.69),float3(.035,.16,.25),smoothstep(0,1,h));
                    float2 p=uv*float2(24,14)+float2(t*.026,0);
                    float cloud=noise(p)*.65+noise(p*2.1)*.25+noise(p*4.5)*.1;
                    float band=smoothstep(.10,.18,uv.y)*(1-smoothstep(.50,.80,uv.y));
                    sky=lerp(sky,float3(.56,.66,.67),smoothstep(.40,.64,cloud)*band*.85);
                    float sun=length((uv-float2(.42,.21))*float2(3.8,1));
                    sky+=float3(.65,.35,.14)*exp(-sun*14)*.32;
                    sky=lerp(sky,float3(1,.86,.57),1-smoothstep(.019,.022,sun));
                    return half4(sky,1);
                }
                float2 p=i.world.xz;float warp=noise(p*.13+float2(t*.02,0))*3;float a=sin(p.x*.55+p.y*.28+warp-t*1.8), b=sin(p.y*.85-p.x*.18+warp*.7-t*1.1);
                float ripple=sin(p.x*2.2+p.y*1.7-t*3.2)*.5+.5;
                float3 sea=lerp(float3(.025,.13,.19),float3(.06,.42,.46),saturate(a*.24+b*.16+.5));
                float crest=smoothstep(.86,.99,a*.5+.5)*( .35+.65*noise(p*.8+float2(t*.2,0)) );
                float foam=smoothstep(.65,.83,crest)*(.4+.6*ripple);
                sea=lerp(sea,float3(.56,.85,.82),foam*.7);
                sea+=pow(ripple,18)*float3(.07,.17,.18)*(.5+.5*b)*noise(p*.3);
                float distanceFog=saturate(distance(i.world,GetCameraPositionWS())/150);
                return half4(lerp(sea,float3(.35,.58,.60),distanceFog*.6),1);
            }
            ENDHLSL
        }
    }
}
