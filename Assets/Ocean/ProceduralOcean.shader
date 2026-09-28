Shader "DarkBrine/Procedural Ocean"
{
    Properties
    {
        [Header(Colors)]
        _ShallowColor ("Shallow brine", Color) = (0.09, 0.22, 0.24, 1)
        _MidColor ("Mid brine", Color) = (0.026, 0.09, 0.12, 1)
        _DeepColor ("Deep brine", Color) = (0.012, 0.043, 0.065, 1)
        _WaterReflectionTint ("Night reflection tint", Color) = (0.07, 0.16, 0.21, 1)
        _FoamColor ("Cold foam", Color) = (0.93, 0.97, 0.98, 1)
        _DepthFadeDistance ("Depth fade distance", Float) = 5.5
        _WaterOpacity ("Water opacity", Range(0, 1)) = 0.94
        _AbsorptionStrength ("Absorption strength", Range(0.1, 8)) = 3.7

        [Header(Waves)]
        _OceanMotionSpeed ("Ocean animation speed", Range(0.25, 4)) = 1.8
        _Wave1 ("Wave 1 (dir XZ, amplitude, wavelength)", Vector) = (0.82, 0.57, 1.30, 95)
        _Wave1Motion ("Wave 1 (speed, steepness)", Vector) = (12.2, 0.25, 0, 0)
        _Wave2 ("Wave 2 (dir XZ, amplitude, wavelength)", Vector) = (-0.38, 0.93, 0.75, 44)
        _Wave2Motion ("Wave 2 (speed, steepness)", Vector) = (8.3, 0.20, 0, 0)
        _Wave3 ("Wave 3 (dir XZ, amplitude, wavelength)", Vector) = (0.96, -0.29, 0.28, 18)
        _Wave3Motion ("Wave 3 (speed, steepness)", Vector) = (5.3, 0.12, 0, 0)
        _Wave4 ("Wave 4 (dir XZ, amplitude, wavelength)", Vector) = (-0.72, -0.69, 0.11, 8)
        _Wave4Motion ("Wave 4 (speed, steepness)", Vector) = (3.5, 0.08, 0, 0)
        _WaveIrregularity ("Wave irregularity", Range(0, 1)) = 0.7

        [Header(Surface Detail)]
        _LargeDetailStrength ("Large variation", Range(0, 2)) = 0.34
        _MediumDetailStrength ("Medium waves", Range(0, 2)) = 0.22
        _RippleStrength ("Highlight ripples", Range(0, 2)) = 0.12

        [Header(Reflection And Specular)]
        _Smoothness ("Water smoothness", Range(0, 1)) = 0.76
        _ReflectionStrength ("Reflection strength", Range(0, 2)) = 0.38
        _SpecularStrength ("Specular strength", Range(0, 2)) = 0.55
        _SpecularSharpness ("Specular sharpness", Range(20, 300)) = 118
        _SunGlitterStrength ("Sun glitter strength", Range(0, 2)) = 0.24
        _SunGlitterThreshold ("Sun glitter threshold", Range(0, 1)) = 0.68
        _FresnelStrength ("Fresnel strength", Range(0, 2)) = 0.82
        _FresnelPower ("Fresnel power", Range(1, 9)) = 4.4

        [Header(Brine)]
        _BrineNoiseScale ("Brine noise scale", Range(0.01, 1)) = 0.065
        _BrineFlowSpeed ("Brine flow speed", Range(0, 1)) = 0.07
        _BrineStrength ("Brine strength", Range(0, 1)) = 0.10

        [Header(Foam)]
        _FoamWidth ("Foam width", Range(0.05, 8)) = 3.0
        _FoamStrength ("Foam strength", Range(0, 2)) = 1.05
        _FoamNoiseScale ("Foam noise scale", Range(0.05, 2)) = 0.22
        _FoamSpeed ("Foam speed", Range(0, 2)) = 0.28
        _FoamIrregularity ("Foam irregularity", Range(0, 1)) = 0.75
        _WhitecapStrength ("Open-water whitecaps", Range(0, 1)) = 0.55

        _NearDetailDistance ("Near detail distance", Float) = 180
        _MidDetailDistance ("Mid detail distance", Float) = 700
        _ViewDistance ("View distance", Float) = 3000
    }

    SubShader
    {
        // Draw after opaque scene geometry, so opaque islands naturally cover the water.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-100" "RenderType"="Transparent" }
        Pass
        {
            Name "OceanForward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            ZTest LEqual
            // The swimmer's camera can settle just below the waterline. Keep
            // the displaced surface visible from that side as well, rather
            // than letting the sky show through a one-sided ocean plane.
            Cull Off

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #define _SURFACE_TYPE_TRANSPARENT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _WaterReflectionTint;
                half4 _ShallowColor;
                half4 _MidColor;
                half4 _FoamColor;
                float4 _Wave1;
                float4 _Wave1Motion;
                float4 _Wave2;
                float4 _Wave2Motion;
                float4 _Wave3;
                float4 _Wave3Motion;
                float4 _Wave4;
                float4 _Wave4Motion;
                half _WaveIrregularity;
                float _OceanMotionSpeed;
                half _WhitecapStrength;
                half _DepthFadeDistance;
                half _WaterOpacity;
                half _AbsorptionStrength;
                half _LargeDetailStrength;
                half _MediumDetailStrength;
                half _RippleStrength;
                half _Smoothness;
                half _ReflectionStrength;
                half _SpecularStrength;
                half _SpecularSharpness;
                half _SunGlitterStrength;
                half _SunGlitterThreshold;
                half _FresnelStrength;
                half _FresnelPower;
                half _BrineNoiseScale;
                half _BrineFlowSpeed;
                half _BrineStrength;
                half _FoamWidth;
                half _FoamStrength;
                half _FoamNoiseScale;
                half _FoamSpeed;
                half _FoamIrregularity;
                float _NearDetailDistance;
                float _MidDetailDistance;
                float _ViewDistance;
            CBUFFER_END

            #include "Assets/Ocean/OceanWaves.hlsl"

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                half fogFactor : TEXCOORD3;
                float2 baseXZ : TEXCOORD4;
            };

            float Hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            float ValueNoise(float2 p)
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
                float oceanTime = _Time.y * _OceanMotionSpeed;
                float3 basePosition = TransformObjectToWorld(input.positionOS.xyz);
                float distanceToCamera = distance(basePosition.xz, _WorldSpaceCameraPos.xz);
                float3 displacement, waveNormal;
                float crest;
                OceanEvaluateWaves(basePosition.xz, oceanTime, OceanDetailWeights(distanceToCamera),
                    displacement, waveNormal, crest);
                output.positionWS = basePosition + displacement;
                output.normalWS = waveNormal;
                output.baseXZ = basePosition.xz;
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float oceanTime = _Time.y * _OceanMotionSpeed;
                float distanceToCamera = distance(input.baseXZ, _WorldSpaceCameraPos.xz);
                float2 detailWeights = OceanDetailWeights(distanceToCamera);
                float nearDetailFade = detailWeights.x;
                float mediumDetailFade = detailWeights.y;
                float2 p = input.baseXZ;
                float3 displacement, waveNormal;
                float waveCrest;
                OceanEvaluateWaves(p, oceanTime, detailWeights, displacement, waveNormal, waveCrest);

                // Three procedural scales affect the surface only near the camera, avoiding
                // repeated normal maps and distant shimmer at the horizon.
                float3 largeField = OceanNoiseGradient(p * 0.035 + oceanTime * float2(0.022, -0.014));
                float3 mediumField = OceanNoiseGradient(p * 0.115 + oceanTime * float2(-0.065, 0.041));
                float3 rippleField = OceanNoiseGradient(p * 0.58 + oceanTime * float2(0.22, 0.16));
                float largeNoise = largeField.x, mediumNoise = mediumField.x, rippleNoise = rippleField.x;
                float pixelFootprint = max(length(ddx(p)), length(ddy(p)));
                float rippleFilter = 1.0 - smoothstep(0.35, 1.1, pixelFootprint * 0.58);
                // Actual two-axis height derivatives, not a fixed diagonal tilt.
                float2 detailSlope = largeField.yz * (0.035 * 8.0 * _LargeDetailStrength) * mediumDetailFade
                    + mediumField.yz * (0.115 * 2.0 * _MediumDetailStrength) * nearDetailFade
                    + rippleField.yz * (0.58 * 0.2 * _RippleStrength) * nearDetailFade * rippleFilter;
                float2 capillaryA = normalize(float2(0.93, 0.37));
                float2 capillaryB = normalize(float2(-0.46, 0.89));
                float microA = cos(dot(p, capillaryA) * 2.2 - oceanTime * 3.0 + mediumNoise * 2.0) * 0.045;
                float microB = cos(dot(p, capillaryB) * 4.5 - oceanTime * 4.7) * 0.022;
                float filterA = 1.0 - smoothstep(0.8, 2.4, pixelFootprint * 2.2);
                float filterB = 1.0 - smoothstep(0.8, 2.4, pixelFootprint * 4.5);
                float2 capillaryTilt = (capillaryA * microA * filterA + capillaryB * microB * filterB) * nearDetailFade;
                half3 normalWS = normalize(waveNormal + float3(-detailSlope.x - capillaryTilt.x, 0,
                    -detailSlope.y - capillaryTilt.y));
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                float sceneRawDepth = SampleSceneDepth(screenUv);
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                // SV_POSITION is already rasterized in the fragment stage, so
                // z/w is not clip depth here. Use view-space water depth to
                // compare like-for-like with the sampled opaque scene depth.
                float waterEyeDepth = -TransformWorldToView(input.positionWS).z;
                float waterThickness = max(0.0, sceneEyeDepth - waterEyeDepth);
                float shallowToMid = saturate(waterThickness / max(_DepthFadeDistance, 0.01));
                float midToDeep = saturate(waterThickness * _AbsorptionStrength / max(_DepthFadeDistance, 0.01));

                half3 bodyColor = lerp(_ShallowColor.rgb, _MidColor.rgb, shallowToMid);
                bodyColor = lerp(bodyColor, _DeepColor.rgb, midToDeep);
                float brineNoise = ValueNoise(p * _BrineNoiseScale + oceanTime * float2(0.028, -0.018) * _BrineFlowSpeed);
                bodyColor *= 1.0h + (brineNoise - 0.5h) * _BrineStrength;

                // Opaque Texture permits only a restrained near-shore hint of terrain.
                half3 sceneColor = SampleSceneColor(screenUv);
                half opacity = saturate(_WaterOpacity + waterThickness * 0.12);
                half3 water = lerp(sceneColor, bodyColor, opacity);

                Light sun = GetMainLight(TransformWorldToShadowCoord(input.positionWS), input.positionWS,
                    half4(1, 1, 1, 1));
                half NoL = saturate(dot(normalWS, sun.direction));
                half NoV = saturate(dot(normalWS, viewDirection));
                half attenuation = sun.distanceAttenuation * sun.shadowAttenuation;
                half3 ambient = max(SampleSH(waveNormal), 0.0h);
                // Keep the authored brine palette, with restrained scattering light.
                half3 bodyIllumination = lerp(half3(1, 1, 1),
                    saturate(ambient * 0.8h + sun.color * NoL * attenuation * 0.55h), 0.45h);
                water *= bodyIllumination;
                half fresnel = (0.0204h + 0.9796h * pow(1.0h - NoV, _FresnelPower)) * _FresnelStrength;
                half3 reflectionVector = reflect(-viewDirection, normalWS);
                float normalVariance = dot(ddx(normalWS), ddx(normalWS)) + dot(ddy(normalWS), ddy(normalWS));
                half perceptualRoughness = saturate(sqrt((1.0h - _Smoothness) * (1.0h - _Smoothness)
                    + min(normalVariance, 0.25) * 0.35));
                half3 probeReflection = GlossyEnvironmentReflection(reflectionVector, input.positionWS,
                    perceptualRoughness, 1.0h, screenUv);
                half3 coldSkyFallback = _WaterReflectionTint.rgb;
                // Preserve dark reflected silhouettes instead of raising each colour
                // channel to a sky-colour floor. Use fallback only for an empty probe.
                half probeAvailable = step(0.0001h, dot(probeReflection, half3(0.2126h, 0.7152h, 0.0722h)));
                half3 environment = lerp(coldSkyFallback * 0.70h, probeReflection, probeAvailable);
                water = lerp(water, environment, saturate(fresnel * _ReflectionStrength));

                float glintNoise = ValueNoise(p * 0.31 + oceanTime * float2(0.16, -0.11));
                half glintField = glintNoise * 0.48h + mediumNoise * 0.32h + rippleNoise * 0.20h;
                half glintMask = smoothstep(_SunGlitterThreshold - 0.16h,
                    _SunGlitterThreshold + 0.10h, glintField);
                half glintCluster = smoothstep(0.28h, 0.64h, largeNoise);
                BRDFData waterBrdf;
                half alpha = 1.0h;
                half roughnessFloor = sqrt(sqrt(max(2.0h / max(_SpecularSharpness + 2.0h, 1.0h), 0.0001h))) * 0.75h;
                InitializeBRDFDataDirect(half3(0, 0, 0), half3(0, 0, 0), half3(0.0204h, 0.0204h, 0.0204h),
                    0.0204h, 0.9796h, 1.0h - max(perceptualRoughness, roughnessFloor), alpha, waterBrdf);
                half sunGlint = DirectBRDFSpecular(waterBrdf, normalWS, sun.direction, viewDirection);
                water += waterBrdf.specular * sun.color * NoL * attenuation * sunGlint * _SpecularStrength *
                    (0.75h + glintMask * glintCluster * _SunGlitterStrength);

                float foamNoise = ValueNoise(p * _FoamNoiseScale + oceanTime * float2(0.15, -0.10) * _FoamSpeed);
                float foamRegion = ValueNoise(p * (_FoamNoiseScale * 0.27) + float2(9.3, -17.6)
                    + oceanTime * float2(0.008, -0.006) * _FoamSpeed);
                float foamDetail = ValueNoise(p * (_FoamNoiseScale * 2.4) + float2(-14.2, 3.8)
                    + oceanTime * float2(-0.09, 0.07) * _FoamSpeed);
                half crestCutoff = lerp(0.52h, 0.45h + (1.0h - foamRegion) * 0.18h, _FoamIrregularity);
                half crestFoam = waveCrest *
                    smoothstep(crestCutoff, crestCutoff + 0.22h, foamNoise) * mediumDetailFade *
                    (0.38h + 0.62h * smoothstep(0.23h, 0.62h, foamDetail));
                // Opaque depth measures water above submerged terrain. A narrow
                // bright contact line plus irregular advancing bands follows the
                // real coastline, while deep water receives no shore foam.
                half shoreMask = 1.0h - smoothstep(0.0h, _FoamWidth, waterThickness);
                half contactPatch = lerp(1.0h, smoothstep(0.25h, 0.70h,
                    foamRegion * 0.65h + foamDetail * 0.35h), _FoamIrregularity * 0.55h);
                half contactWidth = lerp(0.28h, 0.15h + foamRegion * 0.26h, _FoamIrregularity);
                half contactFoam = (1.0h - smoothstep(0.0h, contactWidth, waterThickness)) *
                    0.72h * contactPatch;
                half localSpacing = 2.5h + _FoamIrregularity * (foamDetail - 0.5h) * 1.1h;
                half shorePulse = sin(waterThickness * localSpacing - oceanTime * 1.8h
                    + foamNoise * 3.0h + (foamRegion - 0.5h) * _FoamIrregularity * 5.0h) * 0.5h + 0.5h;
                half patchMask = lerp(1.0h, smoothstep(0.29h, 0.67h,
                    foamRegion * 0.7h + foamDetail * 0.3h), _FoamIrregularity * 0.8h);
                half breakerFoam = smoothstep(0.57h, 0.87h, shorePulse) *
                    (0.34h + foamNoise * 0.58h + foamDetail * 0.24h) * patchMask;
                half foam = max(crestFoam * _WhitecapStrength, max(contactFoam, breakerFoam) * shoreMask) * _FoamStrength;
                // Lace stays white in daylight, but is no longer self-lit at night
                // or under tree/island shadows.
                half3 foamIllumination = saturate(ambient + sun.color *
                    saturate(dot(waveNormal, sun.direction)) * attenuation);
                water = lerp(water, _FoamColor.rgb * foamIllumination, saturate(foam));

                half haze = smoothstep(_MidDetailDistance * 0.68h, _ViewDistance, distanceToCamera);
                water = lerp(water, coldSkyFallback, haze * 0.48h);
                water = MixFog(water, input.fogFactor);
                return half4(water, 1.0h);
            }
            ENDHLSL
        }
    }
}
