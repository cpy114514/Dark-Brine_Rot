Shader "DarkBrine/Procedural Shore Foam"
{
    Properties
    {
        _FoamColor ("Foam colour", Color) = (0.94, 0.97, 0.98, 0.98)
        _SeaLevel ("Sea level", Float) = 0
        _OceanMotionSpeed ("Ocean animation speed", Range(0.25, 4)) = 1.8
        _WaveIrregularity ("Wave irregularity", Range(0, 1)) = 0.7
        _NearDetailDistance ("Near detail distance", Float) = 180
        _MidDetailDistance ("Mid detail distance", Float) = 700
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
            // The water and surf ribbon have different tessellation. Let the
            // opaque depth texture hide terrain, but do not let the water mesh
            // erase its own foam where their interpolated wave heights cross.
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            // This pass is drawn after the opaque depth texture. Screen-space shadows
            // describe the terrain behind the foam, so sample the shadow atlas at the
            // displaced water surface instead, including when that renderer feature is on.
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _FoamColor;
                float _SeaLevel;
                float _OceanMotionSpeed;
                float _WaveIrregularity;
                float _NearDetailDistance;
                float _MidDetailDistance;
                float4 _Wave1;
                float4 _Wave1Motion;
                float4 _Wave2;
                float4 _Wave2Motion;
                float4 _Wave3;
                float4 _Wave3Motion;
                float4 _Wave4;
                float4 _Wave4Motion;
            CBUFFER_END

            #include "Assets/Ocean/OceanWaves.hlsl"

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
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float4 screenPosition : TEXCOORD4;
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
                float oceanTime = _Time.y * _OceanMotionSpeed;
                float3 displacement, waveNormal;
                float crest;
                float distanceXZ = distance(positionWS.xz, _WorldSpaceCameraPos.xz);
                OceanEvaluateWaves(positionWS.xz, oceanTime, OceanDetailWeights(distanceXZ),
                    displacement, waveNormal, crest);
                float breakerLift = (Noise(positionWS.xz * 0.16
                    + oceanTime * float2(0.055, -0.037)) - 0.5) * 0.085;
                float wetEdge = smoothstep(0.02, 0.20, input.uv.y) * (1.0 - smoothstep(0.62, 0.88, input.uv.y));
                positionWS.xz += displacement.xz;
                positionWS.y = _SeaLevel + displacement.y + 0.14 + breakerLift * wetEdge;
                output.positionWS = positionWS;
                output.normalWS = waveNormal;
                output.positionCS = TransformWorldToHClip(positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                output.uv = input.uv;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                float opaqueEyeDepth = LinearEyeDepth(SampleSceneDepth(screenUv), _ZBufferParams);
                float foamEyeDepth = -TransformWorldToView(input.positionWS).z;
                clip(opaqueEyeDepth - foamEyeDepth + 0.025);
                float2 shore = input.positionWS.xz;
                float oceanTime = _Time.y * _OceanMotionSpeed;
                float broad = Noise(shore * 0.050 + oceanTime * float2(0.012, -0.008));
                float fine = Noise(shore * 0.24 - oceanTime * float2(0.090, 0.065));
                float sections = Noise(shore * 0.016 + oceanTime * float2(0.004, -0.003));
                // Breakers advance in uneven groups, leaving brighter lace
                // at the waterline instead of a faint, periodic white ring.
                float phase = input.uv.y * 11.5 - oceanTime * 2.15
                    + (broad - 0.5) * 4.5 + (sections - 0.5) * 3.0
                    + (fine - 0.5) * 0.6;
                float crest = smoothstep(0.54, 0.88, sin(phase) * 0.5 + 0.5);
                float broken = 0.23 + 0.77 * smoothstep(0.32, 0.67,
                    sections * 0.65 + broad * 0.35);
                float lace = smoothstep(0.39, 0.68, fine) * (0.24 + broad * 0.25);
                float backwash = smoothstep(0.61, 0.84, input.uv.y)
                    * (1.0 - smoothstep(0.94, 1.0, input.uv.y))
                    * (0.30 + 0.70 * smoothstep(0.32, 0.70, broad + fine * 0.15)) * 0.52;
                float edge = smoothstep(0.04, 0.16, input.uv.y)
                           * (1.0 - smoothstep(0.91, 1.0, input.uv.y));
                float coverage = saturate(crest * broken * 1.05 + lace * broken * 0.38 + backwash);
                half alpha = edge * coverage * _FoamColor.a;
                half3 foamColor = lerp(_FoamColor.rgb * 0.86, _FoamColor.rgb, saturate(crest));
                // Preserve the existing white-water tint and lace, but treat them as
                // albedo rather than emission. Daylight stays white; moonlight and
                // shore shadows now dim and colour the same foam naturally.
                half3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight(TransformWorldToShadowCoord(input.positionWS),
                    input.positionWS, half4(1.0h, 1.0h, 1.0h, 1.0h));
                half3 ambient = max(SampleSH(normalWS), 0.0h);
                half diffuse = saturate(dot(normalWS, mainLight.direction)) *
                    mainLight.distanceAttenuation * mainLight.shadowAttenuation;
                half3 illumination = saturate(ambient + mainLight.color * diffuse);
                foamColor *= illumination;
                foamColor = MixFog(foamColor, input.fogFactor);
                return half4(foamColor, alpha);
            }
            ENDHLSL
        }
    }
}
