Shader "DarkBrine/Procedural Moon"
{
    Properties
    {
        _MoonTint ("Moon tint", Color) = (0.82, 0.88, 1, 1)
        _SunDirection ("Sun direction", Vector) = (0, 1, 0, 0)
        _MoonVisibility ("Visibility", Range(0, 1)) = 0
        _MoonIllumination ("Illuminated fraction", Range(0, 1)) = 0.5
        _MoonBrightness ("Surface brightness", Range(0.5, 3)) = 1.6
        _CloudHaze ("Cloud haze", Range(0, 1)) = 0
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent+5" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.5
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _MoonTint;
                float4 _SunDirection;
                half _MoonVisibility;
                half _MoonIllumination;
                half _MoonBrightness;
                half _CloudHaze;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : TEXCOORD0;
                float3 normalOS : TEXCOORD1;
                float3 viewDirWS : TEXCOORD2;
            };

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
                float n000 = Hash31(cell);
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

            float Fbm(float3 p)
            {
                float total = 0.0;
                float amplitude = 0.55;
                [unroll]
                for (int i = 0; i < 4; i++)
                {
                    total += ValueNoise(p) * amplitude;
                    p = p.zxy * 2.03 + float3(19.1, -7.7, 11.3);
                    amplitude *= 0.5;
                }
                return total / 1.03125;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.normalOS = input.normalOS;
                output.viewDirWS = GetWorldSpaceViewDir(positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 normalOS = normalize(input.normalOS);
                float3 lightDirection = normalize(_SunDirection.xyz);
                float cosineLight = dot(normalWS, lightDirection);
                float litSide = smoothstep(-0.065, 0.12, cosineLight);

                float broadSurface = Fbm(normalOS * 5.5 + float3(4.1, 1.7, 8.3));
                float midSurface = Fbm(normalOS * 17.0 + float3(2.6, 9.4, 3.1));
                float fineSurface = ValueNoise(normalOS * 54.0 + float3(7.2, 2.3, 5.8));
                float maria = smoothstep(0.43, 0.66, broadSurface);
                float craterRims = smoothstep(0.54, 0.78, midSurface) *
                                   (1.0 - smoothstep(0.78, 0.91, midSurface));
                float surfaceValue = 0.84 + (broadSurface - 0.5) * 0.22 +
                                     (fineSurface - 0.5) * 0.08 - maria * 0.18 + craterRims * 0.07;
                float3 albedo = _MoonTint.rgb * saturate(surfaceValue);

                float earthshine = lerp(0.018, 0.045, 1.0 - _MoonIllumination);
                float3 surfaceLight = albedo * (earthshine + litSide * (0.56 + 0.72 * saturate(cosineLight)) * _MoonBrightness);
                float viewRim = pow(1.0 - saturate(dot(normalWS, normalize(input.viewDirWS))), 4.0);
                float3 rimGlow = _MoonTint.rgb * viewRim * 0.035 * _MoonBrightness;

                float cloudFade = 1.0 - saturate(_CloudHaze) * 0.32;
                float visibility = saturate(_MoonVisibility) * cloudFade;
                float alpha = visibility * lerp(0.12, 1.0, litSide);
                return half4((surfaceLight + rimGlow) * visibility, alpha);
            }
            ENDHLSL
        }
    }
}
