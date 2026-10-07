Shader "DarkBrine/Procedural Water VFX"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.92, 0.95, 0.96, 1.0)
        _Softness ("Soft billboard edge", Range(0, 1)) = 0
        _MaskTex ("Particle density", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "WaterVfx"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #pragma shader_feature_local _PARTICLE_MASK
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Softness;
            CBUFFER_END
            TEXTURE2D(_MaskTex);
            SAMPLER(sampler_MaskTex);

            struct Attributes
            {
                float4 positionOS : POSITION;
                half4 color : COLOR;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                half4 color : COLOR;
                float particleDepth : TEXCOORD0;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Tint;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.particleDepth = -TransformWorldToView(positionWS).z;
                half3 normal = SafeNormalize(TransformObjectToWorldNormal(input.normalOS));
                Light sun = GetMainLight();
                half3 ambient = max(SampleSH(normal), half3(0.025, 0.03, 0.035));
                half3 illumination = min(ambient + sun.color * (0.30h + 0.45h * abs(dot(normal, sun.direction))), 1.2h);
                output.color.rgb *= illumination;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half opacity = input.color.a;
                #if defined(_PARTICLE_MASK)
                    half4 mask = SAMPLE_TEXTURE2D(_MaskTex, sampler_MaskTex, input.uv);
                    opacity *= mask.r * mask.a;
                #else
                if (_Softness > 0.001)
                {
                    float2 p = input.uv * 2.0 - 1.0;
                    float edge = dot(p, p);
                    opacity *= lerp(1.0, 1.0 - smoothstep(0.01, 0.92, edge), _Softness);
                }
                #endif
                float2 screenUv = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUv), _ZBufferParams);
                opacity *= saturate((sceneDepth - input.particleDepth) / 0.18);
                return half4(MixFog(input.color.rgb, input.fogFactor), opacity);
            }
            ENDHLSL
        }
    }
}
