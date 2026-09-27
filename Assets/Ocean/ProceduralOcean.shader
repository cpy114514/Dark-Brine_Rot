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

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                float4 screenPosition : TEXCOORD2;
                half fogFactor : TEXCOORD3;
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

            // World-space, slowly drifting fields make neighbouring crests differ
            // without jittering their phase independently every frame.
            float2 WaveVariation(float2 p, float time)
            {
                return float2(
                    ValueNoise(p * 0.011 + float2(17.3, -8.1) + time * float2(0.0017, -0.0011)),
                    ValueNoise(p * 0.023 + float2(-12.7, 21.4) + time * float2(-0.0013, 0.0019)));
            }

            float2 WaveSample(float2 p, float2 variation)
            {
                return p + (variation - 0.5) * (10.0 * _WaveIrregularity);
            }

            float2 WaveWeights(float2 variation)
            {
                float swell = 1.0 + _WaveIrregularity *
                    ((variation.x - 0.5) * 0.85 + (variation.y - 0.5) * 0.30);
                float chop = 1.0 + _WaveIrregularity *
                    ((variation.y - 0.5) * 0.70 - (variation.x - 0.5) * 0.20);
                return float2(swell, chop);
            }

            // A Gerstner wave displaces points sideways as well as vertically. This creates
            // a rolling crest rather than the up/down look of a simple sine surface.
            float3 AddGerstnerWave(float4 wave, float4 motion, float weight, float3 samplePosition, float time, inout float3 tangent, inout float3 binormal)
            {
                float2 direction = normalize(wave.xy);
                float waveNumber = TWO_PI / max(wave.w, 0.001);
                float phase = waveNumber * (dot(direction, samplePosition.xz) - motion.x * time);
                float sine = sin(phase);
                float cosine = cos(phase);
                float amplitude = wave.z * weight;
                float steepness = min(motion.y, 0.95);
                float horizontal = steepness * amplitude;
                float slope = amplitude * waveNumber;

                tangent += float3(-direction.x * direction.x * horizontal * waveNumber * sine,
                                   direction.x * slope * cosine,
                                  -direction.x * direction.y * horizontal * waveNumber * sine);
                binormal += float3(-direction.x * direction.y * horizontal * waveNumber * sine,
                                    direction.y * slope * cosine,
                                   -direction.y * direction.y * horizontal * waveNumber * sine);
                return float3(direction.x * horizontal * cosine, amplitude * sine, direction.y * horizontal * cosine);
            }

            // Per-pixel crest detection keeps foam narrow and smooth instead of turning it
            // into large facets based on the underlying mesh triangles.
            float CrestAt(float2 samplePosition, float time, float2 variation)
            {
                float2 warped = WaveSample(samplePosition, variation);
                float2 weights = WaveWeights(variation);
                float a = sin((dot(normalize(_Wave1.xy), warped) - _Wave1Motion.x * time) * (TWO_PI / max(_Wave1.w, 0.001))) * (_Wave1.z * weights.x * 0.34);
                float b = sin((dot(normalize(_Wave2.xy), warped) - _Wave2Motion.x * (time + 1.9)) * (TWO_PI / max(_Wave2.w, 0.001))) * (_Wave2.z * weights.y * 0.40);
                float c = sin((dot(normalize(_Wave3.xy), warped) - _Wave3Motion.x * (time + 4.2)) * (TWO_PI / max(_Wave3.w, 0.001))) * (_Wave3.z * weights.y * 0.50);
                float d = sin((dot(normalize(_Wave4.xy), warped) - _Wave4Motion.x * (time + 2.7)) * (TWO_PI / max(_Wave4.w, 0.001))) * (_Wave4.z * weights.x * 0.65);
                return smoothstep(0.36, 0.72, a + b + c + d);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float oceanTime = _Time.y * _OceanMotionSpeed;
                float3 basePosition = TransformObjectToWorld(input.positionOS.xyz);
                float distanceToCamera = distance(basePosition.xz, _WorldSpaceCameraPos.xz);
                float3 tangent = float3(1.0, 0.0, 0.0);
                float3 binormal = float3(0.0, 0.0, 1.0);
                float3 displacement = 0.0;
                float2 variation = WaveVariation(basePosition.xz, oceanTime);
                float2 weights = WaveWeights(variation);
                float3 wavePosition = basePosition;
                wavePosition.xz = WaveSample(basePosition.xz, variation);

                float mediumWeight = 1.0 - smoothstep(_MidDetailDistance * 0.75, _MidDetailDistance, distanceToCamera);
                float nearWeight = 1.0 - smoothstep(_NearDetailDistance * 0.65, _NearDetailDistance, distanceToCamera);
                displacement += AddGerstnerWave(_Wave1, _Wave1Motion, weights.x, wavePosition, oceanTime, tangent, binormal);
                displacement += AddGerstnerWave(_Wave2, _Wave2Motion, weights.y, wavePosition, oceanTime + 1.9, tangent, binormal);
                if (mediumWeight > 0.001)
                    displacement += AddGerstnerWave(_Wave3, _Wave3Motion, mediumWeight * weights.y, wavePosition, oceanTime + 4.2, tangent, binormal);
                if (nearWeight > 0.001)
                    displacement += AddGerstnerWave(_Wave4, _Wave4Motion, nearWeight * weights.x, wavePosition, oceanTime + 2.7, tangent, binormal);

                output.positionWS = basePosition + displacement;
                output.normalWS = normalize(cross(binormal, tangent));
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float oceanTime = _Time.y * _OceanMotionSpeed;
                float distanceToCamera = distance(input.positionWS.xz, _WorldSpaceCameraPos.xz);
                float nearDetailFade = 1.0 - smoothstep(_NearDetailDistance * 0.68, _NearDetailDistance, distanceToCamera);
                float mediumDetailFade = 1.0 - smoothstep(_MidDetailDistance * 0.72, _MidDetailDistance, distanceToCamera);
                float2 p = input.positionWS.xz;

                // Three procedural scales affect the surface only near the camera, avoiding
                // repeated normal maps and distant shimmer at the horizon.
                float largeNoise = ValueNoise(p * 0.035 + oceanTime * float2(0.022, -0.014));
                float mediumNoise = ValueNoise(p * 0.115 + oceanTime * float2(-0.065, 0.041));
                float rippleNoise = ValueNoise(p * 0.58 + oceanTime * float2(0.22, 0.16));
                float detailSlope = (largeNoise - 0.5) * _LargeDetailStrength * mediumDetailFade
                                  + (mediumNoise - 0.5) * _MediumDetailStrength * nearDetailFade
                                  + (rippleNoise - 0.5) * _RippleStrength * nearDetailFade;
                float2 capillaryA = normalize(float2(0.93, 0.37));
                float2 capillaryB = normalize(float2(-0.46, 0.89));
                float microA = cos(dot(p, capillaryA) * 2.2 - oceanTime * 3.0 + mediumNoise * 2.0) * 0.045;
                float microB = cos(dot(p, capillaryB) * 4.5 - oceanTime * 4.7) * 0.022;
                float2 capillaryTilt = (capillaryA * microA + capillaryB * microB) * nearDetailFade;
                half3 normalWS = normalize(input.normalWS + half3(-detailSlope - capillaryTilt.x, 0,
                    detailSlope * 0.72 - capillaryTilt.y));
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

                Light sun = GetMainLight();
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirection)), _FresnelPower) * _FresnelStrength;
                half3 halfDirection = SafeNormalize(sun.direction + viewDirection);
                half sunGlint = pow(saturate(dot(normalWS, halfDirection)), _SpecularSharpness) * _SpecularStrength;
                half3 reflectionVector = reflect(-viewDirection, normalWS);
                half perceptualRoughness = saturate(1.0h - _Smoothness + abs(detailSlope) * 0.10h);
                half3 probeReflection = GlossyEnvironmentReflection(reflectionVector, perceptualRoughness, 1.0h);
                half3 coldSkyFallback = _WaterReflectionTint.rgb;
                water = lerp(water, max(probeReflection, coldSkyFallback * 0.70h), saturate(fresnel * _ReflectionStrength));

                float glintNoise = ValueNoise(p * 0.31 + oceanTime * float2(0.16, -0.11));
                half glintField = glintNoise * 0.48h + mediumNoise * 0.32h + rippleNoise * 0.20h;
                half glintMask = smoothstep(_SunGlitterThreshold - 0.16h,
                    _SunGlitterThreshold + 0.10h, glintField);
                half glintCluster = smoothstep(0.28h, 0.64h, largeNoise);
                water += half3(0.82h, 0.91h, 0.95h) * sun.color.rgb * sunGlint *
                    (0.015h + glintMask * glintCluster * _SunGlitterStrength);

                float foamNoise = ValueNoise(p * _FoamNoiseScale + oceanTime * float2(0.15, -0.10) * _FoamSpeed);
                float foamRegion = ValueNoise(p * (_FoamNoiseScale * 0.27) + float2(9.3, -17.6)
                    + oceanTime * float2(0.008, -0.006) * _FoamSpeed);
                float foamDetail = ValueNoise(p * (_FoamNoiseScale * 2.4) + float2(-14.2, 3.8)
                    + oceanTime * float2(-0.09, 0.07) * _FoamSpeed);
                float2 waveVariation = WaveVariation(p, oceanTime);
                half crestCutoff = lerp(0.52h, 0.45h + (1.0h - foamRegion) * 0.18h, _FoamIrregularity);
                half crestFoam = CrestAt(p, oceanTime, waveVariation) *
                    smoothstep(crestCutoff, crestCutoff + 0.22h, foamNoise) * mediumDetailFade;
                // Opaque depth measures water above submerged terrain. A narrow
                // bright contact line plus irregular advancing bands follows the
                // real coastline, while deep water receives no shore foam.
                half shoreMask = 1.0h - smoothstep(0.0h, _FoamWidth, waterThickness);
                half contactPatch = lerp(1.0h, smoothstep(0.25h, 0.70h,
                    foamRegion * 0.65h + foamDetail * 0.35h), _FoamIrregularity * 0.85h);
                half contactWidth = lerp(0.28h, 0.15h + foamRegion * 0.26h, _FoamIrregularity);
                half contactFoam = (1.0h - smoothstep(0.0h, contactWidth, waterThickness)) *
                    0.45h * contactPatch;
                half localSpeed = 1.8h + _FoamIrregularity * (foamRegion - 0.5h) * 0.9h;
                half localSpacing = 2.5h + _FoamIrregularity * (foamDetail - 0.5h) * 1.1h;
                half shorePulse = sin(waterThickness * localSpacing - oceanTime * localSpeed
                    + foamNoise * 3.0h + (foamRegion - 0.5h) * _FoamIrregularity * 5.0h) * 0.5h + 0.5h;
                half patchMask = lerp(1.0h, smoothstep(0.29h, 0.67h,
                    foamRegion * 0.7h + foamDetail * 0.3h), _FoamIrregularity * 0.8h);
                half breakerFoam = smoothstep(0.64h, 0.93h, shorePulse) *
                    (0.25h + foamNoise * 0.55h + foamDetail * 0.2h) * patchMask;
                half foam = max(crestFoam * _WhitecapStrength, max(contactFoam, breakerFoam) * shoreMask) * _FoamStrength;
                water = lerp(water, _FoamColor.rgb, saturate(foam));

                half haze = smoothstep(_MidDetailDistance * 0.68h, _ViewDistance, distanceToCamera);
                water = lerp(water, coldSkyFallback, haze * 0.48h);
                water = MixFog(water, input.fogFactor);
                return half4(water, 1.0h);
            }
            ENDHLSL
        }
    }
}
