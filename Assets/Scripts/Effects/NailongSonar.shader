Shader "DarkBrine/Sonar Ring"
{
    Properties
    {
        _Tint("Pressure rim tint",Color)=(0.88,0.94,1,1)
        _Opacity("Pulse visibility",Range(0,1))=1
        _DistortionPixels("Refraction in pixels",Range(0,8))=6
        _RimOpacity("Pressure rim opacity",Range(0,.35))=.26
        _BodyOpacity("Transparent body visibility",Range(0,.12))=.032
        _UseRefraction("Scene colour available",Float)=1
        _UseDepth("Scene depth available",Float)=1
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
            Cull Back
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Opacity, _DistortionPixels, _RimOpacity, _BodyOpacity, _UseRefraction, _UseDepth;
            CBUFFER_END
            struct A {float4 position:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;};
            struct V {float4 position:SV_POSITION;float2 uv:TEXCOORD0;half fog:TEXCOORD1;float3 world:TEXCOORD2;half3 normal:TEXCOORD3;};
            V vert(A input)
            {
                V o;o.world=TransformObjectToWorld(input.position.xyz);o.position=TransformWorldToHClip(o.world);
                o.uv=input.uv;o.fog=ComputeFogFactor(o.position.z);
                o.normal=TransformObjectToWorldNormal(input.normal);return o;
            }
            half4 frag(V i):SV_Target
            {
                float2 screen=i.position.xy/_ScaledScreenParams.xy;
                half3 normal=normalize(i.normal);
                half facing=saturate(dot(normal,GetWorldSpaceNormalizeViewDir(i.world)));
                half rimProfile=pow(1-facing,3);
                // Periodic around the closed tube: no alpha seam on its inner or outer wall.
                half crest=.04+.96*rimProfile;
                half trough=(1-facing)*(.35+.35*saturate(-normal.y));
                half soft=1;
                if(_UseDepth>.5)
                {
                    float scene=LinearEyeDepth(SampleSceneDepth(screen),_ZBufferParams);
                    soft=saturate((scene+TransformWorldToView(i.world).z)/.14);
                }
                half fade=_Opacity*soft;
                half rim=crest*_RimOpacity*fade;
                half body=_BodyOpacity*fade;
                half dark=trough*.075*fade;
                half3 tint=MixFog(lerp(_Tint.rgb,half3(.94,.97,1),.9),i.fog);
                if(_UseRefraction<.5) return half4(tint*(1-trough*.35),saturate(rim+body+dark));
                float turbulence=.82+.18*sin(i.uv.x*62.83+_Time.y*3);
                float2 direction=mul((float3x3)UNITY_MATRIX_V,normal).xy;
                float2 offset=direction*turbulence*_DistortionPixels*fade/_ScaledScreenParams.xy;
                // Never pull a foreground silhouette into the pressure wave.
                if(_UseDepth>.5)
                {
                    float shifted=LinearEyeDepth(SampleSceneDepth(saturate(screen+offset)),_ZBufferParams);
                    offset*=step(-TransformWorldToView(i.world).z,shifted+.02);
                }
                half3 refracted=SampleSceneColor(saturate(screen+offset));
                // A paired light/dark front remains readable on grass and bright ground.
                half alpha=saturate(fade*.65+rim+body+dark);
                half3 colour=refracted*(1-(rim+body+dark)/max(alpha,.001))+tint*(rim+body)/max(alpha,.001);
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
