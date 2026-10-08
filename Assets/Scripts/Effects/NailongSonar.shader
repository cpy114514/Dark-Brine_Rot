Shader "DarkBrine/Sonar Ring"
{
    Properties
    {
        _Tint("Sonar colour",Color)=(0.45,0.92,1,1)
        _Opacity("Pulse visibility",Range(0,1))=1
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Opacity;
            CBUFFER_END
            struct A {float4 position:POSITION;float2 uv:TEXCOORD0;};
            struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1;};
            V vert(A input){V o;o.position=TransformObjectToHClip(input.position.xyz);o.uv=input.uv;o.fog=ComputeFogFactor(o.position.z);return o;}
            half4 frag(V i):SV_Target
            {
                half edge=smoothstep(0,.18,i.uv.y)*(1-smoothstep(.82,1,i.uv.y));
                half core=1-smoothstep(.06,.32,abs(i.uv.y-.5));
                half3 colour=lerp(_Tint.rgb,half3(1,1,1),core*.85);
                return half4(MixFog(colour,i.fog),edge*_Opacity*_Tint.a);
            }
            ENDHLSL
        }
    }
}
