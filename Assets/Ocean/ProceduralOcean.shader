Shader "DarkBrine/Procedural Ocean"
{
    Properties
    {
        [Header(Colors)]
        _ShallowColor ("Shallow brine", Color) = (0.045, 0.095, 0.120, 1)
        _MidColor ("Mid brine", Color) = (0.012, 0.042, 0.058, 1)
        _DeepColor ("Deep brine", Color) = (0.004, 0.012, 0.020, 1)
        _FoamColor ("Cold foam", Color) = (0.62, 0.80, 0.86, 1)
        _DepthFadeDistance ("Depth fade distance", Float) = 5.5
        _WaterOpacity ("Water opacity", Range(0, 1)) = 0.94
        _AbsorptionStrength ("Absorption strength", Range(0.1, 8)) = 3.7

        [Header(Waves)]
        _Wave1 ("Wave 1 (dir XZ, amplitude, wavelength)", Vector) = (0.82, 0.57, 1.35, 180)
        _Wave1Motion ("Wave 1 (speed, steepness)", Vector) = (0.82, 0.15, 0, 0)
        _Wave2 ("Wave 2 (dir XZ, amplitude, wavelength)", Vector) = (-0.38, 0.93, 0.72, 92)
        _Wave2Motion ("Wave 2 (speed, steepness)", Vector) = (0.95, 0.10, 0, 0)
        _Wave3 ("Wave 3 (dir XZ, amplitude, wavelength)", Vector) = (0.96, -0.29, 0.32, 58)
        _Wave3Motion ("Wave 3 (speed, steepness)", Vector) = (1.12, 0.06, 0, 0)
        _Wave4 ("Wave 4 (dir XZ, amplitude, wavelength)", Vector) = (-0.72, -0.69, 0.16, 35)
        _Wave4Motion ("Wave 4 (speed, steepness)", Vector) = (1.28, 0.03, 0, 0)

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
        _FoamWidth ("Foam width", Range(0.05, 8)) = 2.2
        _FoamStrength ("Foam strength", Range(0, 2)) = 0.66
        _FoamNoiseScale ("Foam noise scale", Range(0.05, 2)) = 0.22
        _FoamSpeed ("Foam speed", Range(0, 2)) = 0.28

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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
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
                float2 baseXZ : TEXCOORD4;
                half regionMix : TEXCOORD5;
            };

            // A Gerstner wave displaces points sideways as well as vertically. This creates
            // a rolling crest rather than the up/down look of a simple sine surface.
            // regionMix ∈ [0,1] is a spatial noise that scales the wave amplitude: quiet
            // patches stay glassy, lively patches roll steep crests, so the surface is no
            // longer a single global waveform.
            float3 AddGerstnerWave(float4 wave, float4 motion, float3 samplePosition, float time, float regionMix, inout float3 tangent, inout float3 binormal)
            {
                float2 direction = normalize(wave.xy);
                float waveNumber = TWO_PI / max(wave.w, 0.001);
                float phase = waveNumber * (dot(direction, samplePosition.xz) - motion.x * time);
                float sine = sin(phase);
                float cosine = cos(phase);
                float ampModulation = lerp(0.20, 1.50, regionMix);
                float amplitude = wave.z * ampModulation;
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
            // into large facets based on the underlying mesh triangles. regionMix reuses the
            // spatial amplitude modulation so foam only lights up where waves are tall.
            float CrestAt(float2 samplePosition, float time, float regionMix)
            {
                float ampModulation = lerp(0.20, 1.50, regionMix);
                float a = sin((dot(normalize(_Wave1.xy), samplePosition) - _Wave1Motion.x * time) * (TWO_PI / max(_Wave1.w, 0.001))) * (_Wave1.z * 0.34 * ampModulation);
                float b = sin((dot(normalize(_Wave2.xy), samplePosition) - _Wave2Motion.x * time) * (TWO_PI / max(_Wave2.w, 0.001)) + 1.9) * (_Wave2.z * 0.40 * ampModulation);
                float c = sin((dot(normalize(_Wave3.xy), samplePosition) - _Wave3Motion.x * time) * (TWO_PI / max(_Wave3.w, 0.001)) + 4.2) * (_Wave3.z * 0.50 * ampModulation);
                float d = sin((dot(normalize(_Wave4.xy), samplePosition) - _Wave4Motion.x * time) * (TWO_PI / max(_Wave4.w, 0.001)) + 2.7) * (_Wave4.z * 0.65 * ampModulation);
                return smoothstep(0.56, 0.90, a + b + c + d);
            }

            // A small, texture-free value-noise field breaks the foam into
            // irregular patches instead of repeating stripes on every crest.
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
                float3 basePosition = TransformObjectToWorld(input.positionOS.xyz);
                float distanceToCamera = distance(basePosition.xz, _WorldSpaceCameraPos.xz);
                float3 tangent = float3(1.0, 0.0, 0.0);
                float3 binormal = float3(0.0, 0.0, 1.0);
                float3 displacement = 0.0;

                // Spatial amplitude modulation: low-frequency blocks decide which patches
                // are lively vs glassy, high-frequency noise breaks the boundary into an
                // irregular seam. Both are sampled in undisplaced world-space xz so the
                // wave shape and the foam stay locked to the same baseline.
                float regionNoiseLow = ValueNoise(basePosition.xz * 0.012 + float2(13.7, -7.4));
                float regionNoiseHi = ValueNoise(basePosition.xz * 0.052 + float2(-7.1, 4.2));
                float regionMix = saturate(regionNoiseLow * 0.78 + regionNoiseHi * 0.34);
                output.baseXZ = basePosition.xz;
                output.regionMix = (half)regionMix;

                displacement += AddGerstnerWave(_Wave1, _Wave1Motion, basePosition, _Time.y, regionMix, tangent, binormal);
                displacement += AddGerstnerWave(_Wave2, _Wave2Motion, basePosition, _Time.y + 1.9, regionMix, tangent, binormal);
                if (distanceToCamera < _MidDetailDistance)
                    displacement += AddGerstnerWave(_Wave3, _Wave3Motion, basePosition, _Time.y + 4.2, regionMix, tangent, binormal);
                if (distanceToCamera < _NearDetailDistance)
                    displacement += AddGerstnerWave(_Wave4, _Wave4Motion, basePosition, _Time.y + 2.7, regionMix, tangent, binormal);

                output.positionWS = basePosition + displacement;
                output.normalWS = normalize(cross(binormal, tangent));
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.screenPosition = ComputeScreenPos(output.positionCS);
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float distanceToCamera = distance(input.positionWS.xz, _WorldSpaceCameraPos.xz);
                float nearDetailFade = 1.0 - smoothstep(_NearDetailDistance * 0.68, _NearDetailDistance, distanceToCamera);
                float mediumDetailFade = 1.0 - smoothstep(_MidDetailDistance * 0.72, _MidDetailDistance, distanceToCamera);
                float2 p = input.positionWS.xz;

                // Three procedural scales affect the surface only near the camera, avoiding
                // repeated normal maps and distant shimmer at the horizon.
                float largeNoise = ValueNoise(p * 0.035 + _Time.y * float2(0.022, -0.014));
                float mediumNoise = ValueNoise(p * 0.115 + _Time.y * float2(-0.065, 0.041));
                float rippleNoise = ValueNoise(p * 0.58 + _Time.y * float2(0.22, 0.16));
                float detailSlope = (largeNoise - 0.5) * _LargeDetailStrength * mediumDetailFade
                                  + (mediumNoise - 0.5) * _MediumDetailStrength * nearDetailFade
                                  + (rippleNoise - 0.5) * _RippleStrength * nearDetailFade;
                half3 normalWS = normalize(input.normalWS + half3(-detailSlope, 0, detailSlope * 0.72));
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));

                float2 screenUv = input.screenPosition.xy / input.screenPosition.w;
                float sceneRawDepth = SampleSceneDepth(screenUv);
                float sceneEyeDepth = LinearEyeDepth(sceneRawDepth, _ZBufferParams);
                float waterEyeDepth = LinearEyeDepth(input.positionCS.z / input.positionCS.w, _ZBufferParams);
                float waterThickness = max(0.0, sceneEyeDepth - waterEyeDepth);
                float shallowToMid = saturate(waterThickness / max(_DepthFadeDistance, 0.01));
                float midToDeep = saturate(waterThickness * _AbsorptionStrength / max(_DepthFadeDistance, 0.01));

                half3 bodyColor = lerp(_ShallowColor.rgb, _MidColor.rgb, shallowToMid);
                bodyColor = lerp(bodyColor, _DeepColor.rgb, midToDeep);
                float brineNoise = ValueNoise(p * _BrineNoiseScale + _Time.y * float2(0.028, -0.018) * _BrineFlowSpeed);
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
                half3 coldSkyFallback = half3(0.025h, 0.065h, 0.085h);
                water = lerp(water, max(probeReflection, coldSkyFallback * 0.46h), saturate(fresnel * _ReflectionStrength));

                float glintNoise = ValueNoise(p * 0.31 + _Time.y * float2(0.16, -0.11));
                half glintMask = smoothstep(_SunGlitterThreshold, 1.0h, glintNoise);
                water += half3(0.82h, 0.91h, 0.95h) * sun.color.rgb * sunGlint * (0.20h + glintMask * _SunGlitterStrength);

                float foamNoise = ValueNoise(p * _FoamNoiseScale + _Time.y * float2(0.15, -0.10) * _FoamSpeed);
                half crestFoam = CrestAt(input.baseXZ, _Time.y, input.regionMix) * smoothstep(0.46h, 0.78h, foamNoise) * nearDetailFade;
                // This is evaluated from the opaque island depth behind each water
                // pixel, so breakers follow the actual coast rather than an island radius.
                half shoreFoam = (1.0h - smoothstep(0.02h, _FoamWidth, waterThickness)) * smoothstep(0.36h, 0.76h, foamNoise);
                half foam = max(crestFoam * 0.34h, shoreFoam) * _FoamStrength;
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
