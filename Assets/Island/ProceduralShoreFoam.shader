Shader "DarkBrine/Procedural Shore Foam"
{
    Properties
    {
        _FoamColor ("Foam colour", Color) = (0.94, 0.97, 0.98, 0.98)
        _ShoreBandWidth ("Shoreline band width (m)", Range(0.25, 4)) = 1.8
        _ShoreDensity ("Shoreline foam density", Range(0, 1)) = 0.96
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
            // Respect the island's hardware depth so surf behind dry terrain
            // cannot show through, even when the camera depth copy is stale.
            ZTest LEqual
            // Bias only the depth comparison to keep foam above the displaced
            // ocean where the two meshes have different tessellation.
            Offset -1, -1
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
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
                float _ShoreBandWidth;
                float _ShoreDensity;
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
                float4 shoreData : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                half3 normalWS : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float4 screenPosition : TEXCOORD4;
                float3 breaker : TEXCOORD5;
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
                float crest, primaryPhase;
                float distanceXZ = distance(positionWS.xz, _WorldSpaceCameraPos.xz);
                OceanEvaluateWaves(positionWS.xz, oceanTime, OceanDetailWeights(distanceXZ),
                    displacement, waveNormal, crest, primaryPhase);
                // A crest arrives, rushes up the beach, spreads, then drains
                // more slowly. Carry the unwrapped real swell phase across
                // triangles; wrapping it per vertex would tear the breaker.
                float waterY = _SeaLevel + displacement.y;
                positionWS.xz += displacement.xz;
                // Follow the actual water all the way to sand. Lifting a
                // ribbon onto dry terrain leaves a detached white beach band
                // while the incoming sea is in its trough.
                // The ocean patch has coarser triangles than the shore ribbon.
                // A small surface bias keeps its lace above that rasterized
                // surface without raising it onto dry sand.
                positionWS.y = waterY + 0.14;
                output.breaker = float3(primaryPhase, waterY - input.shoreData.x, crest);
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
                float broad = Noise(shore * 0.11 + oceanTime * float2(0.021, -0.014));
                float fine = Noise(shore * 0.85 - oceanTime * float2(0.11, 0.08));
                float grain = Noise(shore * 2.3 + fine * 0.6);
                // Measure the contact in horizontal world metres. Eye-depth alone
                // produces a broad white carpet on a gently sloping beach.
                float depthGap = max(0.0, input.breaker.y);
                float2 dx = ddx(shore), dy = ddy(shore);
                float2 depthGradient = float2(ddx(depthGap) * dy.y - ddy(depthGap) * dx.y,
                    ddy(depthGap) * dx.x - ddx(depthGap) * dy.x);
                float determinant = abs(dx.x * dy.y - dx.y * dy.x);
                float contactDistance = depthGap * determinant / max(length(depthGradient), 0.00001);
                // A flat/deep background has no shoreline gradient and must not
                // turn into foam. The original shallow-water gate remains.
                contactDistance = max(contactDistance, depthGap * 0.35);
                float cycle = frac((-input.breaker.x + PI * 0.5) / TWO_PI);
                float surge = smoothstep(0.04, 0.28, cycle)
                    * (1.0 - smoothstep(0.46, 0.98, cycle));
                float width = _ShoreBandWidth * lerp(0.65, 1.1, surge) * (0.94 + broad * 0.12);
                float aa = min(fwidth(contactDistance), 0.35);
                float ribbon = 1.0 - smoothstep(width * 0.45, width + aa, contactDistance);
                float lace = 1.0 - smoothstep(0.05, 0.22, abs(fine - 0.51));
                float bubbles = smoothstep(0.18, 0.72, grain);
                // Keep a solid, dense core all along the waterline. Noise varies
                // bubbles and the thin outer lace, never punches holes in the line.
                float fresh = ribbon * (0.86 + bubbles * 0.14);
                float wash = (1.0 - smoothstep(width, width + 0.65 + aa, contactDistance))
                    * (1.0 - ribbon) * lace * 0.22;
                float wet = smoothstep(-0.03, 0.06, input.breaker.y)
                    * (1.0 - smoothstep(0.6, 1.8, input.breaker.y));
                float edge = smoothstep(0.0, 0.035, input.uv.y)
                    * (1.0 - smoothstep(0.97, 1.0, input.uv.y));
                half alpha = edge * wet * saturate(fresh + wash) * _ShoreDensity * _FoamColor.a;
                half3 foamColor = _FoamColor.rgb * lerp(0.93, 1.0, saturate(fresh));
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
