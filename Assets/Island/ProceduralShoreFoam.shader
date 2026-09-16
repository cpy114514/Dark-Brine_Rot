Shader "DarkBrine/Procedural Shore Foam"
{
    Properties
    {
        _FoamColor ("Foam colour", Color) = (0.82, 0.98, 1.0, 0.82)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-90" "RenderType"="Transparent" }
        Pass
        {
            Name "ShoreBreakers"
            Tags { "LightMode"="UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FoamColor;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 345.45));
                p += dot(p, p + 34.345);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);
                return lerp(lerp(Hash21(cell), Hash21(cell + float2(1, 0)), local.x),
                            lerp(Hash21(cell + float2(0, 1)), Hash21(cell + float2(1, 1)), local.x), local.y);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float surge = sin(input.uv.x * 8.0 + _Time.y * 3.1) * 0.042
                            + sin(input.uv.x * 15.0 - _Time.y * 4.4) * 0.022;
                positionWS.y += surge * (0.35 + 0.65 * sin(input.uv.y * PI));
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float slowNoise = Noise(input.positionWS.xz * 0.19 + _Time.y * float2(0.11, -0.070));
                float fineNoise = Noise(input.positionWS.xz * 0.72 - _Time.y * float2(0.24, 0.17));
                float breakingBands = sin(input.uv.y * 16.0 - _Time.y * 4.2 + slowNoise * 5.0) * 0.5 + 0.5;
                float shoreFade = smoothstep(0.02, 0.30, input.uv.y) * (1.0 - smoothstep(0.64, 1.0, input.uv.y));
                float broken = smoothstep(0.28, 0.76, slowNoise * 0.62 + fineNoise * 0.38 + breakingBands * 0.30);
                half alpha = shoreFade * broken * _FoamColor.a;
                return half4(_FoamColor.rgb, alpha);
            }
            ENDHLSL
        }
    }
}
