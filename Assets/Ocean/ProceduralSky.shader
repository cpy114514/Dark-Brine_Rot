Shader "DarkBrine/Procedural Sky"
{
    Properties
    {
        _HorizonColor ("Horizon colour", Color) = (0.50, 0.69, 0.77, 1)
        _ZenithColor ("Zenith colour", Color) = (0.055, 0.20, 0.38, 1)
        _CloudColor ("Sunlit cloud colour", Color) = (0.96, 0.98, 1, 1)
        _SunColor ("Sun halo colour", Color) = (1, 0.42, 0.10, 1)
        _CloudCoverage ("Cloud coverage", Range(0, 1)) = 0.58
        _CloudStrength ("Cloud strength", Range(0, 1)) = 0.72
        _CloudDetail ("Cloud detail", Range(1, 5)) = 4
        _CloudSpeed ("Cloud speed", Range(0, 1)) = 0.015
        _SunGlow ("Sunlight intensity", Range(0, 1)) = 0.72
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Background" "RenderType"="Background" }
        Pass
        {
            Name "ProceduralSky"
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

            float ValueNoise(float2 p)
            {
                float2 cell = floor(p);
                float2 local = frac(p);
                float2 smooth = local * local * (3.0 - 2.0 * local);
                return lerp(lerp(Hash21(cell), Hash21(cell + float2(1.0, 0.0)), smooth.x),
                            lerp(Hash21(cell + float2(0.0, 1.0)), Hash21(cell + float2(1.0, 1.0)), smooth.x), smooth.y);
            }

            float CloudFbm(float2 p, int octaves)
            {
                float total = 0.0;
                float amplitude = 0.52;
                float2x2 rotation = float2x2(0.80, -0.60, 0.60, 0.80);
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    if (i >= octaves) break;
                    total += ValueNoise(p) * amplitude;
                    p = mul(rotation, p) * 2.02 + float2(11.7, -7.3);
                    amplitude *= 0.5;
                }
                return total / 1.0075;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                // Object-space direction works both on the enclosing scene sphere and when
                // Unity renders this material as the global Skybox.
                output.directionOS = normalize(input.positionOS.xyz);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 viewDirection = normalize(input.directionOS);
                half skyHeight = saturate(viewDirection.y * 0.70h + 0.30h);
                half3 sky = lerp(_HorizonColor.rgb, _ZenithColor.rgb, pow(skyHeight, 0.68h));

                // Project a moving, layered noise field on a very high virtual cloud deck.
                // It wraps the sky dome itself, rather than using billboard cards, so no flat
                // cloud geometry can be exposed while flying upward.
                float deckProjection = max(viewDirection.y + 0.32, 0.36);
                float2 cloudPosition = viewDirection.xz / deckProjection;
                // Three cloud decks move independently. Their different directions and scales
                // keep the sky from repeating as a single scrolling noise sheet.
                float2 lowDrift = _Time.y * _CloudSpeed * float2(0.22, -0.13);
                float2 midDrift = _Time.y * _CloudSpeed * float2(-0.14, 0.18);
                float2 highDrift = _Time.y * _CloudSpeed * float2(0.49, 0.09);
                int detail = (int)_CloudDetail;
                float broad = CloudFbm(cloudPosition * 0.66 + lowDrift, detail);
                float puffs = CloudFbm(cloudPosition * 1.85 - midDrift * 1.6 + float2(9.4, -4.6), min(detail, 3));
                float broken = CloudFbm(cloudPosition * 3.65 + midDrift * 2.2 + float2(-3.6, 12.8), min(detail, 2));
                float cirrus = CloudFbm(cloudPosition * 6.2 + highDrift + float2(21.1, 5.9), 2);
                float density = broad * 0.54 + puffs * 0.34 + broken * 0.12;

                // Bias the threshold below the FBM midpoint: the cloud deck has clear gaps,
                // but the individual banks remain immediately visible from normal flight angles.
                // Keep a substantial amount of blue between cloud banks even at a "cloudy"
                // setting.  This exposes the three shapes instead of flattening them into fog.
                float threshold = lerp(0.78, 0.38, _CloudCoverage);
                half cloudMask = smoothstep(threshold - 0.12, threshold + 0.10, density);
                half cloudCore = smoothstep(threshold + 0.01, threshold + 0.25, density);
                // Wispy high-altitude cloud strokes fill only the otherwise empty gaps.
                half cirrusMask = smoothstep(0.64, 0.84, cirrus) * (1.0h - cloudMask) * 0.34h;
                cloudMask = saturate(cloudMask + cirrusMask);
                // Fade only at the geometric horizon. This keeps clouds above the player,
                // while retaining distant broken banks instead of a grey horizontal wall.
                // The cloud decks sit high above the sea: retain a clear horizon band while
                // still allowing the upper sky to carry moving cloud shapes.
                cloudMask *= smoothstep(0.10, 0.28, viewDirection.y);

                float3 sunDirection = normalize(_SunDirection.xyz);
                half forwardLight = saturate(dot(viewDirection, sunDirection) * 0.5h + 0.5h);
                half3 cloudShade = half3(0.42h, 0.54h, 0.63h);
                half3 cloudLit = lerp(cloudShade, _CloudColor.rgb, cloudCore * 0.88h + 0.12h);
                half sunHalo = pow(forwardLight, 12.0h);
                half silverLining = pow(forwardLight, 5.0h) * (1.0h - cloudCore);
                cloudLit += _SunColor.rgb * (silverLining * cloudMask * 0.30h + sunHalo * cloudMask * 0.10h) * _SunGlow;

                // When the directional sun is near the horizon, light travels through more
                // atmosphere. Warm orange/red scattering only colours clouds that face it;
                // at daytime elevations this factor falls to zero automatically.
                half sunrise = smoothstep(0.015h, 0.12h, sunDirection.y) * (1.0h - smoothstep(0.18h, 0.52h, sunDirection.y));
                half sunsetGlow = sunrise * (pow(forwardLight, 2.4h) * 0.82h + (1.0h - cloudCore) * 0.18h) * cloudMask;
                half3 fireCloud = half3(1.0h, 0.17h, 0.035h);
                cloudLit = lerp(cloudLit, fireCloud, sunsetGlow * 0.74h * _SunGlow);
                sky = lerp(sky, cloudLit, cloudMask * _CloudStrength);
                // The halo is deliberately broad and soft: it is atmospheric sunlight, not a
                // second sun disc, so it remains compatible with the single scene sun object.
                sky += _SunColor.rgb * sunHalo * (1.0h - cloudMask * 0.72h) * (0.23h + sunrise * 0.19h) * _SunGlow;

                return half4(sky, 1.0h);
            }
            ENDHLSL
        }
    }
}
