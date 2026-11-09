Shader "DarkBrine/Crema Mesh"
{
    Properties { _Tint("Tint",Color)=(1,.7,.3,1) _Radius("Radius",Float)=1 _Ring("Annulus",Float)=0 }
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
            struct A { float4 positionOS:POSITION; half4 color:COLOR; };
            struct V { float4 positionCS:SV_POSITION; half4 color:COLOR; float3 world:TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint; float _Radius; float _Ring;
            CBUFFER_END
            V Vert(A input)
            {
                V o;float3 p=input.positionOS.xyz;
                if(_Ring>.5){p=float3(p.x*(_Radius+p.y),0,p.z*(_Radius+p.y));}
                o.world=TransformObjectToWorld(p);o.positionCS=TransformWorldToHClip(o.world);o.color=input.color*_Tint;return o;
            }
            half4 Frag(V input):SV_Target
            {
                float ripple=.82+.18*sin(input.world.y*14+input.world.x*4-_Time.y*8);
                return half4(input.color.rgb,input.color.a*ripple);
            }
            ENDHLSL
        }
    }
}
