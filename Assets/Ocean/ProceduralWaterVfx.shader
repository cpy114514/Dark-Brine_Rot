Shader "DarkBrine/Procedural Water VFX"
{
    Properties
    {
        _Tint ("Tint", Color) = (0.92, 0.95, 0.96, 1.0)
        _Softness ("Soft billboard edge", Range(0, 1)) = 0
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Tint;
                half _Softness;
            CBUFFER_END

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
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                half fogFactor : TEXCOORD3;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color * _Tint;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half opacity = input.color.a;
                if (_Softness > 0.001)
                {
                    float2 p = input.uv * 2.0 - 1.0;
                    float edge = length(p) + sin(p.x * 11.0 + p.y * 7.0) * 0.035;
                    opacity *= lerp(1.0, 1.0 - smoothstep(0.10, 0.96, edge), _Softness);
                }
                float2 screenUv = GetNormalizedScreenSpaceUV(input.positionCS);
                float sceneDepth = LinearEyeDepth(SampleSceneDepth(screenUv), _ZBufferParams);
                float particleDepth = -TransformWorldToView(input.positionWS).z;
                opacity *= saturate((sceneDepth - particleDepth) / 0.18);
                half3 normal = SafeNormalize(input.normalWS);
                Light sun = GetMainLight();
                half3 ambient = max(SampleSH(normal), half3(0.025, 0.03, 0.035));
                half3 illumination = min(ambient + sun.color * (0.30h + 0.45h * abs(dot(normal, sun.direction))), 1.2h);
                return half4(MixFog(input.color.rgb * illumination, input.fogFactor), opacity);
            }
            ENDHLSL
        }
    }
}
