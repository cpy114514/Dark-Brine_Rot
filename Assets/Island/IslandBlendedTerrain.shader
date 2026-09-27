Shader "DarkBrine/Island Blended Terrain"
{
    Properties
    {
        _SandTex ("Coastal sand", 2D) = "white" {}
        _SandAltTex ("Alternate coastal sand", 2D) = "white" {}
        _DryTex ("Dry ground", 2D) = "white" {}
        _GrassTex ("Sparse grass", 2D) = "white" {}
        _LeafyGrassTex ("Leafy grass", 2D) = "white" {}
        _ForestTex ("Forest soil", 2D) = "white" {}
        _RockTex ("Rock", 2D) = "white" {}
        [Normal] _SandNormal ("Sand normal", 2D) = "bump" {}
        [Normal] _GrassNormal ("Grass normal", 2D) = "bump" {}
        [Normal] _RockNormal ("Rock normal", 2D) = "bump" {}
        _SandTint ("Sand tint", Color) = (1, 1, 1, 1)
        _GrassTint ("Grass tint", Color) = (1, 1, 1, 1)
        _RockTint ("Rock tint", Color) = (1, 1, 1, 1)
        _SeaLevel ("Sea level", Float) = 0
        [HideInInspector] _BaseMap ("Shadow base map", 2D) = "white" {}
        [HideInInspector] _BaseColor ("Shadow base color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Cutoff ("Shadow cutoff", Float) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On

            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SandTint;
                half4 _GrassTint;
                half4 _RockTint;
                float _SeaLevel;
            CBUFFER_END

            TEXTURE2D(_SandTex); SAMPLER(sampler_SandTex);
            TEXTURE2D(_SandAltTex); SAMPLER(sampler_SandAltTex);
            TEXTURE2D(_DryTex); SAMPLER(sampler_DryTex);
            TEXTURE2D(_GrassTex); SAMPLER(sampler_GrassTex);
            TEXTURE2D(_LeafyGrassTex); SAMPLER(sampler_LeafyGrassTex);
            TEXTURE2D(_ForestTex); SAMPLER(sampler_ForestTex);
            TEXTURE2D(_RockTex); SAMPLER(sampler_RockTex);
            TEXTURE2D(_SandNormal); SAMPLER(sampler_SandNormal);
            TEXTURE2D(_GrassNormal); SAMPLER(sampler_GrassNormal);
            TEXTURE2D(_RockNormal); SAMPLER(sampler_RockNormal);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
                float2 biome : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
                float radius : TEXCOORD3;
                half fogFactor : TEXCOORD4;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.radius = input.biome.x;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            float Hash21(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453);
            }

            float PatchNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 blend = frac(p);
                blend = blend * blend * (3.0 - 2.0 * blend);
                return lerp(
                    lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), blend.x),
                    lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + 1.0), blend.x),
                    blend.y);
            }

            half4 frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                // Broad, deterministic erosion bends the biome boundaries instead
                // of leaving the island with concentric painted rings.
                float macro = 0.48 * sin(p.x * 0.013 + p.y * 0.007)
                    + 0.33 * sin(p.y * 0.019 - p.x * 0.004 + 1.7)
                    + 0.19 * sin((p.x + p.y) * 0.038 - 0.8);
                float broadPatch = PatchNoise(p * 0.012);
                float detailPatch = PatchNoise(p * 0.055 + 19.4);
                float zone = saturate(input.radius + macro * 0.035 + (broadPatch - 0.5) * 0.075);

                half3 rock = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, input.uv).rgb * _RockTint.rgb;
                half3 dry = SAMPLE_TEXTURE2D(_DryTex, sampler_DryTex, input.uv).rgb * _SandTint.rgb;
                half3 forest = SAMPLE_TEXTURE2D(_ForestTex, sampler_ForestTex, input.uv).rgb * _GrassTint.rgb;
                half3 sparse = SAMPLE_TEXTURE2D(_GrassTex, sampler_GrassTex, input.uv).rgb * _GrassTint.rgb;
                half3 leafy = SAMPLE_TEXTURE2D(_LeafyGrassTex, sampler_LeafyGrassTex, input.uv).rgb * _GrassTint.rgb;
                half3 coast = SAMPLE_TEXTURE2D(_SandTex, sampler_SandTex, input.uv).rgb * _SandTint.rgb;
                half3 paleSand = SAMPLE_TEXTURE2D(_SandAltTex, sampler_SandAltTex, input.uv).rgb * _SandTint.rgb;
                half grassPatch = smoothstep(0.33, 0.68, broadPatch * 0.55 + detailPatch * 0.45);
                half sandPatch = smoothstep(0.30, 0.72, PatchNoise(p * 0.041 + 47.1));
                half3 grass = lerp(sparse, leafy, grassPatch * 0.70h);
                half3 sand = lerp(coast, paleSand, sandPatch * 0.55h);

                half forestBlend = smoothstep(0.26, 0.43, zone);
                half grassBlend = smoothstep(0.40, 0.62, zone);
                float shoreHeight = input.positionWS.y - _SeaLevel + macro * 0.75;
                half shoreBlend = 1.0h - smoothstep(1.5, 7.0, shoreHeight);
                half3 albedo = lerp(rock, dry, smoothstep(0.17, 0.36, zone));
                albedo = lerp(albedo, forest, forestBlend);
                albedo = lerp(albedo, grass, grassBlend);
                albedo = lerp(albedo, sand, shoreBlend);

                half3 surfaceNormal = normalize(input.normalWS);
                half slopeRock = (1.0h - smoothstep(0.68h, 0.92h, surfaceNormal.y)) * 0.70h;
                half exposedSoil = smoothstep(0.60, 0.79, detailPatch) * 0.22h;
                albedo = lerp(albedo, dry, exposedSoil * (1.0h - shoreBlend) * (1.0h - slopeRock));
                albedo = lerp(albedo, rock, slopeRock);
                half wetSand = (1.0h - smoothstep(0.3h, 2.8h, shoreHeight)) * (1.0h - slopeRock);
                albedo *= lerp(1.0h, 0.78h, wetSand);
                albedo *= 0.96h + 0.04h * macro;

                half3 rockNormal = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal, sampler_RockNormal, input.uv));
                half3 grassNormal = UnpackNormal(SAMPLE_TEXTURE2D(_GrassNormal, sampler_GrassNormal, input.uv));
                half3 sandNormal = UnpackNormal(SAMPLE_TEXTURE2D(_SandNormal, sampler_SandNormal, input.uv));
                half3 detailNormal = normalize(lerp(rockNormal, grassNormal, forestBlend));
                detailNormal = normalize(lerp(detailNormal, sandNormal, shoreBlend));
                detailNormal = normalize(lerp(detailNormal, rockNormal, slopeRock));
                detailNormal.xy *= 0.55h;
                detailNormal = normalize(detailNormal);

                half3 tangentWS = normalize(half3(1.0h, -surfaceNormal.x / max(surfaceNormal.y, 0.1h), 0.0h));
                half3 bitangentWS = normalize(cross(tangentWS, surfaceNormal));
                half3 normalWS = normalize(detailNormal.x * tangentWS + detailNormal.y * bitangentWS + detailNormal.z * surfaceNormal);

                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS));
                half NdotL = saturate(dot(normalWS, sun.direction));
                half3 ambient = max(SampleSH(normalWS), half3(0.19h, 0.20h, 0.20h));
                half3 direct = sun.color * NdotL * sun.distanceAttenuation * sun.shadowAttenuation * 0.57h;
                half3 lit = albedo * (ambient + direct);
                return half4(MixFog(lit, input.fogFactor), 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
    }
    FallBack Off
}
