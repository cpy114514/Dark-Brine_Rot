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
        _BeachTex ("Yellow beach sand", 2D) = "white" {}
        [Normal] _BeachNormal ("Beach sand normal", 2D) = "bump" {}
        _BeachTint ("Beach tint", Color) = (1.18, 1.08, 0.72, 1)
        _BeachHeight ("Beach height above sea", Float) = 8
        _BeachBlendWidth ("Beach transition height", Float) = 3.5
        _BeachTiling ("Beach world tiling", Float) = 0.075
        [HideInInspector] _BeachEnabled ("Beach enabled", Float) = 0
        _FineSandTex ("Fine sand patches", 2D) = "white" {}
        _WetSandTex ("Wet shore patches", 2D) = "white" {}
        _MeadowTex ("Meadow patches", 2D) = "white" {}
        _DryGrassTex ("Dry grass patches", 2D) = "white" {}
        [Normal] _FineSandNormal ("Fine sand normal", 2D) = "bump" {}
        [Normal] _WetSandNormal ("Wet sand normal", 2D) = "bump" {}
        [Normal] _MeadowNormal ("Meadow normal", 2D) = "bump" {}
        [Normal] _DryGrassNormal ("Dry grass normal", 2D) = "bump" {}
        _VariationAmount ("Surface variation", Range(0,1)) = 0.85
        _PatchScale ("Patch scale", Float) = 0.045
        [HideInInspector] _VariationEnabled ("Variation enabled", Float) = 0
        [HideInInspector] _HabitatMap ("Baked habitat", 2D) = "black" {}
        [HideInInspector] _HabitatBounds ("Habitat world bounds", Vector) = (0, 0, 1, 1)
        [HideInInspector] _HabitatEnabled ("Habitat enabled", Float) = 0
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
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            // URP selects one main-light shadow mode and strips unused modes in Player builds.
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _SandTint;
                half4 _GrassTint;
                half4 _RockTint;
                float _SeaLevel;
                half4 _BeachTint;
                float _BeachHeight;
                float _BeachBlendWidth;
                float _BeachTiling;
                half _BeachEnabled;
                half _VariationAmount;
                half _VariationEnabled;
                float _PatchScale;
                float4 _HabitatBounds;
                half _HabitatEnabled;
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
            TEXTURE2D(_BeachTex); SAMPLER(sampler_BeachTex);
            TEXTURE2D(_BeachNormal); SAMPLER(sampler_BeachNormal);
            // All new maps share existing repeating samplers to stay below DX11's sampler limit.
            TEXTURE2D(_FineSandTex); TEXTURE2D(_WetSandTex);
            TEXTURE2D(_MeadowTex); TEXTURE2D(_DryGrassTex);
            TEXTURE2D(_FineSandNormal); TEXTURE2D(_WetSandNormal);
            TEXTURE2D(_MeadowNormal); TEXTURE2D(_DryGrassNormal);
            TEXTURE2D(_HabitatMap); SAMPLER(sampler_HabitatMap);

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
                half3 surfaceNormal = normalize(input.normalWS);
                half4 habitat = 0;
                if (_HabitatEnabled > 0.5h)
                {
                    float2 uv = (p - _HabitatBounds.xy) / _HabitatBounds.zw;
                    habitat = SAMPLE_TEXTURE2D(_HabitatMap, sampler_HabitatMap, uv);
                }

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
                // Match the instanced grass cards: rebuild the ground hue from its
                // luma as well, otherwise the painted terrain stays a much brighter,
                // more saturated green and the 3D tufts look pasted onto it.
                half grassLuma = dot(grass, half3(0.299h, 0.587h, 0.114h));
                grass = lerp(grass, grassLuma * half3(0.64h, 0.90h, 0.56h), 0.62h);
                half3 sand = lerp(coast, paleSand, sandPatch * 0.55h);
                // Two noise scales and a warped coordinate give irregular, stable patches,
                // not stripes, grid cells, or a pattern that swims as the camera moves.
                float2 patchPosition = p * _PatchScale +
                    float2(broadPatch, detailPatch) * 0.7;
                half meadowWeight = smoothstep(0.30, 0.66,
                    PatchNoise(patchPosition + 8.3)) * _VariationAmount * _VariationEnabled;
                half dryGrassWeight = smoothstep(0.58, 0.80,
                    PatchNoise(patchPosition * 1.65 + 52.7)) * 0.46h * _VariationAmount * _VariationEnabled;
                float2 grassVariationUv = p * 0.22;
                if (_VariationEnabled > 0.5h)
                {
                    half3 meadow = SAMPLE_TEXTURE2D(_MeadowTex, sampler_GrassTex, grassVariationUv).rgb *
                        _GrassTint.rgb * half3(1.08h, 1.15h, 0.97h);
                    half3 dryGrass = SAMPLE_TEXTURE2D(_DryGrassTex, sampler_GrassTex, grassVariationUv * 1.13 + 11.4).rgb *
                        _GrassTint.rgb * half3(0.86h, 0.94h, 0.80h);
                    grass = lerp(grass, meadow, meadowWeight);
                    grass = lerp(grass, dryGrass, dryGrassWeight);
                }

                half forestBlend = smoothstep(0.26, 0.43, zone);
                // Widen the grass/dirt ramp so the biome edge reads as a soft
                // transition instead of a painted line across the hillside.
                // Noise on the ramp keeps the grass edge ragged instead of a painted
                // contour that follows the elevation band exactly.
                half grassBlend = smoothstep(0.28, 0.66, zone + (detailPatch - 0.5h) * 0.10h);
                float shoreHeight = input.positionWS.y - _SeaLevel + macro * 0.75;
                half shoreBlend = 1.0h - smoothstep(1.4, 4.6, shoreHeight);
                half3 albedo = lerp(rock, dry, smoothstep(0.17, 0.36, zone));
                albedo = lerp(albedo, forest, forestBlend);
                albedo = lerp(albedo, grass, grassBlend);
                albedo = lerp(albedo, sand, shoreBlend);

                // Use real sea-relative elevation instead of painting a fixed
                // radial ring. Broad patches soften the dry-sand/grass boundary.
                float2 beachUv = p * _BeachTiling;
                half beachBlend = 0.0h;
                half fineSandWeight = smoothstep(0.30, 0.68, PatchNoise(patchPosition + 31.2)) *
                    _VariationAmount * _VariationEnabled;
                half wetPatchWeight = (1.0h - smoothstep(0.2, 4.8,
                    shoreHeight + (detailPatch - 0.5) * 2.0)) * _VariationAmount * _VariationEnabled;
                float2 sandVariationUv = beachUv * 2.8;
                if (_BeachEnabled > 0.5h)
                {
                    float beachElevation = shoreHeight + (detailPatch - 0.5) * 1.3;
                    beachBlend = 1.0h - smoothstep(_BeachHeight - max(_BeachBlendWidth, 0.1),
                        _BeachHeight, beachElevation);
                    half3 yellowSand = SAMPLE_TEXTURE2D(_BeachTex, sampler_BeachTex, beachUv).rgb * _BeachTint.rgb;
                    if (_VariationEnabled > 0.5h)
                    {
                        half3 fineSand = SAMPLE_TEXTURE2D(_FineSandTex, sampler_BeachTex, sandVariationUv).rgb *
                            half3(1.42h, 1.30h, 1.04h);
                        half3 wetSand = SAMPLE_TEXTURE2D(_WetSandTex, sampler_BeachTex, sandVariationUv + 13.2).rgb *
                            half3(1.10h, 1.05h, 0.91h);
                        yellowSand = lerp(yellowSand, fineSand, fineSandWeight);
                        yellowSand = lerp(yellowSand, wetSand, wetPatchWeight);
                    }
                    albedo = lerp(albedo, yellowSand, beachBlend);
                }

                half slopeRock = (1.0h - smoothstep(0.68h, 0.92h, surfaceNormal.y)) * 0.70h;
                half exposedSoil = smoothstep(0.50, 0.90, detailPatch) * 0.14h;
                albedo = lerp(albedo, dry, exposedSoil * (1.0h - shoreBlend) * (1.0h - slopeRock));
                albedo = lerp(albedo, rock, slopeRock);
                // A ragged dark band above the water separates the wet sand from
                // the dry beach. Use the local slope and the same beach breakup.
                half tidalHeight = shoreHeight + (detailPatch - 0.5h) * 0.80h;
                half tidalDamp = (1.0h - smoothstep(0.25h, 2.15h, tidalHeight)) * (1.0h - slopeRock);
                albedo *= lerp(1.0h, 0.70h, tidalDamp);
                albedo *= 0.96h + 0.04h * macro;

                half3 rockNormal = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal, sampler_RockNormal, input.uv));
                half3 grassNormal = UnpackNormal(SAMPLE_TEXTURE2D(_GrassNormal, sampler_GrassNormal, input.uv));
                if (_VariationEnabled > 0.5h)
                {
                    half3 meadowNormal = UnpackNormal(SAMPLE_TEXTURE2D(_MeadowNormal, sampler_GrassNormal, grassVariationUv));
                    half3 dryGrassNormal = UnpackNormal(SAMPLE_TEXTURE2D(_DryGrassNormal, sampler_GrassNormal, grassVariationUv * 1.13 + 11.4));
                    grassNormal = normalize(lerp(grassNormal, meadowNormal, meadowWeight * grassBlend));
                    grassNormal = normalize(lerp(grassNormal, dryGrassNormal, dryGrassWeight * grassBlend));
                }
                half3 sandNormal = UnpackNormal(SAMPLE_TEXTURE2D(_SandNormal, sampler_SandNormal, input.uv));
                half3 detailNormal = normalize(lerp(rockNormal, grassNormal, forestBlend));
                detailNormal = normalize(lerp(detailNormal, sandNormal, shoreBlend));
                if (_BeachEnabled > 0.5h)
                {
                    half3 beachNormal = UnpackNormal(SAMPLE_TEXTURE2D(_BeachNormal, sampler_BeachNormal, beachUv));
                    if (_VariationEnabled > 0.5h)
                    {
                        half3 fineNormal = UnpackNormal(SAMPLE_TEXTURE2D(_FineSandNormal, sampler_BeachNormal, sandVariationUv));
                        half3 wetNormal = UnpackNormal(SAMPLE_TEXTURE2D(_WetSandNormal, sampler_BeachNormal, sandVariationUv + 13.2));
                        beachNormal = normalize(lerp(beachNormal, fineNormal, fineSandWeight));
                        beachNormal = normalize(lerp(beachNormal, wetNormal, wetPatchWeight));
                    }
                    detailNormal = normalize(lerp(detailNormal, beachNormal, beachBlend));
                }
                detailNormal = normalize(lerp(detailNormal, rockNormal, slopeRock));
                detailNormal.xy *= 0.55h;
                half damp = 0;
                if (_HabitatEnabled > 0.5h)
                {
                    // World-aligned details break up the baked edges at foot scale.
                    // Sample the same turf, stone and sand surfaces across both scales.
                    half grain = PatchNoise(p * 0.87 + 25.3) - 0.5h;
                    half turf = saturate(habitat.r + grain * 0.21h);
                    half wear = saturate(habitat.g + grain * habitat.g * 0.45h);
                    half stone = saturate(habitat.b + grain * habitat.b * 0.43h);
                    damp = habitat.a * saturate(beachBlend + shoreBlend);
                    float2 earthUv = p * 0.34 + float2(5.7, 13.2);
                    half3 earth = SAMPLE_TEXTURE2D(_ForestTex, sampler_ForestTex, earthUv).rgb;
                    half3 grit = SAMPLE_TEXTURE2D(_DryTex, sampler_DryTex, earthUv * 0.71).rgb;
                    // Keep the path blend readable while breaking up the large
                    // stretches of uniform ochre soil between existing grass tufts.
                    half organicSoil = smoothstep(0.30h, 0.68h, broadPatch * 0.48h + detailPatch * 0.52h);
                    earth *= lerp(half3(0.78h, 0.70h, 0.57h), half3(0.59h, 0.56h, 0.44h), organicSoil);
                    earth = lerp(earth, grit * half3(0.81h, 0.69h, 0.54h), wear * 0.91h);
                    half shore = max(beachBlend, shoreBlend);
                    half turfCoverage = turf * (1.0h - wear * 0.88h);
                    half edge = 4.0h * turfCoverage * (1.0h - turfCoverage);
                    // Use the existing turf sample as a height cue where both
                    // materials meet, so the boundary follows the ground texture.
                    half turfRelief = dot(leafy, half3(0.2126h, 0.7152h, 0.0722h));
                    turfCoverage = saturate(turfCoverage + (turfRelief - 0.22h + grain * 0.4h) * edge * 0.42h);
                    half3 inland = lerp(earth, grass * half3(0.83h, 0.89h, 0.77h), turfCoverage);
                    albedo = lerp(inland, albedo, shore);
                    // Rotate the rock projection out of the default terrain UV lattice.
                    float2 rockUv = float2(dot(p, float2(0.143, 0.081)), dot(p, float2(-0.081, 0.143)));
                    half3 fracturedRock = SAMPLE_TEXTURE2D(_RockTex, sampler_RockTex, rockUv).rgb * _RockTint.rgb;
                    albedo = lerp(albedo, fracturedRock * half3(0.84h, 0.86h, 0.83h), stone);
                    albedo *= lerp(1.0h, 0.65h, damp);
                    half3 earthNormal = UnpackNormal(SAMPLE_TEXTURE2D(_GrassNormal, sampler_GrassNormal, earthUv));
                    half3 fractureNormal = UnpackNormal(SAMPLE_TEXTURE2D(_RockNormal, sampler_RockNormal, rockUv));
                    fractureNormal.xy = mul(half2x2(0.87h, -0.49h, 0.49h, 0.87h), fractureNormal.xy);
                    half3 inlandNormal = lerp(earthNormal * half3(0.85h, 0.85h, 1), grassNormal, turfCoverage);
                    detailNormal = lerp(lerp(inlandNormal, detailNormal, shore), fractureNormal, stone);
                    detailNormal.xy *= lerp(1.0h, 0.55h, damp);
                }
                float3 viewDelta = input.positionWS - _WorldSpaceCameraPos;
                detailNormal.xy *= lerp(1.0h,.58h,smoothstep(1225.0,15625.0,dot(viewDelta,viewDelta)));
                detailNormal = normalize(detailNormal);

                half3 tangentWS = normalize(half3(1.0h, -surfaceNormal.x / max(surfaceNormal.y, 0.1h), 0.0h));
                half3 bitangentWS = normalize(cross(tangentWS, surfaceNormal));
                half3 normalWS = normalize(detailNormal.x * tangentWS + detailNormal.y * bitangentWS + detailNormal.z * surfaceNormal);

                // Low-frequency mineral/soil variation sits underneath foot-scale
                // detail instead of repeating the same contrast at every distance.
                half mineral = PatchNoise(p * .013 + float2(41.7, 9.2));
                albedo *= lerp(half3(.89h,.91h,.88h), half3(1.04h,1.02h,.98h), mineral);
                half surfaceLuma=dot(albedo,half3(.2126h,.7152h,.0722h));
                albedo=lerp(albedo,surfaceLuma*half3(1.04h,1.0h,.93h),max(shoreBlend,beachBlend)*.24h)*.91h;
                half stoneSurface = saturate(max(slopeRock, habitat.b));
                half grainRoughness = PatchNoise(p * .39 + 71.3);
                SurfaceData surface = (SurfaceData)0;
                surface.albedo = max(albedo, 0);
                surface.alpha = 1;
                surface.metallic = 0;
                surface.smoothness = lerp(.08h, .23h, stoneSurface) + (grainRoughness-.5h)*.06h;
                surface.smoothness = lerp(surface.smoothness, .57h, max(damp, tidalDamp * .72h));
                surface.occlusion = 1;
                surface.normalTS = detailNormal;
                InputData lighting = (InputData)0;
                lighting.positionWS = input.positionWS;
                lighting.normalWS = normalWS;
                lighting.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                lighting.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                lighting.bakedGI = SampleSH(normalWS);
                lighting.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                lighting.shadowMask = half4(1,1,1,1);
                half4 lit = UniversalFragmentPBR(lighting, surface);
                return half4(MixFog(lit.rgb, input.fogFactor), 1.0h);
            }
            ENDHLSL
        }

        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        // The PC SSAO depth-normals prepass also supplies depth to the ocean.
        UsePass "Universal Render Pipeline/Lit/DepthNormals"
    }
    FallBack Off
}
