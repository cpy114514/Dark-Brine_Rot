Shader "DarkBrine/Brine Ground Stone"
{
    Properties { _Life("Dissolution",Range(0,1))=0 _Incandescence("Cooling metal",Range(0,1))=0 }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        CBUFFER_START(UnityPerMaterial)
            float _Life,_Incandescence;
        CBUFFER_END
        struct A{float4 position:POSITION;float3 normal:NORMAL;half4 color:COLOR;};
        struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half4 color:COLOR;half fog:TEXCOORD2;};
        V Vert(A i)
        {
            V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);
            o.normal=TransformObjectToWorldNormal(i.normal);o.color=i.color;o.fog=ComputeFogFactor(o.position.z);return o;
        }
        void Dissolve(float3 world)
        {
            float3 p=floor(world*32);float noise=frac(sin(dot(p,float3(12.9898,78.233,45.164)))*43758.5453);
            clip(noise-_Life+.001);
        }
        half4 Frag(V i):SV_Target
        {
            Dissolve(i.world);half3 normal=normalize(i.normal);Light light=GetMainLight(TransformWorldToShadowCoord(i.world));
            half3 diffuse=SampleSH(normal)+light.color*saturate(dot(normal,light.direction))*light.shadowAttenuation;
            half3 color=i.color.rgb*lerp(max(diffuse,.10),half3(1,1,1),_Incandescence);
            return half4(MixFog(color,i.fog),1);
        }
        half4 Depth(V i):SV_Target{Dissolve(i.world);return 0;}
        float3 _LightDirection,_LightPosition;
        V ShadowVert(A i)
        {
            V o=Vert(i);
            #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                float3 direction=normalize(_LightPosition-o.world);
            #else
                float3 direction=_LightDirection;
            #endif
            o.position=ApplyShadowClamping(TransformWorldToHClip(ApplyShadowBias(o.world,o.normal,direction)));return o;
        }
        ENDHLSL
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            ZWrite On Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            ENDHLSL
        }
        Pass
        {
            Tags {"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment Depth
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            ENDHLSL
        }
        Pass
        {
            Tags {"LightMode"="DepthOnly"}
            ZWrite On ColorMask R Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Depth
            ENDHLSL
        }
    }
}
