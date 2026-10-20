Shader "DarkBrine/Nailong Water Droplet"
{
    Properties
    {
        _WaterTint("Water transmission tint",Color)=(.10,.46,.55,1)
        _BodyColour("Water body colour",Color)=(.015,.22,.28,1)
        _Absorption("Water colour absorption",Range(0,1))=.42
        _Visibility("Visibility",Range(0,1))=1
        _RefractionPixels("Water refraction in pixels",Range(0,8))=4
        _UseRefraction("Scene colour available",Float)=1
        _ReflectionStrength("Environment reflection strength",Range(0,1))=.72
        _Smoothness("Water surface smoothness",Range(0,1))=.95
        _SpecularStrength("Wet highlight strength",Range(0,4))=2.4
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _WaterTint, _BodyColour;
                half _Visibility, _RefractionPixels, _UseRefraction, _Absorption;
                half _ReflectionStrength, _Smoothness, _SpecularStrength;
            CBUFFER_END
            struct A {float4 position:POSITION;float3 normal:NORMAL;};
            struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;half fog:TEXCOORD2;};
            V Vert(A i)
            {
                V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(i.normal);o.fog=ComputeFogFactor(o.position.z);return o;
            }
            half4 Frag(V i):SV_Target
            {
                half3 normal=normalize(i.normal),view=GetWorldSpaceNormalizeViewDir(i.world);
                half fresnel=.02+.98*pow(1-saturate(dot(normal,view)),5);
                float2 uv=i.position.xy/_ScaledScreenParams.xy;
                half3 reflected=GlossyEnvironmentReflection(reflect(-view,normal),i.world,1-_Smoothness,1.0,uv);
                Light light=GetMainLight();half3 halfway=SafeNormalize(light.direction+view);
                half ndh=saturate(dot(normal,halfway));
                half highlight=(pow(ndh,lerp(40,220,_Smoothness))+pow(ndh,24)*.22)*light.distanceAttenuation;
                half3 transmitted=_BodyColour.rgb;
                if(_UseRefraction>.5)
                {
                    float2 bend=mul((float3x3)UNITY_MATRIX_V,normal).xy*_RefractionPixels/_ScaledScreenParams.xy;
                    transmitted=SampleSceneColor(saturate(uv+bend))*_WaterTint.rgb;
                }
                transmitted=lerp(transmitted,_BodyColour.rgb,_Absorption);
                half reflectionWeight=lerp(_ReflectionStrength*.22,_ReflectionStrength,fresnel);
                half3 colour=lerp(transmitted,reflected,reflectionWeight)+light.color*highlight*_SpecularStrength;
                colour+=half3(.08,.16,.18)*fresnel;
                half alpha=(_UseRefraction>.5?0.80:0.72)+fresnel*.16+highlight*.12;
                return half4(MixFog(colour,i.fog),saturate(alpha)*_Visibility);
            }
            ENDHLSL
        }
    }
}
