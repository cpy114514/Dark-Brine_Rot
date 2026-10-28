Shader "Mavis/MeadowProp"
{
    Properties
    {
        _Color ("Tint", Color) = (1, 1, 1, 1)
        _Wind ("Wind", Range(0, 1)) = 0.35
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Back
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                half _Wind;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float4 color : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                float height = saturate(IN.positionOS.y);
                float sway = sin(_Time.y * 1.35 + positionWS.x * 0.37 + positionWS.z * 0.21);
                positionWS.xz += sway * height * _Wind * 0.12;
                OUT.positionWS = positionWS;
                OUT.positionCS = TransformWorldToHClip(positionWS);
                OUT.normalWS = TransformObjectToWorldNormal(IN.normalOS);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 tint = IN.color.a > 0 ? IN.color : half4(1, 1, 1, 1);
                half3 albedo = tint.rgb * _Color.rgb;
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(IN.positionWS));
                half3 normalWS = normalize(IN.normalWS);
                half ndotl = saturate(dot(normalWS, mainLight.direction));
                half3 sky = half3(0.55, 0.62, 0.70);
                half3 ground = half3(0.20, 0.18, 0.14);
                half3 ambient = lerp(ground, sky, normalWS.y * 0.5 + 0.5);
                half3 lit = albedo * (ambient * 0.85 + mainLight.color * (0.22 + ndotl * 0.78) * mainLight.shadowAttenuation);
                return half4(lit, 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
