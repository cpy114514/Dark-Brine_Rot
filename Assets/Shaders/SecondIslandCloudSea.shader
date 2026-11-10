Shader "Dark Brine/Second Island/Cloud Sea"
{
    Properties
    {
        _CloudTop ("Sunlit cloud", Color) = (1, .90, .84, 1)
        _CloudShadow ("Cloud shadow", Color) = (.56, .53, .72, 1)
        _Horizon ("Distant lavender haze", Color) = (.75, .65, .81, 1)
        _Density ("Density", Range(.004,.07)) = .025
        [NoScaleOffset] _BillowField ("Periodic cloud field", 2D) = "gray" {}
        _Wind ("Wind in metres per second", Vector) = (3, 0, 1.2, 0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-25" "RenderType"="Transparent" }
        Pass
        {
            Name "Cloud Sea"
            ZWrite Off ZTest Always Cull Front
            Blend One OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/Shaders/EnvironmentVisualStyle.hlsl"
            #pragma multi_compile_fragment _ VAN_GOGH_SURFACE
            #if defined(VAN_GOGH_SURFACE)
            #include "Assets/Shaders/VanGoghPigment.hlsl"
            #endif

            CBUFFER_START(UnityPerMaterial)
            half4 _CloudTop, _CloudShadow, _Horizon;
            float4 _Wind;
            float _Density;
            CBUFFER_END
            TEXTURE2D(_BillowField);
            SAMPLER(sampler_BillowField);

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                return output;
            }

            float Billows(float2 p)
            {
                p = float2(dot(p, float2(.94, .342)), dot(p, float2(-.342, .94)));
                return SAMPLE_TEXTURE2D_LOD(_BillowField, sampler_BillowField, p / 5500, 0).r;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float2 uv = GetNormalizedScreenSpaceUV(input.positionCS);
                float3 origin = GetCameraPositionWS();
                float3 ray = normalize(input.positionWS - origin);
                float3 originOS = TransformWorldToObject(origin);
                float3 rayOS = TransformWorldToObjectDir(ray, false);
                float3 reciprocal = rcp(rayOS + sign(rayOS + 1e-12) * 1e-9);
                float3 a = (-.5 - originOS) * reciprocal;
                float3 b = ( .5 - originOS) * reciprocal;
                float3 nearHit = min(a, b);
                float3 farHit = max(a, b);
                float enter = max(0, max(nearHit.x, max(nearHit.y, nearHit.z)));
                float leave = min(farHit.x, min(farHit.y, farHit.z));
                float sceneEye = LinearEyeDepth(SampleSceneDepth(uv), _ZBufferParams);
                float forwardDot = max(.001, -TransformWorldToViewDir(ray).z);
                leave = min(leave, min(sceneEye / forwardDot - .45, 6700));
                if (leave <= enter) return 0;

                float3 worldUp = TransformObjectToWorld(float3(0, .5, 0));
                float3 worldDown = TransformObjectToWorld(float3(0, -.5, 0));
                float thickness = worldUp.y - worldDown.y;
                float3 mainLight = GetMainLight().direction;
                float2 drift = _Wind.xz * _Time.y;
                float daylight = saturate(GetMainLight().color.g * .8 + .15);
                const int steps = 24;
                float stepSize = min((leave - enter) / steps, 31);
                float broadDepth = clamp((worldUp.y - thickness * .38 - origin.y) / min(-.001, ray.y), enter, leave);
                float2 broadXZ = origin.xz + ray.xz * broadDepth - drift;
                float broadShape = Billows(broadXZ);
                float broadSlope = broadShape - Billows(broadXZ + mainLight.xz * 55);
                half3 distant = lerp(_CloudShadow.rgb, _CloudTop.rgb, saturate(.55 + broadSlope * 5.2));
                distant *= lerp(.46, 1, daylight);
                distant = lerp(distant, _Horizon.rgb * lerp(.46, 1, daylight), smoothstep(950, 5400, broadDepth) * .95);
                float distantOpacity = smoothstep(5, 135, leave - enter) * smoothstep(.12, .33, broadShape);
                float4 light = 0;
                float closeWeight = 1 - smoothstep(440, 1050, enter);
                [loop] for (int i = 0; i < steps && closeWeight > .001; i++)
                {
                    // A fixed midpoint avoids temporal dithering on slow camera pans.
                    float distance = enter + (i + .5) * stepSize;
                    float3 p = origin + ray * distance;
                    float h = (p.y - worldDown.y) / thickness;
                    float2 flow = p.xz - drift + float2(h * 24, -h * 37);
                    float form = Billows(flow);
                    float crown = .24 + .79 * smoothstep(.21, .84, form);
                    float bottom = smoothstep(0, .18, h);
                    float density = bottom * smoothstep(-.10, .105, crown - h) * smoothstep(.20, .40, form);
                    if (density < .004) continue;
                    float neighbour = Billows(flow + mainLight.xz * 36);
                    float relief = saturate(.48 + (form - neighbour) * 3.6 + (h - crown) * 1.5);
                    half3 colour = lerp(_CloudShadow.rgb, _CloudTop.rgb, relief) * lerp(.45, 1, daylight);
                    colour = lerp(colour, _Horizon.rgb * lerp(.46, 1, daylight), smoothstep(800, 5900, distance) * .85);
                    float alpha = 1 - exp(-density * _Density * stepSize);
                    float covered = (1 - light.a) * alpha;
                    light += float4(colour * covered, covered);
                    if (light.a > .992) break;
                }
                float farCoverage = (1 - light.a * closeWeight) * distantOpacity;
                light = light * closeWeight + float4(distant * farCoverage, farCoverage);
                float fade = (1 - smoothstep(3400, 6100, enter)) * smoothstep(0, 165, leave - enter);
                if (light.a > .001)
                {
                    half3 colour = StyleEnvironmentColor(light.rgb / light.a, uv, 0);
                    #if defined(VAN_GOGH_SURFACE)
                    float3 firstContact = origin + ray * enter;
                    colour = VGSurface(colour, (firstContact.xz - drift) / 38, .28, 4);
                    #endif
                    light.rgb = colour * light.a;
                }
                return light * fade;
            }
            ENDHLSL
        }
    }
}
