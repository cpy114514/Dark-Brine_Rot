Shader "DarkBrine/Procedural Sky"
{
    Properties
    {
        _HorizonColor ("Horizon colour", Color) = (0.67, 0.82, 0.94, 1)
        _ZenithColor ("Zenith colour", Color) = (0.18, 0.47, 0.79, 1)
        _CloudColor ("Cloud highlight", Color) = (0.94, 0.97, 0.98, 1)
        _SunColor ("Sunlight colour", Color) = (1.0, 0.96, 0.87, 1)
        _CloudCoverage ("Weather coverage", Range(0, 1)) = 0.55
        _CloudStrength ("Cloud strength", Range(0, 1)) = 0.72
        _CloudDetail ("Raymarch quality", Range(1, 5)) = 4
        _CloudRenderDistance ("Cloud render distance (m)", Range(4000, 48000)) = 12000
        _CloudSpeed ("Wind speed", Range(0, 1)) = 0.10
        [HideInInspector] _CloudTimeOffset ("Cloud preview clock offset", Float) = 0
        _SunGlow ("Sunlight intensity", Range(0, 1)) = 0.72
        _Daylight ("Daylight", Range(0, 1)) = 1
        _NightFactor ("Night sky", Range(0, 1)) = 0
        _MoonDirection ("Moon direction", Vector) = (0, 1, 0, 0)
        _MoonIllumination ("Moon illumination", Range(0, 1)) = 0.5
    }
    SubShader
    {
        // Draw behind the scene, but after opaque objects and the depth-writing ocean.
        // The depth test then skips expensive cloud rays for covered pixels.
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-50" "RenderType"="Transparent" }
        Pass
        {
            Name "RaymarchedVolumetricClouds"
            Tags { "LightMode"="SRPDefaultUnlit" }
            Cull Front
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/EnvironmentVisualStyle.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _HorizonColor;
                half4 _ZenithColor;
                half4 _CloudColor;
                half4 _SunColor;
                half _CloudCoverage;
                half _CloudStrength;
                half _CloudDetail;
                float _CloudRenderDistance;
                half _CloudSpeed;
                float _CloudTimeOffset;
                half _SunGlow;
                half _Daylight;
                half _NightFactor;
                float4 _SunDirection;
                float4 _MoonDirection;
                half _MoonIllumination;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            float Hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float ValueNoise(float3 p)
            {
                float3 cell = floor(p);
                float3 local = frac(p);
                local = local * local * (3.0 - 2.0 * local);
                float n000 = Hash31(cell + float3(0, 0, 0));
                float n100 = Hash31(cell + float3(1, 0, 0));
                float n010 = Hash31(cell + float3(0, 1, 0));
                float n110 = Hash31(cell + float3(1, 1, 0));
                float n001 = Hash31(cell + float3(0, 0, 1));
                float n101 = Hash31(cell + float3(1, 0, 1));
                float n011 = Hash31(cell + float3(0, 1, 1));
                float n111 = Hash31(cell + float3(1, 1, 1));
                float nx00 = lerp(n000, n100, local.x);
                float nx10 = lerp(n010, n110, local.x);
                float nx01 = lerp(n001, n101, local.x);
                float nx11 = lerp(n011, n111, local.x);
                return lerp(lerp(nx00, nx10, local.y), lerp(nx01, nx11, local.y), local.z);
            }

            float Fbm(float3 p, int octaves)
            {
                float total = 0.0;
                float amplitude = 0.55;
                float weights = 0.0;
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    if (i >= octaves) break;
                    total += ValueNoise(p) * amplitude;
                    weights += amplitude;
                    p = p.zxy * 2.03 + float3(19.1, -7.7, 11.3);
                    amplitude *= 0.5;
                }
                return total / max(weights, 0.0001);
            }

            float FilteredFbm(float3 p, float footprint, int octaves)
            {
                float total = 0.0, weight = 0.0, amplitude = 0.55;
                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    if (i >= octaves) break;
                    float resolved = 1.0 - smoothstep(0.35, 0.90, footprint);
                    [branch] if (resolved > 0.001)
                        total += (ValueNoise(p) - 0.5) * amplitude * resolved;
                    weight += amplitude;
                    p = p.zxy * 2.03 + float3(19.1, -7.7, 11.3);
                    footprint *= 2.03;
                    amplitude *= 0.5;
                }
                return 0.5 + total / max(weight, 0.0001);
            }

            float3 CloudPosition(float3 position)
            {
                // One coherent air mass. Fine and broad detail share the same
                // modest wind speed instead of boiling or racing independently.
                position.xz += (_Time.y + _CloudTimeOffset) * _CloudSpeed * float2(16.0, -9.0);
                return position;
            }

            float CloudProfile(float3 position, float cloudBase, float cloudThickness, out float coverage)
            {
                // Horizontal weather columns must not change with sample height.
                float weather = Fbm(float3(position.x * 0.00045, 0.37, position.z * 0.00045), 2);
                float threshold = lerp(0.76, 0.32, saturate(_CloudCoverage));
                coverage = smoothstep(threshold - 0.10, threshold + 0.14, weather);
                float localBase = cloudBase + (weather - 0.5) * 100.0;
                float localThickness = cloudThickness * lerp(0.65, 1.35, weather);
                float h = (position.y - localBase) / localThickness;
                return smoothstep(0.0, 0.16, h) * (1.0 - smoothstep(0.55, 1.0, h));
            }

            float CloudDensity(float3 position, float cloudBase, float cloudThickness, float stepLength)
            {
                float distanceLod = smoothstep(3500.0, 10000.0, distance(position, _WorldSpaceCameraPos));
                position = CloudPosition(position);
                float coverage;
                float profile = CloudProfile(position, cloudBase, cloudThickness, coverage);
                [branch] if (profile * coverage < 0.0001) return 0.0;
                float3 p = float3(position.x, position.y - cloudBase, position.z) * 0.0018;
                float macro = FilteredFbm(p + float3(coverage * 0.55, 0.0, -coverage * 0.36), stepLength * 0.0018, 3);
                // Filter unresolved detail; do not rescale the noise field with
                // camera distance, which made the volume stretch into sheets.
                macro = lerp(macro, 0.5, distanceLod * 0.6);
                float shape = macro - lerp(0.65, 0.50, coverage);
                [branch] if (shape < -0.0501) return 0.0;
                float erosion = FilteredFbm(p * 2.2 + float3(31.0, 7.0, -12.0), stepLength * 0.00396, 2);
                shape += (erosion - 0.5) * 0.10 * (1.0 - distanceLod);
                return saturate(shape * 5.5) * profile * coverage;
            }

            float ShadowDensity(float3 position, float cloudBase, float cloudThickness)
            {
                position = CloudPosition(position);
                float coverage;
                float profile = CloudProfile(position, cloudBase, cloudThickness, coverage);
                [branch] if (profile * coverage < 0.0001) return 0.0;
                float3 p = float3(position.x, position.y - cloudBase, position.z) * 0.0018;
                float macro = Fbm(p + float3(coverage * 0.55, 0.0, -coverage * 0.36), 2);
                return saturate((macro - lerp(0.65, 0.50, coverage)) * 5.5) * profile * coverage;
            }

            float SampleSunLight(float3 position, float3 sunDirection, float cloudBase, float cloudThickness)
            {
                float shadow = ShadowDensity(position + sunDirection * 130.0, cloudBase, cloudThickness);
                shadow += ShadowDensity(position + sunDirection * 310.0, cloudBase, cloudThickness) * 0.7;
                return exp(-shadow * 3.4);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 rayOrigin = _WorldSpaceCameraPos;
                float3 rayDirection = normalize(input.positionWS - rayOrigin);
                float elevation = saturate(rayDirection.y * 0.5 + 0.5);
                float horizon = pow(saturate(1.0 - max(rayDirection.y, 0.0)), 2.2);
                float3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(elevation, 0.72));
                sky = lerp(sky, float3(0.78, 0.88, 0.96), horizon * 0.19);

                float3 sunDirection = normalize(_SunDirection.xyz);
                if (dot(sunDirection, sunDirection) < 0.01)
                    sunDirection = normalize(float3(0.32, 0.78, 0.49));
                float sunFacing = saturate(dot(rayDirection, sunDirection));
                float cloudAlpha = 0.0;

                // A soft warm scattering halo follows the actual sunset direction.
                // The procedural disk remains a separate camera-facing sun mesh.
                float mieHalo = pow(sunFacing, 11.0) * _SunGlow * 0.20;
                float aureole = pow(sunFacing, 3.4) * _SunGlow * (1.0 - _Daylight) * 0.055;
                float sunsetWash = pow(sunFacing, 1.7) * _SunGlow * (1.0 - _Daylight) * 0.07;
                sky += _SunColor.rgb * (mieHalo + aureole + sunsetWash) * (1.0 - cloudAlpha * 0.72);

                // Stable angular stars and an opposite-side moon fade in through blue hour.
                float3 starCell = floor(rayDirection * 420.0);
                float starHash = Hash31(starCell + 17.31);
                float starPoint = smoothstep(0.99855, 0.99992, starHash);
                float twinkle = 0.72 + 0.28 * sin(_Time.y * (0.45 + starHash) + starHash * 75.0);
                float starVisibility = saturate(_NightFactor) * smoothstep(0.025, 0.18, rayDirection.y);
                sky += float3(0.55, 0.70, 1.0) * starPoint * twinkle * starVisibility * 1.65;

                float3 moonDirection = normalize(_MoonDirection.xyz);
                if (dot(moonDirection, moonDirection) < 0.01)
                    moonDirection = normalize(-sunDirection);
                float moonFacing = saturate(dot(rayDirection, moonDirection));
                float moonGlow = pow(moonFacing, 38.0) * lerp(0.04, 0.11, _MoonIllumination);
                float moonAureole = pow(moonFacing, 5.5) * 0.012;
                float3 moonColor = float3(0.58, 0.73, 1.0);
                sky += moonColor * (moonGlow + moonAureole) * saturate(_NightFactor);

                // World-space, high-altitude volume: no billboards or cloud textures.
                float cloudBase = 1100.0;
                float cloudThickness = 800.0;
                // Enclose every possible local floor/top, not just the centre
                // slab: clipping shifted columns produced the old hard edges.
                float cloudBottom = cloudBase - 55.0;
                float cloudTop = cloudBase + cloudThickness * 1.35 + 55.0;
                float3 cloudLight = 0.0;

                // Extend detailed clouds toward the horizon, then blend into the
                // angular layer beyond the view distance to avoid undersampled rays.
                float cloudDistance = clamp(_CloudRenderDistance, 4000.0, 48000.0);
                float horizonStart = max(0.0, cloudBottom - rayOrigin.y) / cloudDistance;
                if (rayDirection.y > max(0.001, horizonStart))
                {
                    float entry = max(0.0, (cloudBottom - rayOrigin.y) / rayDirection.y);
                    float exit = min(cloudDistance, (cloudTop - rayOrigin.y) / rayDirection.y);
                    if (exit > entry)
                    {
                        uint steps = _CloudDetail < 3.0 ? 8u : (_CloudDetail < 5.0 ? 14u : 20u);
                        // The same coarse world buffer handles sky and geometry; fewer rays suffice here.
                        if (_PixelStyleUI > .5) steps = 8u;
                        // Keep each quality tier continuous across viewing angles.
                        // Distance-dependent step-count jumps exposed hard rings.
                        float segmentLength = (exit - entry) / steps;
                        // Smooth angular jitter breaks up sampling bands without
                        // introducing pixel-sized grain in the distant cloud layer.
                        float jitter = ValueNoise(rayDirection * 173.0) - 0.5;
                        float jitterStrength = 0.20 * (1.0 - saturate((entry - 4000.0) / 8000.0));
                        float transmittance = 1.0;
                        [loop]
                        for (uint i = 0u; i < 20u; i++)
                        {
                            if (i >= steps || transmittance < 0.015) break;
                            float travel = entry + (i + 0.5 + jitter * jitterStrength) * segmentLength;
                            float3 samplePosition = rayOrigin + rayDirection * travel;
                            float density = CloudDensity(samplePosition, cloudBase, cloudThickness, segmentLength);
                            if (density > 0.001)
                            {
                                float lightThroughCloud = SampleSunLight(samplePosition, sunDirection, cloudBase, cloudThickness);
                                float h = saturate((samplePosition.y - cloudBase) / cloudThickness);
                                float3 ambient = lerp(_HorizonColor.rgb * 0.40, _CloudColor.rgb * 0.74, 0.22 + h * 0.78);
                                float3 shaded = ambient + _CloudColor.rgb * _SunColor.rgb
                                    * (0.30 * _Daylight * lightThroughCloud) * (0.9 + pow(sunFacing, 5.0) * 0.1);
                                float rangeFade = 1.0 - smoothstep(cloudDistance * 0.65, cloudDistance, travel);
                                float opacity = 1.0 - exp(-density * rangeFade * segmentLength * 0.007);
                                // Beer-Lambert compositing: integrate absorbed energy,
                                // rather than adding a full linear step at soft edges.
                                cloudLight += transmittance * shaded * opacity;
                                transmittance *= 1.0 - opacity;
                            }
                        }
                        cloudAlpha = saturate(1.0 - transmittance);
                    }
                }

                float3 cloudColor = cloudLight / max(cloudAlpha, 0.001);
                // The distant volume fades into atmosphere without a second,
                // unrelated sinusoidal cloud pattern or a hard angular cut.
                cloudAlpha *= smoothstep(0.015, 0.065, rayDirection.y);

                if (_PixelStyleUI > .5)
                {
                    // Broad, readable pixel clusters instead of a soft photographic cloud gradient.
                    cloudAlpha = floor(cloudAlpha * 8 + .5) / 8;
                    cloudColor = floor(cloudColor * 8 + .5) / 8;
                }

                sky = lerp(sky, cloudColor, cloudAlpha * _CloudStrength);
                sky *= 1.0 - cloudAlpha * saturate(_NightFactor) * 0.35;
                sky = StyleEnvironmentColor(sky, input.positionCS.xy / _ScaledScreenParams.xy,
                    cloudAlpha * .16 * (1 - saturate(dot(cloudColor, float3(.2126, .7152, .0722)))));
                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
}
