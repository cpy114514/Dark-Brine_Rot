Shader "Hidden/DarkBrine/VanGoghSkinAnchor"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            ZWrite Off ZTest Always Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D_X_FLOAT(_VGDepth); SAMPLER(sampler_VGDepth);
            float4 _VGIdentity;
            struct A { float4 positionOS:POSITION;float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION;float2 uv:TEXCOORD0; };
            V Vert(A a){V v;v.positionCS=TransformObjectToHClip(a.positionOS.xyz);v.uv=a.uv;return v;}
            float4 Frag(V v):SV_Target
            {
                float2 uv=v.positionCS.xy/_ScaledScreenParams.xy;
                float eye=LinearEyeDepth(v.positionCS.z,_ZBufferParams);
                float opaque=LinearEyeDepth(SAMPLE_TEXTURE2D_X_LOD(_VGDepth,sampler_PointClamp,uv,0).r,_ZBufferParams);
                clip(.025+eye*.0005-abs(opaque-eye));
                return float4(v.uv*_VGIdentity.x,_VGIdentity.y,1);
            }
            ENDHLSL
        }
    }
}
