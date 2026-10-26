Shader "DarkBrine/Combat Pressure"
{
    Properties
    {
        _Tint("Air tint",Color)=(.88,.94,1,1)
        _Fade("Visibility",Range(0,1))=1
        _DistortionPixels("Refraction pixels",Range(0,4))=2.6
        _RimOpacity("Thin pressure rim",Range(0,.3))=.24
        _BodyOpacity("Air body",Range(0,.08))=.016
        _Ribbon("Weapon ribbon",Float)=0
        _UseRefraction("Scene colour available",Float)=0
        _UseDepth("Scene depth available",Float)=0
        [Enum(UnityEngine.Rendering.CullMode)] _Cull("Cull",Float)=2
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+10"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull [_Cull]
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Fade,_DistortionPixels,_RimOpacity,_BodyOpacity,_Ribbon,_UseRefraction,_UseDepth;
            CBUFFER_END
            struct A {float4 position:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;half4 colour:COLOR;};
            struct V {float4 position:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half4 colour:COLOR;half fog:TEXCOORD3;};
            V vert(A input)
            {
                V o;o.world=TransformObjectToWorld(input.position.xyz);o.position=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(input.normal);o.uv=input.uv;o.colour=input.colour;
                o.fog=ComputeFogFactor(o.position.z);return o;
            }
            half4 frag(V i):SV_Target
            {
                float2 screen=i.position.xy/_ScaledScreenParams.xy;
                half3 normal=normalize(i.normal);
                half facing=abs(dot(normal,GetWorldSpaceNormalizeViewDir(i.world)));
                half rim=pow(1-saturate(facing),2.5);
                half shape=lerp(1,sin(saturate(i.uv.y)*3.14159265),_Ribbon);
                half fade=_Fade*i.colour.a*shape;
                if(_UseDepth>.5)
                {
                    float scene=LinearEyeDepth(SampleSceneDepth(screen),_ZBufferParams);
                    fade*=saturate((scene+TransformWorldToView(i.world).z)/.06);
                }
                half crest=(.12+.88*rim)*_RimOpacity*fade;
                half body=_BodyOpacity*fade;
                half trough=(1-facing)*.04*fade;
                half3 tint=MixFog(_Tint.rgb*i.colour.rgb,i.fog);
                half alpha=saturate(crest+body+trough);
                if(_UseRefraction<.5)return half4(tint*(crest+body),alpha);
                float2 distortion=mul((float3x3)UNITY_MATRIX_V,normal).xy;
                distortion*=(_DistortionPixels*fade*(.9+.1*sin(i.uv.x*50+_Time.y*4)))/_ScaledScreenParams.xy;
                if(_UseDepth>.5)
                {
                    float shifted=LinearEyeDepth(SampleSceneDepth(saturate(screen+distortion)),_ZBufferParams);
                    distortion*=step(-TransformWorldToView(i.world).z,shifted+.015);
                }
                half3 refracted=SampleSceneColor(saturate(screen+distortion));
                // Apply only the displaced colour difference. Copying the opaque
                // background itself would punch a sky/terrain patch into ocean
                // water and other transparent objects drawn before this wave.
                half3 original=SampleSceneColor(screen);
                half3 delta=clamp(refracted-original,-.18,.18)*fade*.4;
                half3 colour=tint*(crest+body)+delta;
                return half4(colour,alpha);
            }
            ENDHLSL
        }
    }
}
