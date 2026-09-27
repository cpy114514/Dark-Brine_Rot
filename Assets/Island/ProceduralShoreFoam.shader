Shader "DarkBrine/Procedural Shore Foam"
{
    Properties
    {
        _FoamColor ("Foam colour", Color) = (0.94, 0.97, 0.98, 0.98)
        _SeaLevel ("Sea level", Float) = 0
        _Wave1 ("Wave 1", Vector) = (0.82, 0.57, 1.35, 180)
        _Wave1Motion ("Wave 1 motion", Vector) = (9.0, 0.15, 0, 0)
        _Wave2 ("Wave 2", Vector) = (-0.38, 0.93, 0.72, 92)
        _Wave2Motion ("Wave 2 motion", Vector) = (6.2, 0.10, 0, 0)
        _Wave3 ("Wave 3", Vector) = (0.96, -0.29, 0.32, 58)
        _Wave3Motion ("Wave 3 motion", Vector) = (4.4, 0.06, 0, 0)
        _Wave4 ("Wave 4", Vector) = (-0.72, -0.69, 0.16, 35)
        _Wave4Motion ("Wave 4 motion", Vector) = (2.8, 0.03, 0, 0)
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
            Offset -1, -1
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FoamColor;
                float _SeaLevel;
                float4 _Wave1;
                float4 _Wave1Motion;
                float4 _Wave2;
                float4 _Wave2Motion;
                float4 _Wave3;
                float4 _Wave3Motion;
                float4 _Wave4;
                float4 _Wave4Motion;
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

            float WaveHeight(float4 wave, float4 motion, float2 samplePosition, float time)
            {
                float2 direction = normalize(wave.xy);
                float phase = (dot(direction, samplePosition) - motion.x * time) * (TWO_PI / max(wave.w, 0.001));
                return sin(phase) * wave.z;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                float waveHeight = WaveHeight(_Wave1, _Wave1Motion, positionWS.xz, _Time.y)
                                 + WaveHeight(_Wave2, _Wave2Motion, positionWS.xz, _Time.y + 1.9)
                                 + WaveHeight(_Wave3, _Wave3Motion, positionWS.xz, _Time.y + 4.2)
                                 + WaveHeight(_Wave4, _Wave4Motion, positionWS.xz, _Time.y + 2.7);
                float breakerLift = sin(input.uv.x * 9.0 + _Time.y * 3.1) * 0.045
                                  + sin(input.uv.x * 17.0 - _Time.y * 4.4) * 0.022;
                float wetEdge = smoothstep(0.02, 0.20, input.uv.y) * (1.0 - smoothstep(0.62, 0.88, input.uv.y));
                positionWS.y = _SeaLevel + waveHeight + 0.14 + breakerLift * wetEdge;
                output.positionWS = positionWS;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 shore = input.positionWS.xz;
                float broad = Noise(shore * 0.12 + _Time.y * float2(0.024, -0.016));
                float fine = Noise(shore * 0.67 - _Time.y * float2(0.18, 0.13));
                float sections = Noise(shore * 0.075 + _Time.y * float2(0.015, -0.01));
                // Narrow, irregular crests travel from sea to land. The thin
                // remnants behind them fade instead of painting a solid ring.
                float phase = input.uv.y * 18.0 - _Time.y * 2.9 + (broad - 0.5) * 3.5;
                float crest = pow(saturate(sin(phase) * 0.5 + 0.5), 6.0);
                float broken = 0.38 + 0.62 * smoothstep(0.26, 0.66, sections + fine * 0.18);
                float lace = smoothstep(0.42, 0.72, fine) * (0.22 + broad * 0.24);
                float edge = smoothstep(0.08, 0.22, input.uv.y)
                           * (1.0 - smoothstep(0.76, 0.96, input.uv.y));
                float coverage = saturate(crest * broken * 0.95 + lace * broken * 0.36);
                half alpha = edge * coverage * _FoamColor.a;
                half3 foamColor = lerp(_FoamColor.rgb * 0.86, _FoamColor.rgb, saturate(crest));
                return half4(foamColor, alpha);
            }
            ENDHLSL
        }
    }
}
