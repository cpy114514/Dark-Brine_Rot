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
        _CloudSpeed ("Wind speed", Range(0, 1)) = 0.10
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

            CBUFFER_START(UnityPerMaterial)
                half4 _HorizonColor;
                half4 _ZenithColor;
                half4 _CloudColor;
                half4 _SunColor;
                half _CloudCoverage;
                half _CloudStrength;
                half _CloudDetail;
                half _CloudSpeed;
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
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    if (i >= octaves) break;
                    total += ValueNoise(p) * amplitude;
                    p = p.zxy * 2.03 + float3(19.1, -7.7, 11.3);
                    amplitude *= 0.5;
                }
                return total / 1.06875;
            }

            float CloudDensity(float3 position, float cloudBase, float cloudThickness)
            {
                float2 wind = _Time.y * _CloudSpeed * float2(95.0, -58.0);
                float3 samplePosition = position;
                samplePosition.xz += wind;
                // Use broader noise at long range. Fine 600 m features cannot be
                // resolved by a horizon ray that travels tens of kilometres.
                float distanceLod = saturate((distance(position, _WorldSpaceCameraPos) - 4500.0) / 15000.0);
                // Each horizontal column gets its own floor and thickness so the
                // volume reads as stacked tiers instead of a single flat slab.
                float columnShape = Fbm(samplePosition * lerp(0.0008, 0.00015, distanceLod) + float3(7.0, 0.0, -3.0), 3);
                float localBase = cloudBase + (columnShape - 0.5) * 420.0;
                float localThickness = cloudThickness * lerp(0.30, 1.55, saturate(columnShape * 1.15));

                float heightFraction = (position.y - localBase) / localThickness;
                float verticalProfile = smoothstep(0.02, 0.16, heightFraction) * (1.0 - smoothstep(0.64, 1.0, heightFraction));
                if (verticalProfile <= 0.0)
                    return 0.0;

                float macro = Fbm(samplePosition * lerp(0.00165, 0.00018, distanceLod), 4);
                float erosion = Fbm(samplePosition * lerp(0.0052, 0.00038, distanceLod) + float3(31.0, 7.0, -12.0), 3);
                float wisps = Fbm(samplePosition * lerp(0.012, 0.0007, distanceLod) + float3(-13.0, 41.0, 9.0), 2);
                float threshold = lerp(0.74, 0.31, _CloudCoverage);
                float shape = macro - threshold + (erosion - 0.50) * 0.30 + (wisps - 0.50) * 0.10;
                return saturate(shape * 3.25) * verticalProfile;
            }

            float ShadowDensity(float3 position, float cloudBase, float cloudThickness)
            {
                float distanceLod = saturate((distance(position, _WorldSpaceCameraPos) - 4500.0) / 15000.0);
                float2 wind = _Time.y * _CloudSpeed * float2(95.0, -58.0);
                position.xz += wind;
                // Lighting must use the same columnShape as the view march, otherwise
                // shadows would appear under columns that have already lifted their floor.
                float columnShape = Fbm(position * lerp(0.0008, 0.00015, distanceLod) + float3(7.0, 0.0, -3.0), 3);
                float localBase = cloudBase + (columnShape - 0.5) * 420.0;
                float localThickness = cloudThickness * lerp(0.30, 1.55, saturate(columnShape * 1.15));

                float heightFraction = (position.y - localBase) / localThickness;
                float verticalProfile = smoothstep(0.02, 0.16, heightFraction)
                    * (1.0 - smoothstep(0.64, 1.0, heightFraction));
                // Lighting only needs the broad cloud silhouette. The view march retains
                // all fine erosion and wisps, avoiding three full density evaluations here.
                float macro = Fbm(position * lerp(0.00165, 0.00018, distanceLod), 2) * 1.29545;
                float threshold = lerp(0.74, 0.31, _CloudCoverage);
                return saturate((macro - threshold) * 3.25) * verticalProfile;
            }

            float SampleSunLight(float3 position, float3 sunDirection, float cloudBase, float cloudThickness)
            {
                float shadow = ShadowDensity(position + sunDirection * 110.0, cloudBase, cloudThickness);
                shadow += ShadowDensity(position + sunDirection * 285.0, cloudBase, cloudThickness) * 0.7;
                return exp(-shadow * 2.1);
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
                float cloudBase = rayOrigin.y + 650.0;
                float cloudThickness = 460.0;
                float cloudTop = cloudBase + cloudThickness;
                float3 cloudLight = 0.0;

                // Near the horizon, a cloud ray travels tens of kilometres and
                // sparse volume samples alias into white speckles. The distant
                // angular cloud layer below covers that range instead.
                if (rayDirection.y > 0.14)
                {
                    float entry = max(0.0, (cloudBase - rayOrigin.y) / rayDirection.y);
                    float exit = min(24000.0, (cloudTop - rayOrigin.y) / rayDirection.y);
                    if (exit > entry)
                    {
                        int steps = _CloudDetail < 3.0 ? 8 : (_CloudDetail < 5.0 ? 14 : 20);
                        if (entry > 3500.0) steps = max(10, steps * 3 / 4);
                        else if (entry > 1600.0) steps = max(6, steps * 3 / 4);
                        float segmentLength = (exit - entry) / steps;
                        float jitter = Hash31(rayDirection * 127.7) - 0.5;
                        float jitterStrength = 0.65 * (1.0 - saturate((entry - 4000.0) / 8000.0));
                        float transmittance = 1.0;
                        [loop]
                        for (int i = 0; i < 20; i++)
                        {
                            if (i >= steps || transmittance < 0.015) break;
                            float travel = entry + (i + 0.5 + jitter * jitterStrength) * segmentLength;
                            float3 samplePosition = rayOrigin + rayDirection * travel;
                            float density = CloudDensity(samplePosition, cloudBase, cloudThickness);
                            if (density > 0.001)
                            {
                                float lightThroughCloud = SampleSunLight(samplePosition, sunDirection, cloudBase, cloudThickness);
                                float phase = 0.72 + pow(sunFacing, 5.0) * 0.28;
                                float3 nightLight = float3(0.13, 0.19, 0.34);
                                float3 sunlitCloud = lerp(nightLight, _CloudColor.rgb * _SunColor.rgb * 1.45,
                                    saturate(lightThroughCloud * (0.25 + _Daylight * 0.75)));
                                float3 shaded = lerp(nightLight, sunlitCloud, 0.35 + _Daylight * 0.65);
                                float opacity = density * segmentLength * 0.0034;
                                cloudLight += transmittance * shaded * opacity * phase;
                                transmittance *= exp(-opacity * 1.35);
                            }
                        }
                        cloudAlpha = saturate(1.0 - transmittance);
                    }
                }

                float3 cloudColor = cloudLight / max(cloudAlpha, 0.001);
                float horizonMix = 1.0 - smoothstep(0.08, 0.22, rayDirection.y);
                if (horizonMix > 0.0)
                {
                    float2 farWind = _Time.y * _CloudSpeed * float2(0.021, -0.014);
                    float2 farPosition = rayDirection.xz * 13.0 + farWind;
                    float farCloudShape = 0.5
                        + 0.31 * sin(farPosition.x * 0.81 + farPosition.y * 0.43)
                        + 0.19 * sin(farPosition.x * 1.63 - farPosition.y * 0.92 + 1.7);
                    float farCloud = saturate(lerp(0.015, 0.70, _CloudCoverage)
                        + (farCloudShape - 0.5) * 0.22);
                    cloudAlpha = lerp(cloudAlpha, farCloud, horizonMix);
                    cloudColor = lerp(cloudColor, float3(0.77, 0.85, 0.92), horizonMix);
                }

                sky = lerp(sky, cloudColor, cloudAlpha * _CloudStrength);
                sky *= 1.0 - cloudAlpha * saturate(_NightFactor) * 0.35;
                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
}
