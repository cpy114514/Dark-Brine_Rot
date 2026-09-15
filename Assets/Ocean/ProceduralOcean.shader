Shader "DarkBrine/Procedural Ocean"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (0.018, 0.13, 0.19, 1.0)
        _ShallowColor ("Wave colour", Color) = (0.035, 0.25, 0.31, 1.0)
        _CrestColor ("Crest colour", Color) = (0.52, 0.82, 0.80, 1.0)
        _WaveHeight ("Wave height", Range(0, 2)) = 1.05
        _WaveScale ("Wave scale", Range(0.05, 1.5)) = 0.30
        _WaveSpeed ("Wave speed", Range(0, 4)) = 1.0
        _Smoothness ("Smoothness", Range(0, 1)) = 0.93
        _Opacity ("Opacity", Range(0, 1)) = 0.87
        _NearDetailDistance ("Near detail distance", Float) = 65
        _MidDetailDistance ("Mid detail distance", Float) = 150
        _ViewDistance ("View distance", Float) = 260
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" "RenderType"="Opaque" }
        Pass
        {
            Name "OceanForward"
            Tags { "LightMode"="UniversalForward" }
            ZWrite On
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _CrestColor;
                half _WaveHeight;
                half _WaveScale;
                half _WaveSpeed;
                half _Smoothness;
                half _Opacity;
                float _NearDetailDistance;
                float _MidDetailDistance;
                float _ViewDistance;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                half3 normalWS : TEXCOORD1;
                half crest : TEXCOORD2;
            };

            // Coherent value noise and FBM give every part of the water a different pattern.
            // They are evaluated mathematically: this shader never samples a texture.
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
                float2 smooth = local * local * (3.0 - 2.0 * local);
                float a = Hash21(cell);
                float b = Hash21(cell + float2(1.0, 0.0));
                float c = Hash21(cell + float2(0.0, 1.0));
                float d = Hash21(cell + float2(1.0, 1.0));
                return lerp(lerp(a, b, smooth.x), lerp(c, d, smooth.x), smooth.y);
            }

            float Fbm(float2 p)
            {
                float value = 0.0;
                float amplitude = 0.5;
                float2x2 rotation = float2x2(0.8, -0.6, 0.6, 0.8);
                [unroll]
                for (int octave = 0; octave < 4; octave++)
                {
                    value += amplitude * ValueNoise(p);
                    p = mul(rotation, p) * 2.07 + 11.7;
                    amplitude *= 0.5;
                }
                return value / 0.9375;
            }

            float OceanHeight(float2 p, float t, out float crest)
            {
                float distanceToCamera = distance(p, _WorldSpaceCameraPos.xz);
                // Far water only carries two cheap swells. It is intentionally low-frequency because
                // the subsequent haze makes it read as a distant, softly blurred ocean surface.
                if (distanceToCamera > _MidDetailDistance)
                {
                    float farSwellA = sin(dot(p, normalize(float2(0.77, 0.64))) * 0.13 + t * _WaveSpeed * 0.65) * 0.52;
                    float farSwellB = sin(dot(p, normalize(float2(-0.48, 0.88))) * 0.20 + t * _WaveSpeed * 0.92) * 0.20;
                    crest = 0.0;
                    return (farSwellA + farSwellB) * _WaveHeight;
                }

                float2 slowDrift = float2(0.055, 0.026) * t * _WaveSpeed;
                float2 fastDrift = float2(-0.19, 0.13) * t * _WaveSpeed;
                float macro = Fbm(p * 0.028 + slowDrift);
                float2 warped = p + float2(
                    Fbm(p * 0.052 + slowDrift * 1.6),
                    Fbm(p * 0.052 - slowDrift * 1.2)) * 22.0;

                float longSwell = sin(dot(warped, normalize(float2(0.77, 0.64))) * 0.17 + t * _WaveSpeed * 0.75 + macro * 4.5) * 0.76;
                float crossSwell = sin(dot(warped, normalize(float2(-0.48, 0.88))) * 0.31 + t * _WaveSpeed * 1.13 - macro * 3.0) * 0.34;
                // Keep the smallest pattern broader than the mesh spacing, so irregular detail
                // remains fluid instead of breaking into visible grid facets.
                float windNoise = Fbm(warped * 0.13 + fastDrift);
                // Mid-distance water skips the highest-frequency cap noise to reduce vertex cost.
                float capNoise = distanceToCamera > _NearDetailDistance ? 0.5 : Fbm(warped * 0.27 - fastDrift * 1.7);
                float windChop = (windNoise - 0.5) * 0.34 + (capNoise - 0.5) * 0.11;

                // Broad breakers reuse the macro swell already calculated above, so larger foam
                // appears without another expensive noise layer or a foam texture.
                float broadBreaker = saturate((longSwell + crossSwell + (macro - 0.5) * 0.55 - 0.58) * 1.95);
                float fineCrest = saturate((windNoise - 0.70) * 3.0 + (longSwell + crossSwell - 0.42) * 0.75);
                crest = max(fineCrest, broadBreaker * (0.38 + windNoise * 0.62));
                return (longSwell + crossSwell + windChop) * _WaveHeight;
            }

            void OceanShape(float2 p, float t, out float height, out float2 slope, out float crest)
            {
                height = OceanHeight(p, t, crest);
                // Central differences make normals follow the irregular procedural surface.
                float distanceToCamera = distance(p, _WorldSpaceCameraPos.xz);
                float sampleStep = distanceToCamera > _MidDetailDistance ? 1.4 : distanceToCamera > _NearDetailDistance ? 0.8 : 0.35;
                float unusedCrest;
                float dx = OceanHeight(p + float2(sampleStep, 0.0), t, unusedCrest) - OceanHeight(p - float2(sampleStep, 0.0), t, unusedCrest);
                float dz = OceanHeight(p + float2(0.0, sampleStep), t, unusedCrest) - OceanHeight(p - float2(0.0, sampleStep), t, unusedCrest);
                slope = float2(dx, dz) / (2.0 * sampleStep);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 worldBase = TransformObjectToWorld(input.positionOS.xyz);
                float height;
                float2 slope;
                float crest;
                OceanShape(worldBase.xz, _Time.y, height, slope, crest);
                worldBase.y += height;
                output.positionWS = worldBase;
                output.normalWS = normalize(float3(-slope.x, 1.0, -slope.y));
                output.crest = crest;
                output.positionCS = TransformWorldToHClip(worldBase);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                float distanceToCamera = distance(input.positionWS.xz, _WorldSpaceCameraPos.xz);
                half3 viewDir = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light mainLight = GetMainLight();
                half NdotL = saturate(dot(normalWS, mainLight.direction));
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDir)), 5.0h);
                half sparkle = 0.0h;
                if (distanceToCamera < _MidDetailDistance)
                {
                    half3 halfVector = SafeNormalize(mainLight.direction + viewDir);
                    // A dense, moving glint mask prevents a broad mesh triangle from becoming
                    // one giant circular sun decal. The reflection remains a broken ribbon of
                    // light that follows the waves instead of looking like a texture stamp.
                    float glintNoise = ValueNoise(input.positionWS.xz * 3.4 + _Time.y * float2(0.75, -0.46));
                    half glintMask = smoothstep(0.46h, 0.82h, glintNoise);
                    half specular = pow(saturate(dot(normalWS, halfVector)), lerp(280.0h, 120.0h, 1.0h - _Smoothness));
                    sparkle = specular * glintMask;
                }

                half facingLight = saturate(dot(normalWS, mainLight.direction) * 0.5h + 0.5h);
                // Keep the entire water body in one blue-green family.  Distance changes clarity,
                // not the underlying hue, so the eye reads one continuous ocean.
                half3 body = lerp(_DeepColor.rgb, _ShallowColor.rgb, facingLight * 0.44h + input.crest * 0.16h + 0.20h);
                half3 horizon = half3(0.20h, 0.38h, 0.43h);
                half3 colour = lerp(body, horizon, fresnel * 0.72h);
                colour += mainLight.color * (sparkle * 1.25h + NdotL * input.crest * 0.13h);
                colour = lerp(colour, _CrestColor.rgb, input.crest * fresnel * 0.28h);
                // A long, continuous haze ramp is a cheap far-field blur: high-frequency lighting
                // disappears first, then the remaining water softly merges into the horizon.
                half haze = smoothstep(_NearDetailDistance * 0.8h, _ViewDistance, distanceToCamera);
                half3 farWater = lerp(body, horizon, 0.58h);
                colour = lerp(colour, farWater, haze * 0.72h);
                // Larger pale breakers remain readable near and mid-range, then dissolve into
                // the distance haze instead of requiring detailed geometry everywhere.
                half broadFoam = smoothstep(0.20h, 0.64h, input.crest);
                colour = lerp(colour, _CrestColor.rgb, broadFoam * (0.42h + 0.30h * (1.0h - haze)));
                return half4(colour, 1.0h);
            }
            ENDHLSL
        }
    }
}
