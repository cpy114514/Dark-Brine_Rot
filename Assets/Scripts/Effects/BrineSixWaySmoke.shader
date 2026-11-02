Shader "DarkBrine/Brine Six Way Smoke"
{
    Properties
    {
        _Positive("Right Top Back + density",2D)="black"{}
        _Negative("Left Bottom Front",2D)="black"{}
        _Age("Normalized simulation time",Float)=0
        _Fade("Opacity",Float)=1
        _DepthAvailable("Depth available",Float)=0
        _Tint("Smoke albedo",Color)=(.27,.25,.22,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+17" "RenderType"="Transparent"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Blend One OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            TEXTURE2D(_Positive);SAMPLER(sampler_Positive);
            TEXTURE2D(_Negative);SAMPLER(sampler_Negative);
            CBUFFER_START(UnityPerMaterial)
                float _Age,_Fade,_DepthAvailable;half4 _Tint;
            CBUFFER_END
            struct A{float4 position:POSITION;float2 uv:TEXCOORD0;};
            struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;float2 uv:TEXCOORD1;float fog:TEXCOORD2;};
            V Vert(A i){V o;o.world=TransformObjectToWorld(i.position.xyz);o.position=TransformWorldToHClip(o.world);o.uv=i.uv;o.fog=ComputeFogFactor(o.position.z);return o;}
            float2 Atlas(float2 uv,float frame)
            {
                float2 cell=float2(fmod(frame,8),7-floor(frame/8));
                // Inset prevents filtering across neighbouring simulation frames.
                return (cell+clamp(uv,.008,.992))/8;
            }
            half4 Frag(V i):SV_Target
            {
                float f=saturate(_Age)*63,index=floor(f),next=min(63,index+1),blend=frac(f);
                float2 uv0=Atlas(i.uv,index),uv1=Atlas(i.uv,next);
                half4 p=lerp(SAMPLE_TEXTURE2D(_Positive,sampler_Positive,uv0),SAMPLE_TEXTURE2D(_Positive,sampler_Positive,uv1),blend);
                half3 n=lerp(SAMPLE_TEXTURE2D(_Negative,sampler_Negative,uv0).rgb,SAMPLE_TEXTURE2D(_Negative,sampler_Negative,uv1).rgb,blend);
                Light light=GetMainLight();float3 l=normalize(TransformWorldToObjectDir(light.direction));
                l.z=-l.z;float3 positive=max(l,0),negative=max(-l,0);
                // RGB map axes are right/top/back, not RGB colour.
                half response=dot(p.rgb,positive*positive)+dot(n,negative*negative);
                half ambient=(dot(p.rgb,half3(1,1,1))+dot(n,half3(1,1,1)))/6;
                half3 irradiance=light.color*response+max(SampleSH(float3(0,1,0)),half3(.12,.12,.12))*ambient*2;
                half alpha=p.a*_Fade*smoothstep(0,.035,_Age)*(1-smoothstep(.78,1,_Age));
                if(_DepthAvailable>.5)
                {
                    float scene=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.position)),_ZBufferParams);
                    alpha*=saturate((scene+TransformWorldToView(i.world).z)/.35);
                }
                half3 color=_Tint.rgb*(.18+irradiance*1.8);
                return half4(MixFog(color,i.fog)*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
