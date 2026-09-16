Shader "DarkBrine/Procedural Sky"
{
    Properties
    {
        _HorizonColor ("Horizon colour", Color) = (0.58, 0.74, 0.80, 1)
        _ZenithColor ("Zenith colour", Color) = (0.09, 0.28, 0.48, 1)
        _CloudColor ("Cloud highlight", Color) = (0.94, 0.97, 0.98, 1)
        _SunColor ("Sunlight colour", Color) = (1.0, 0.62, 0.32, 1)
        _CloudCoverage ("Weather coverage", Range(0, 1)) = 0.55
        _CloudStrength ("Cloud strength", Range(0, 1)) = 0.72
        _CloudDetail ("Raymarch quality", Range(1, 5)) = 4
        _CloudSpeed ("Wind speed", Range(0, 1)) = 0.10
        _SunGlow ("Sunlight intensity", Range(0, 1)) = 0.72
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
                float4 _SunDirection;
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
                float heightFraction = (position.y - cloudBase) / cloudThickness;
                float verticalProfile = smoothstep(0.02, 0.16, heightFraction) * (1.0 - smoothstep(0.64, 1.0, heightFraction));
                if (verticalProfile <= 0.0)
                    return 0.0;

                float2 wind = _Time.y * _CloudSpeed * float2(95.0, -58.0);
                float3 samplePosition = position;
                samplePosition.xz += wind;
                float macro = Fbm(samplePosition * 0.00165, 4);
                float erosion = Fbm(samplePosition * 0.0052 + float3(31.0, 7.0, -12.0), 3);
                float wisps = Fbm(samplePosition * 0.012 + float3(-13.0, 41.0, 9.0), 2);
                float threshold = lerp(0.74, 0.31, _CloudCoverage);
                float shape = macro - threshold + (erosion - 0.50) * 0.30 + (wisps - 0.50) * 0.10;
                return saturate(shape * 3.25) * verticalProfile;
            }

            float ShadowDensity(float3 position, float cloudBase, float cloudThickness)
            {
                float heightFraction = (position.y - cloudBase) / cloudThickness;
                float verticalProfile = smoothstep(0.02, 0.16, heightFraction)
                    * (1.0 - smoothstep(0.64, 1.0, heightFraction));
                float2 wind = _Time.y * _CloudSpeed * float2(95.0, -58.0);
                position.xz += wind;
                // Lighting only needs the broad cloud silhouette. The view march retains
                // all fine erosion and wisps, avoiding three full density evaluations here.
                float macro = Fbm(position * 0.00165, 2) * 1.29545;
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
                sky = lerp(sky, float3(0.47, 0.62, 0.71), horizon * 0.17);

                float3 sunDirection = normalize(_SunDirection.xyz);
                if (dot(sunDirection, sunDirection) < 0.01)
                    sunDirection = normalize(float3(0.32, 0.78, 0.49));
                float sunFacing = saturate(dot(rayDirection, sunDirection));

                // World-space, high-altitude volume: no billboards or cloud textures.
                float cloudBase = rayOrigin.y + 650.0;
                float cloudThickness = 460.0;
                float cloudTop = cloudBase + cloudThickness;
                float cloudAlpha = 0.0;
                float3 cloudLight = 0.0;

                if (rayDirection.y > 0.018)
                {
                    float entry = max(0.0, (cloudBase - rayOrigin.y) / rayDirection.y);
                    float exit = min(7200.0, (cloudTop - rayOrigin.y) / rayDirection.y);
                    if (exit > entry)
                    {
                        int steps = _CloudDetail < 3.0 ? 8 : (_CloudDetail < 5.0 ? 14 : 20);
                        if (entry > 3500.0) steps = max(4, steps / 2);
                        else if (entry > 1600.0) steps = max(6, steps * 3 / 4);
                        float segmentLength = (exit - entry) / steps;
                        float jitter = Hash31(rayDirection * 127.7) - 0.5;
                        float transmittance = 1.0;
                        [loop]
                        for (int i = 0; i < 20; i++)
                        {
                            if (i >= steps || transmittance < 0.015) break;
                            float travel = entry + (i + 0.5 + jitter * 0.65) * segmentLength;
                            float3 samplePosition = rayOrigin + rayDirection * travel;
                            float density = CloudDensity(samplePosition, cloudBase, cloudThickness);
                            if (density > 0.001)
                            {
                                float lightThroughCloud = SampleSunLight(samplePosition, sunDirection, cloudBase, cloudThickness);
                                float phase = 0.32 + pow(sunFacing, 5.0) * 0.68;
                                float3 shaded = lerp(float3(0.17, 0.24, 0.34), _CloudColor.rgb, lightThroughCloud);
                                float opacity = density * segmentLength * 0.0034;
                                cloudLight += transmittance * shaded * opacity * phase;
                                transmittance *= exp(-opacity * 1.35);
                            }
                        }
                        cloudAlpha = saturate(1.0 - transmittance);
                    }
                }

                float sunHalo = pow(sunFacing, 34.0) * _SunGlow;
                sky += _SunColor.rgb * sunHalo * (1.0 - cloudAlpha * 0.82) * 0.07;
                sky = lerp(sky, cloudLight / max(cloudAlpha, 0.001), cloudAlpha * _CloudStrength);
                return half4(sky, 1.0);
            }
            ENDHLSL
        }
    }
}
