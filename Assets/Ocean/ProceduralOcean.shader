Shader "DarkBrine/Procedural Ocean"
{
    Properties
    {
        _DeepColor ("Deep water", Color) = (0.004, 0.032, 0.060, 1)
        _ShallowColor ("Surface water", Color) = (0.018, 0.135, 0.190, 1)
        _CrestColor ("Foam colour", Color) = (0.78, 0.93, 0.92, 1)
        _WaveHeight ("Wave height", Range(0, 2)) = 0.78
        _WaveSpeed ("Wave speed", Range(0, 3)) = 0.82
        _Smoothness ("Water smoothness", Range(0, 1)) = 0.88
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
                half _WaveSpeed;
                half _Smoothness;
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
            };

            // A Gerstner wave displaces points sideways as well as vertically. This creates
            // a rolling crest rather than the up/down look of a simple sine surface.
            float3 AddGerstnerWave(float4 wave, float3 samplePosition, float time, inout float3 tangent, inout float3 binormal)
            {
                float2 direction = normalize(wave.xy);
                float steepness = wave.z * _WaveHeight;
                float waveNumber = TWO_PI / wave.w;
                float phase = waveNumber * (dot(direction, samplePosition.xz) - sqrt(9.81 / waveNumber) * time * _WaveSpeed);
                float sine = sin(phase);
                float cosine = cos(phase);
                float amplitude = steepness / waveNumber;

                tangent += float3(-direction.x * direction.x * steepness * sine,
                                   direction.x * steepness * cosine,
                                  -direction.x * direction.y * steepness * sine);
                binormal += float3(-direction.x * direction.y * steepness * sine,
                                    direction.y * steepness * cosine,
                                   -direction.y * direction.y * steepness * sine);
                return float3(direction.x * amplitude * cosine, amplitude * sine, direction.y * amplitude * cosine);
            }

            // Per-pixel crest detection keeps foam narrow and smooth instead of turning it
            // into large facets based on the underlying mesh triangles.
            float CrestAt(float2 samplePosition, float time)
            {
                float2 d0 = normalize(float2(0.82, 0.57));
                float2 d1 = normalize(float2(-0.38, 0.93));
                float2 d2 = normalize(float2(0.96, -0.29));
                float2 d3 = normalize(float2(-0.72, -0.69));
                float a = sin((dot(d0, samplePosition) - sqrt(9.81 / (TWO_PI / 180.0)) * time * _WaveSpeed) * (TWO_PI / 180.0)) * 0.54;
                float b = sin((dot(d1, samplePosition) - sqrt(9.81 / (TWO_PI / 92.0))  * time * _WaveSpeed) * (TWO_PI / 92.0) + 1.9) * 0.27;
                float c = sin((dot(d2, samplePosition) - sqrt(9.81 / (TWO_PI / 58.0))  * time * _WaveSpeed) * (TWO_PI / 58.0) + 4.2) * 0.13;
                float d = sin((dot(d3, samplePosition) - sqrt(9.81 / (TWO_PI / 35.0))  * time * _WaveSpeed) * (TWO_PI / 35.0) + 2.7) * 0.06;
                return smoothstep(0.76, 0.94, a + b + c + d);
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 basePosition = TransformObjectToWorld(input.positionOS.xyz);
                float distanceToCamera = distance(basePosition.xz, _WorldSpaceCameraPos.xz);
                float3 tangent = float3(1.0, 0.0, 0.0);
                float3 binormal = float3(0.0, 0.0, 1.0);
                float3 displacement = 0.0;

                displacement += AddGerstnerWave(float4(0.82, 0.57, 0.15, 180.0), basePosition, _Time.y, tangent, binormal);
                displacement += AddGerstnerWave(float4(-0.38, 0.93, 0.10, 92.0), basePosition, _Time.y + 1.9, tangent, binormal);
                if (distanceToCamera < _MidDetailDistance)
                    displacement += AddGerstnerWave(float4(0.96, -0.29, 0.06, 58.0), basePosition, _Time.y + 4.2, tangent, binormal);
                if (distanceToCamera < _NearDetailDistance)
                    displacement += AddGerstnerWave(float4(-0.72, -0.69, 0.03, 35.0), basePosition, _Time.y + 2.7, tangent, binormal);

                output.positionWS = basePosition + displacement;
                output.normalWS = normalize(cross(binormal, tangent));
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half3 normalWS = normalize(input.normalWS);
                float distanceToCamera = distance(input.positionWS.xz, _WorldSpaceCameraPos.xz);
                half3 viewDirection = SafeNormalize(GetWorldSpaceViewDir(input.positionWS));
                Light sun = GetMainLight();
                half fresnel = pow(1.0h - saturate(dot(normalWS, viewDirection)), 4.5h);
                half facingSun = saturate(dot(normalWS, sun.direction));

                half3 water = lerp(_DeepColor.rgb, _ShallowColor.rgb, 0.22h + facingSun * 0.36h);
                half3 skyReflection = half3(0.18h, 0.34h, 0.42h);
                water = lerp(water, skyReflection, fresnel * 0.48h);

                half haze = smoothstep(_MidDetailDistance * 0.72h, _ViewDistance, distanceToCamera);
                water = lerp(water, skyReflection, haze * 0.64h);
                return half4(water, 1.0h);
            }
            ENDHLSL
        }
    }
}
