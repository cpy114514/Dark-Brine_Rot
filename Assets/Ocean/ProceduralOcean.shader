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
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
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
                float _ShipHullMaskEnabled;
                float4x4 _ShipHullWorldToLocal;
                float4 _ShipHullWidths[21];
                float4 _WakeHull;
                float4 _WakeDirection;
                float4 _WakeTrail[16];
                float4 _WakeTrailSettings[16];
                int _WakeCount;
                float _ShipWakeMode;
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

            float3 WindDetail(float2 p, float2 direction, float2 scale, float2 drift)
            {
                float2 crossWind = float2(-direction.y, direction.x);
                float2 uv = float2(dot(p, direction), dot(p, crossWind)) * scale + drift;
                float3 field = OceanNoiseGradient(uv);
                return float3(field.x, direction * (field.y * scale.x) + crossWind * (field.z * scale.y));
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

            half SurfingFoam(float2 p, float detail)
            {
                // Everything is painted onto the displaced ocean itself: no floating foam planes.
                float2 offset = p - _WakeHull.xy;
                if (dot(offset, offset) > 90000.0) return 0;
                float along = dot(offset, _WakeDirection.xy);
                float across = abs(dot(offset, float2(-_WakeDirection.y, _WakeDirection.x)));
                float bowProgress = saturate((_WakeHull.z - along) / max(2.0 * _WakeHull.z, 1.0));
                float hullWidth = _WakeHull.w * sqrt(bowProgress);
                float sideBand = 1.0 - smoothstep(1.0, 4.5, abs(across - hullWidth - 1.5));
                float hullGate = smoothstep(-_WakeHull.z - 4.0, -_WakeHull.z + 4.0, along) *
                    (1.0 - smoothstep(_WakeHull.z, _WakeHull.z + 5.0, along));
                half foam = sideBand * hullGate * _WakeDirection.z * (0.45 + detail * 0.55);
                // Segment-distance history follows turns and stays behind after the ship has passed.
                [loop] for (int i = 1; i < _WakeCount; i++)
                {
                    float4 a = _WakeTrail[i - 1];
                    float4 b = _WakeTrail[i];
                    float2 segment = b.xy - a.xy;
                    float t = saturate(dot(p - a.xy, segment) / max(dot(segment, segment), 0.01));
                    float age = max(0.0, _Time.y - lerp(a.z, b.z, t));
                    float fade = saturate(1.0 - age / max(_WakeDirection.w, 0.1));
                    float width = lerp(a.w, b.w, t) + age * 1.5;
                    float d = length(p - lerp(a.xy, b.xy, t));
                    float edge = 1.0 - smoothstep(1.0, 4.0, abs(d - width));
                    float center = (1.0 - smoothstep(width * 0.25, width, d)) * 0.45;
                    foam = max(foam, max(edge, center) * fade * (0.25 + detail * 0.75));
                }
                return foam;
            }

            half SailingFoam(float2 p, float3 surfaceWS, float detail, float breakup)
            {
                if (_ShipWakeMode < 0.5) return SurfingFoam(p, detail);
                p = surfaceWS.xz;
                float2 offset = p - _WakeHull.xy;
                if (dot(offset, offset) > 90000.0) return 0;
                float2 direction = _WakeDirection.xy;
                float along = dot(offset, direction);
                float signedAcross = dot(offset, float2(-direction.y, direction.x));
                float across = abs(signedAcross);
                float halfLength = max(_WakeHull.z, 1.0);
                float fromBow = halfLength - along;
                float progress = saturate(fromBow / (halfLength * 2.0));
                // Thin breaking water follows the hull shoulder. World-space
                // turbulence makes the two sides break at different places.
                float hullWidth = _WakeHull.w * 0.73 * pow(saturate(sin(progress * PI)), 0.65);
                float divergentWidth = fromBow * 0.13 + _WakeHull.w * 0.12;
                float sideWidth = max(hullWidth + 0.65, divergentWidth);
                float hullGate = smoothstep(-halfLength - 2.5, -halfLength, along)
                    * (1.0 - smoothstep(halfLength - 1.5, halfLength + 1.5, along));
                float2 foamFlow = p - normalize(_Wave1.xy) * _Time.y * 0.13;
                float patches = ValueNoise(foamFlow * 0.16 + float2(31.7, -8.4));
                float grain = ValueNoise(foamFlow * 0.83 + float2(-4.2, 13.8));
                float swirl = ValueNoise(foamFlow * 0.055 + float2(17.8, 24.3));
                float sideDistance = across - sideWidth - (patches - 0.5) * 1.9;
                float hullDistance = across - hullWidth - 0.65;
                if (_ShipHullMaskEnabled > 0.5)
                {
                    // Use the same measured hull sections as the water mask.
                    // An approximate ellipse otherwise puts fresh foam inside
                    // the ship, where its closed hull correctly hides it.
                    float3 hull = mul(_ShipHullWorldToLocal, float4(surfaceWS, 1)).xyz;
                    float section = clamp((hull.x + 50.0) / 5.0, 0.0, 19.999);
                    int index = (int)floor(section);
                    float4 widths = lerp(_ShipHullWidths[index], _ShipHullWidths[index + 1], frac(section));
                    float measuredWidth = hull.y < 0 ? widths.x * saturate((hull.y + 2.2) / 2.2) :
                        hull.y < 3 ? lerp(widths.x, widths.y, hull.y / 3) :
                        hull.y < 6 ? lerp(widths.y, widths.z, (hull.y - 3) / 3) :
                        lerp(widths.z, widths.w, saturate((hull.y - 6) / 3));
                    float unitsPerMetre = max(length(_ShipHullWorldToLocal[2].xyz), 0.001);
                    hullDistance = (abs(hull.z) - measuredWidth) / unitsPerMetre - 0.55;
                }
                hullDistance -= (patches - 0.5) * 0.8;
                float hullRim = 1.0 - smoothstep(0.2, 1.65, abs(hullDistance));
                float hullWash = (1.0 - smoothstep(0.35, 3.6, max(hullDistance, 0.0)))
                    * smoothstep(-0.4, 0.2, hullDistance);
                float rim = 1.0 - smoothstep(0.25, 1.8, abs(sideDistance));
                float wash = (1.0 - smoothstep(0.3, 4.2, max(sideDistance, 0.0)))
                    * smoothstep(-0.65, 0.25, sideDistance);
                float lace = smoothstep(0.28, 0.70, patches * 0.65 + breakup * 0.35);
                float bubbles = lerp(0.28, 1.0, smoothstep(0.26, 0.72, grain));
                float shoulder = smoothstep(0.01, 0.13, progress) * (1.0 - smoothstep(0.7, 1.0, progress));
                half foam = max((rim * lace * 0.38 + wash * lace * 0.16),
                    (hullRim * (0.18 + lace * 0.72) + hullWash * lace * 0.28) * shoulder)
                    * bubbles * hullGate * _WakeDirection.z;
                // Join the live stern to the latest fixed sample between emissions,
                // so the wake cannot detach as the ship advances or turns.
                float4 liveStern = float4(_WakeHull.xy - direction * halfLength, _Time.y,
                    halfLength * 2.0 * 0.13 + _WakeHull.w * 0.12);
                [loop] for (int i = 0; i < _WakeCount; i++)
                {
                    int previousIndex = max(0, i - 1);
                    float4 a = i == 0 ? liveStern : _WakeTrail[previousIndex];
                    float4 b = _WakeTrail[i];
                    float2 segment = b.xy - a.xy;
                    float lengthSquared = dot(segment, segment);
                    if (lengthSquared < 0.01) continue;
                    float segmentLength = sqrt(lengthSquared);
                    float alongSegment = dot(p - a.xy, segment) / segmentLength;
                    float t = saturate(alongSegment / segmentLength);
                    float age = max(0.0, _Time.y - lerp(a.z, b.z, t));
                    float life = max(_WakeDirection.w, 0.1);
                    float fade = exp(-age * 0.12) * (1.0 - smoothstep(life * 0.2, life, age));
                    float4 newestSettings = float4(direction, _WakeDirection.z, _WakeTrailSettings[0].w);
                    float4 settings = lerp(i == 0 ? newestSettings : _WakeTrailSettings[previousIndex],
                        _WakeTrailSettings[i], t);
                    float2 normal = float2(-segment.y, segment.x) / segmentLength;
                    float lateralDistance = abs(dot(p - lerp(a.xy, b.xy, t), normal));
                    float width = lerp(a.w, b.w, t) + age * settings.w;
                    float bandWidth = lerp(2.0, 7.5, saturate(age / life));
                    float meander = (swirl - 0.5) * lerp(1.5, 5.5, saturate(age / life))
                        + (patches - 0.5) * 2.0;
                    float ribbon = 1.0 - smoothstep(bandWidth * 0.12, bandWidth,
                        abs(lateralDistance - width - meander));
                    // Longitudinal gates make open ribbons; clamped point distance
                    // would add a visible semicircle at every trail endpoint.
                    float endGate = smoothstep(-2.0, 0.0, alongSegment)
                        * (1.0 - smoothstep(segmentLength, segmentLength + 2.0, alongSegment));
                    float innerWash = (1.0 - smoothstep(width * 0.15, width * 0.95, lateralDistance))
                        * exp(-age * 0.45) * 0.22 * smoothstep(0.40, 0.75, patches);
                    // Older sheets open into patches and fine bubbles, rather
                    // than keeping two equally opaque parallel stripes.
                    float dissolvedLace = smoothstep(lerp(0.26, 0.5, saturate(age/life)), 0.78,
                        patches * 0.65 + breakup * 0.35);
                    foam = max(foam, (ribbon * dissolvedLace * bubbles + innerWash) * endGate
                        * fade * settings.z * 0.78);
                }
                return foam;
            }

            half4 frag(Varyings input) : SV_Target
            {
                if (_ShipHullMaskEnabled > 0.5)
                {
                    float3 hull = mul(_ShipHullWorldToLocal, float4(input.positionWS, 1)).xyz;
                    if (abs(hull.x) < 50 && hull.y > -2.2 && hull.y < 12)
                    {
                        float section = clamp((hull.x + 50) / 5, 0, 19.999);
                        int index = (int)floor(section);
                        float4 widths = lerp(_ShipHullWidths[index], _ShipHullWidths[index + 1], frac(section));
                        float width = hull.y < 0 ? widths.x * saturate((hull.y + 2.2) / 2.2) :
                            hull.y < 3 ? lerp(widths.x, widths.y, hull.y / 3) :
                            hull.y < 6 ? lerp(widths.y, widths.z, (hull.y - 3) / 3) :
                            lerp(widths.z, widths.w, saturate((hull.y - 6) / 3));
                        clip(abs(hull.z) - width);
                    }
                }
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
                float2 windDirection = normalize(_Wave1.xy);
                float2 crossDirection = normalize(_Wave3.xy);
                float3 largeField = WindDetail(p, windDirection, float2(0.035, 0.026),
                    oceanTime * float2(-0.075, 0.012));
                float3 mediumField = WindDetail(p, crossDirection, float2(0.115, 0.075),
                    oceanTime * float2(-0.21, 0.025));
                float3 rippleField = WindDetail(p, windDirection, float2(0.58, 0.32),
                    oceanTime * float2(-0.48, 0.06));
                float largeNoise = largeField.x, mediumNoise = mediumField.x, rippleNoise = rippleField.x;
                float pixelFootprint = max(length(ddx(p)), length(ddy(p)));
                float rippleFilter = 1.0 - smoothstep(0.35, 1.1, pixelFootprint * 0.58);
                float mediumFilter = 1.0 - smoothstep(0.35, 1.1, pixelFootprint * 0.115);
                // Actual two-axis height derivatives, not a fixed diagonal tilt.
                float2 detailSlope = largeField.yz * (6.5 * _LargeDetailStrength) * mediumDetailFade
                    + mediumField.yz * (1.8 * _MediumDetailStrength) * nearDetailFade * mediumFilter
                    + rippleField.yz * (0.2 * _RippleStrength) * nearDetailFade * rippleFilter;
                float2 capillaryA = windDirection;
                float2 capillaryB = crossDirection;
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
                half surfaceRoughness = saturate(1.0h - _Smoothness + waveCrest * 0.055h
                    + (mediumNoise - 0.5h) * 0.035h);
                half perceptualRoughness = saturate(sqrt(surfaceRoughness * surfaceRoughness
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

                // Foam drifts with the displaced surface and wind rather than
                // reading as an independently sliding texture over the waves.
                float2 foamPosition = input.positionWS.xz - displacement.xz * 0.65
                    - windDirection * oceanTime * _FoamSpeed * 1.5;
                float foamNoise = ValueNoise(foamPosition * _FoamNoiseScale);
                float foamRegion = ValueNoise(p * (_FoamNoiseScale * 0.27) + float2(9.3, -17.6)
                    + oceanTime * float2(0.008, -0.006) * _FoamSpeed);
                float foamDetail = ValueNoise(foamPosition * (_FoamNoiseScale * 2.4) + float2(-14.2, 3.8)
                    + oceanTime * float2(-0.09, 0.07) * _FoamSpeed);
                half crestCutoff = lerp(0.52h, 0.45h + (1.0h - foamRegion) * 0.18h, _FoamIrregularity);
                half crestFoam = waveCrest *
                    smoothstep(crestCutoff, crestCutoff + 0.22h, foamNoise) * mediumDetailFade *
                    (0.38h + 0.62h * smoothstep(0.23h, 0.62h, foamDetail));
                // Thin lace and broken patches leave water visible within each
                // whitecap, instead of filling the whole crest with a white band.
                half foamLace = 1.0h - smoothstep(0.07h, 0.25h, abs(foamDetail - 0.48h));
                crestFoam *= lerp(1.0h, 0.30h + foamLace * 0.70h, _FoamIrregularity);
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
                breakerFoam *= (0.60h + waveCrest * 0.40h) * (0.65h + foamLace * 0.35h);
                half foam = max(crestFoam * _WhitecapStrength, max(contactFoam, breakerFoam) * shoreMask) * _FoamStrength;
                foam = max(foam, SailingFoam(p, input.positionWS, foamDetail, foamNoise));
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
