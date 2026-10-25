Shader "Hidden/DarkBrine/OceanWavesValidationV244"
{
    SubShader { Pass {
        ZTest Always ZWrite Off Cull Off
        HLSLPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        #pragma target 3.5
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        float _WaveIrregularity, _NearDetailDistance, _MidDetailDistance, _Clock;
        float4 _Wave1, _Wave2, _Wave3, _Wave4;
        float4 _Wave1Motion, _Wave2Motion, _Wave3Motion, _Wave4Motion;
        float4 _Points[128];
        #include "Assets/Ocean/OceanWaves.hlsl"
        struct A { float4 vertex:POSITION; };
        float4 vert(A i):SV_POSITION { return float4(i.vertex.xy*2-1,0,1); }
        float4 frag(float4 pixel:SV_POSITION):SV_Target
        {
            float4 p=_Points[(int)pixel.x];
            float3 d,n;float crest;
            OceanEvaluateWaves(p.xy,_Clock,p.zw,d,n,crest);
            float3 a,an,b,bn,c,cn,e,en;float k;
            OceanEvaluateWaves(p.xy+float2(.04,0),_Clock,p.zw,a,an,k);
            OceanEvaluateWaves(p.xy-float2(.04,0),_Clock,p.zw,b,bn,k);
            OceanEvaluateWaves(p.xy+float2(0,.04),_Clock,p.zw,c,cn,k);
            OceanEvaluateWaves(p.xy-float2(0,.04),_Clock,p.zw,e,en,k);
            float3 numerical=normalize(cross(float3(0,0,.08)+c-e,float3(.08,0,0)+a-b));
            return float4(float3(p.x,0,p.y)+d,1-dot(n,numerical));
        }
        ENDHLSL
    } }
}
