Shader "DarkBrine/Boss Flame"
{
    Properties { _Intensity("Intensity", Range(0,4)) = 1.3 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            struct A { float4 positionOS:POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; float2 uv:TEXCOORD0; half4 color:COLOR; float4 screen:TEXCOORD1; float depth:TEXCOORD2; };
            CBUFFER_START(UnityPerMaterial) float _Intensity; CBUFFER_END
            V vert(A v){ V o; VertexPositionInputs p=GetVertexPositionInputs(v.positionOS.xyz);o.positionCS=p.positionCS;o.uv=v.uv;o.color=v.color;o.screen=ComputeScreenPos(p.positionCS);o.depth=-p.positionVS.z;return o; }
            float hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(hash(i),hash(i+float2(1,0)),f.x),lerp(hash(i+float2(0,1)),hash(i+1),f.x),f.y);}
            half4 frag(V i):SV_Target
            {
                float n=noise(i.uv*float2(7,5)+float2(_Time.y*.7,-_Time.y*3));
                float y=i.uv.y;
                float x=abs(i.uv.x-.5+sin(y*11-_Time.y*6)*.07*y);
                float width=(.32*(1-y)+.015)*( .7+n*.6);
                float detail=noise(i.uv*float2(15,12)+float2(-_Time.y*.8,-_Time.y*5));
                float silhouette=saturate((width-x)*17)*smoothstep(0,.08,y)*(1-smoothstep(.82,1,y));
                silhouette*=smoothstep(.12,.68,n*.55+detail*.45);
                float core=saturate(1-x/max(.025,width*.55))*(1-y);
                float depth=LinearEyeDepth(SampleSceneDepth(i.screen.xy/i.screen.w),_ZBufferParams);
                float soft=saturate((depth-i.depth)/.3);
                return half4(lerp(i.color.rgb,float3(1,.92,.6),core*.65)*_Intensity*(.75+n*.25),silhouette*i.color.a*soft);
            }
            ENDHLSL
        }
    }
}
