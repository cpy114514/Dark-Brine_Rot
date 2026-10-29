Shader "DarkBrine/Candy Fluff"
{
    Properties { _Softness ("Soft edge", Range(.1,1)) = .7 }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            TEXTURE2D_X_FLOAT(_CameraDepthTexture); SAMPLER(sampler_CameraDepthTexture);
            struct A { float4 positionOS:POSITION; float4 color:COLOR; float2 uv:TEXCOORD0; };
            struct V { float4 positionCS:SV_POSITION; half4 color:COLOR; float2 uv:TEXCOORD0; float4 screen:TEXCOORD1; float eye:TEXCOORD2; };
            V vert(A i)
            {
                V o; float3 world = TransformObjectToWorld(i.positionOS.xyz);
                o.positionCS = TransformWorldToHClip(world); o.color=i.color; o.uv=i.uv;
                o.screen=ComputeScreenPos(o.positionCS);o.eye=-TransformWorldToView(world).z;
                return o;
            }
            half4 frag(V i):SV_Target
            {
                float2 p=i.uv*2-1;
                float r=length(p);
                float tuft=.8+.08*sin(atan2(p.y,p.x)*5+r*5);
                float alpha=pow(saturate(1-r/tuft),1.6)*2.2;
                float raw=SAMPLE_TEXTURE2D_X(_CameraDepthTexture,sampler_CameraDepthTexture,i.screen.xy/i.screen.w).r;
                float depth=LinearEyeDepth(raw,_ZBufferParams);
                alpha*=saturate((depth-i.eye)*2);
                return half4(i.color.rgb, i.color.a*alpha);
            }
            ENDHLSL
        }
    }
}
