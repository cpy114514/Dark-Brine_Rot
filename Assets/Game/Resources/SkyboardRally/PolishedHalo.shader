Shader "DarkBrine/Skyboard Polished Halo"
{
    Properties
    {
        _Color ("Metal tint", Color) = (1, 0.78, 0.34, 1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half3 normal = normalize(input.normalWS);
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half3 reflection = reflect(-view, normal);
                // Bright and dark bands behave like reflected softboxes even in the dark circuit.
                half key = smoothstep(.68, .96, reflection.y * .8 + abs(reflection.z) * .6);
                half fill = smoothstep(.62, .94, dot(reflection, half3(-.8, -.6, 0)));
                half strip = pow(saturate(1 - abs(dot(reflection, half3(.82, .57, 0)) - .48)), 22);
                half rim = pow(1 - saturate(dot(normal, view)), 4);
                half3 metal = _Color.rgb * (.27 + .22 * (reflection.y * .5 + .5));
                metal += lerp(_Color.rgb, half3(1, 1, 1), .6) * (key * .85 + strip * .52);
                metal += half3(.54, .72, .96) * (fill * .34 + rim * .18);
                return half4(metal, 1);
            }
            ENDHLSL
        }
    }
}
