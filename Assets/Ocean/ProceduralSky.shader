Shader "DarkBrine/Procedural Sky"
{
    Properties
    {
        _HorizonColor ("Horizon colour", Color) = (0.58, 0.74, 0.80, 1)
        _ZenithColor ("Zenith colour", Color) = (0.09, 0.28, 0.48, 1)
        _CloudColor ("Cloud colour", Color) = (0.94, 0.97, 0.98, 1)
        _SunColor ("Sunlight colour", Color) = (1.0, 0.62, 0.32, 1)
        _CloudCoverage ("Cloud coverage", Range(0, 1)) = 0.55
        _CloudStrength ("Cloud strength", Range(0, 1)) = 0.72
        _CloudDetail ("Cloud detail", Range(1, 5)) = 4
        _CloudSpeed ("Cloud speed", Range(0, 1)) = 0.10
        _SunGlow ("Sunlight intensity", Range(0, 1)) = 0.72
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" }
        Pass
        {
            Cull Front
            ZWrite Off
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
            struct Varyings { float4 positionCS : SV_POSITION; float3 directionOS : TEXCOORD0; };

            float Hash21(float2 p)
            {
                p = frac(p * float2(127.1, 311.7));
                p += dot(p, p + 19.19);
                return frac(p.x * p.y);
            }

            float Noise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                float2 smooth = local * local * (3.0 - 2.0 * local);
                return lerp(lerp(Hash21(cell), Hash21(cell + float2(1, 0)), smooth.x),
                            lerp(Hash21(cell + float2(0, 1)), Hash21(cell + float2(1, 1)), smooth.x), smooth.y);
            }

            float Fbm(float2 p, int octaves)
            {
                float total = 0.0;
                float amplitude = 0.56;
                float2x2 rotation = float2x2(0.80, -0.60, 0.60, 0.80);
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    if (i >= octaves) break;
                    total += Noise(p) * amplitude;
                    p = mul(rotation, p) * 2.01 + float2(8.3, -5.7);
                    amplitude *= 0.5;
                }
                return total / 1.085;
            }

            float CloudField(float2 plane, float2 drift, int detail)
            {
                // Large-scale distortion stops the cloud banks from reading as a regular grid.
                float2 warp = float2(Noise(plane * 0.34 + drift * 0.22),
                                     Noise(plane * 0.34 - drift * 0.17 + 17.6)) - 0.5;
                return Fbm(plane + warp * 1.15 + drift, detail);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.directionOS = normalize(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 direction = normalize(input.directionOS);
                half elevation = saturate(direction.y);
                half horizon = pow(1.0h - elevation, 2.7h);
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(elevation, 0.58h));
                // A cool, dense horizon and a subtly darker upper atmosphere add depth even
                // when the cloud coverage slider is low.
                sky = lerp(sky, half3(0.52h, 0.68h, 0.75h), horizon * 0.14h);
                sky *= lerp(0.78h, 1.0h, pow(elevation, 0.28h));

                // A high virtual cloud deck: the mapping keeps the horizon clear and produces
                // recognisable cumulus banks plus a faster, thinner layer above them.
                float2 plane = direction.xz / max(direction.y + 0.40, 0.56);
                float2 lowDrift = _Time.y * _CloudSpeed * float2(0.09, -0.045);
                float2 middleDrift = _Time.y * _CloudSpeed * float2(-0.13, 0.065);
                float2 highDrift = _Time.y * _CloudSpeed * float2(-0.23, 0.13);
                int detail = (int)_CloudDetail;
                float cumulus = CloudField(plane * 1.42, lowDrift, detail);
                float puffs = CloudField(plane * 3.65 + float2(4.6, -8.2), -lowDrift * 1.35, min(detail, 3));
                float middleCloud = Fbm(float2(plane.x * 2.1, plane.y * 0.72) + middleDrift + float2(9.7, 2.4), min(detail, 3));
                float wisps = Fbm(float2(plane.x * 7.8, plane.y * 1.3) + highDrift + float2(-11.3, 7.4), 2);
                float filaments = Fbm(float2(plane.x * 15.0, plane.y * 0.52) + highDrift * 1.9 + float2(3.8, -14.1), 2);
                float density = cumulus * 0.54 + puffs * 0.28 + middleCloud * 0.18 + (filaments - 0.5) * 0.075;
                float threshold = lerp(0.74, 0.36, _CloudCoverage);
                half cloud = smoothstep(threshold - 0.075, threshold + 0.095, density);
                half cloudCore = smoothstep(threshold + 0.02, threshold + 0.22, density);
                half cloudEdge = smoothstep(threshold - 0.12, threshold + 0.015, density) - cloud;
                half highWisps = smoothstep(0.68, 0.86, wisps) * (1.0h - cloud) * 0.22h;
                highWisps += smoothstep(0.71, 0.87, filaments) * (1.0h - cloud) * 0.16h;
                half horizonClouds = smoothstep(0.10h, 0.35h, direction.y);
                cloud = saturate(cloud + highWisps) * horizonClouds;

                float3 sunDirection = normalize(_SunDirection.xyz);
                half sunFacing = saturate(dot(direction, sunDirection));
                half3 cloudShadow = half3(0.31h, 0.41h, 0.52h);
                half3 cloudColour = lerp(cloudShadow, _CloudColor.rgb, cloudCore * 0.86h + 0.12h);
                half silverLining = cloudEdge * pow(sunFacing, 3.2h) * _SunGlow;
                cloudColour += _SunColor.rgb * (silverLining * 0.45h + pow(sunFacing, 7.0h) * (1.0h - cloudCore) * 0.12h * _SunGlow);
                sky = lerp(sky, cloudColour, cloud * _CloudStrength);

                // Soft atmosphere only; the scene's physical sun remains the sole sun disc.
                sky += _SunColor.rgb * pow(sunFacing, 42.0h) * (1.0h - cloud * 0.80h) * 0.045h * _SunGlow;
                return half4(sky, 1.0h);
            }
            ENDHLSL
        }
    }
}
