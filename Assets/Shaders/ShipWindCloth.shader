Shader "DarkBrine/Ship Wind Cloth"
{
    Properties
    {
        _BaseMap("Original sail texture",2D)="white"{}
        _BaseColor("Tint",Color)=(1,1,1,1)
        _Smoothness("Smoothness",Range(0,1))=.067
        _Billow("Billow metres",Float)=1.8
        _Flutter("Edge flutter metres",Float)=.25
        _Frequency("Flutter frequency",Float)=6
        _Phase("Panel phase",Float)=0
        [HideInInspector]_WindLocal("Wind in object units",Vector)=(0,0,0,0)
        [HideInInspector]_SailTime("Game clock",Float)=0
    }
    SubShader
    {
        Tags{"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry"}
        Cull Off
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        TEXTURE2D(_BaseMap);SAMPLER(sampler_BaseMap);
        CBUFFER_START(UnityPerMaterial)
        float4 _BaseMap_ST,_BaseColor,_WindLocal;
        float _Smoothness,_Billow,_Flutter,_Frequency,_Phase,_SailTime;
        CBUFFER_END
        struct Attributes{float4 positionOS:POSITION;float3 normalOS:NORMAL;float2 uv:TEXCOORD0;float4 color:COLOR;};
        float3 ClothPosition(Attributes i)
        {
            float phase=i.color.a*6.283185+_Phase;
            float gust=.72+.18*sin(_SailTime*.83+_Phase)+.10*sin(_SailTime*1.71+phase);
            float ripple=sin(_SailTime*_Frequency-phase*2)*.65+sin(_SailTime*(_Frequency*1.47)-phase*3.1)*.35;
            float displacement=i.color.r*(_Billow*gust+_Flutter*i.color.g*ripple);
            return i.positionOS.xyz+_WindLocal.xyz*displacement;
        }
        ENDHLSL
        Pass
        {
            Name "ForwardLit"
            Tags{"LightMode"="UniversalForward"}
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog
            struct Varyings{float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;float3 normalWS:TEXCOORD1;float2 uv:TEXCOORD2;half fog:TEXCOORD3;};
            Varyings Vert(Attributes i)
            {
                Varyings o;VertexPositionInputs p=GetVertexPositionInputs(ClothPosition(i));
                o.positionCS=p.positionCS;o.positionWS=p.positionWS;o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.uv=TRANSFORM_TEX(i.uv,_BaseMap);o.fog=ComputeFogFactor(p.positionCS.z);return o;
            }
            half4 Frag(Varyings i,FRONT_FACE_TYPE face:FRONT_FACE_SEMANTIC):SV_Target
            {
                half4 albedo=SAMPLE_TEXTURE2D(_BaseMap,sampler_BaseMap,i.uv)*_BaseColor;
                half3 geometric=normalize(cross(ddy(i.positionWS),ddx(i.positionWS)));
                half3 normal=dot(geometric,i.normalWS)<0 ? -geometric : geometric;
                normal*=IS_FRONT_VFACE(face,1,-1);
                InputData input=(InputData)0;input.positionWS=i.positionWS;input.normalWS=normal;
                input.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);input.shadowCoord=TransformWorldToShadowCoord(i.positionWS);
                input.bakedGI=SampleSH(normal);input.shadowMask=half4(1,1,1,1);input.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);
                SurfaceData surface=(SurfaceData)0;surface.albedo=albedo.rgb;surface.alpha=1;surface.occlusion=1;surface.smoothness=_Smoothness;surface.normalTS=half3(0,0,1);surface.specular=half3(.04,.04,.04);
                half4 color=UniversalFragmentPBR(input,surface);color.rgb=MixFog(color.rgb,i.fog);return color;
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags{"LightMode"="ShadowCaster"}
            ZWrite On ZTest LEqual ColorMask 0
            HLSLPROGRAM
            #pragma vertex ShadowVert
            #pragma fragment DepthFrag
            float3 _LightDirection;
            float4 ShadowVert(Attributes i):SV_POSITION
            {
                float3 world=TransformObjectToWorld(ClothPosition(i));float3 normal=TransformObjectToWorldNormal(i.normalOS);
                float4 p=TransformWorldToHClip(ApplyShadowBias(world,normal,_LightDirection));
                #if UNITY_REVERSED_Z
                p.z=min(p.z,UNITY_NEAR_CLIP_VALUE*p.w);
                #else
                p.z=max(p.z,UNITY_NEAR_CLIP_VALUE*p.w);
                #endif
                return p;
            }
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags{"LightMode"="DepthOnly"}
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma vertex DepthVert
            #pragma fragment DepthFrag
            float4 DepthVert(Attributes i):SV_POSITION{return TransformObjectToHClip(ClothPosition(i));}
            half4 DepthFrag():SV_Target{return 0;}
            ENDHLSL
        }
    }
}
