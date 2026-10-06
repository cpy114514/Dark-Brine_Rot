Shader "Dark Brine/Bombardino FX"
{
    Properties { _Color("Color", Color)=(1,1,1,1) _Flame("Flame", Float)=0 _Clock("Clock", Float)=0 }
    SubShader
    {
        Tags {"RenderType"="Transparent" "Queue"="Transparent" "RenderPipeline"="UniversalPipeline"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"} Blend SrcAlpha OneMinusSrcAlpha ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct A {float4 vertex:POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;};
            struct V {float4 vertex:SV_POSITION;float2 uv:TEXCOORD0;float4 color:COLOR;half fog:TEXCOORD1;};
            CBUFFER_START(UnityPerMaterial)
                float4 _Color;float _Flame;float _Clock;
            CBUFFER_END
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p){float2 cell=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);}
            V vert(A v){V o;o.vertex=TransformObjectToHClip(v.vertex.xyz);o.uv=v.uv;o.color=v.color;o.fog=ComputeFogFactor(o.vertex.z);return o;}
            half4 frag(V i):SV_Target
            {
                if(_Flame>.5){float y=i.uv.y;float turbulence=Noise(i.uv*float2(5,7)-float2(0,_Clock*3.2));float fine=Noise(i.uv*float2(11,13)-float2(0,_Clock*5.1));float x=(i.uv.x-.5)*2+(turbulence-.5)*.28*y;float width=(1-y)*(.55+turbulence*.35);float a=(1-smoothstep(width*.45,width+.06,abs(x)))*(1-smoothstep(.60,1,y));a*=smoothstep(.18,.55,turbulence*.7+fine*.3);float3 heat=lerp(float3(1.05,.59,.16),float3(.58,.16,.035),saturate(y+fine*.16));return half4(MixFog(heat,i.fog),a*.7);}
                return half4(MixFog(i.color.rgb*_Color.rgb,i.fog),i.color.a*_Color.a);
            }
            ENDHLSL
        }
    }
}
