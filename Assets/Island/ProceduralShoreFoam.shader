Shader "DarkBrine/Procedural Shore Foam"
{
    Properties
    {
        _FoamColor ("Foam colour", Color) = (0.78, 0.96, 1.0, 0.94)
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
                float slowNoise = Noise(input.positionWS.xz * 0.17 + _Time.y * float2(0.10, -0.065));
                float fineNoise = Noise(input.positionWS.xz * 0.74 - _Time.y * float2(0.29, 0.19));
                // Broad cells break a crest into separate sections instead of
                // drawing a uniform white ring around the whole island.
                float breakerSections = Noise(input.positionWS.xz * 0.052 + _Time.y * float2(0.022, -0.014));
                // Constant phase travels toward larger UV.y: from the water-side row
                // onto the island, where depth naturally hides the spent foam.
                float breakingBands = sin(input.uv.y * 21.0 - _Time.y * 4.8 + slowNoise * 6.0) * 0.5 + 0.5;
                float shoreFade = smoothstep(0.025, 0.13, input.uv.y) * (1.0 - smoothstep(0.72, 0.94, input.uv.y));
                // Keep distinct gaps between breakers, but give each crest a
                // readable footprint from the player's beach-level camera.
                float crestTexture = smoothstep(0.12, 0.58, fineNoise + slowNoise * 0.28);
                float crestSections = smoothstep(0.24, 0.58, breakerSections + fineNoise * 0.18);
                float crest = pow(saturate(breakingBands), 2.25) * crestTexture * crestSections;
                float residueSections = smoothstep(0.18, 0.56, breakerSections + slowNoise * 0.25);
                float residue = smoothstep(0.31, 0.68, slowNoise * 0.58 + fineNoise * 0.42) * residueSections * 0.54;
                // A persistent wet-foam base makes the surf legible at beach
                // height; the brighter crest and residue still travel through it.
                float foamCoverage = saturate(0.32 + crest * 0.68 + residue * 0.35);
                half alpha = shoreFade * foamCoverage * _FoamColor.a;
                half3 foamColor = lerp(_FoamColor.rgb * 0.66, _FoamColor.rgb, saturate(crest + residue));
                return half4(foamColor, alpha);
            }
            ENDHLSL
        }
    }
}
