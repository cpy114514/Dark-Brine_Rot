Shader "Mavis/SandFootprint"
{
    Properties
    {
        _Color ("Color", Color) = (0.33, 0.22, 0.11, 0.62)
        _Mirror ("Foot side", Float) = 1
        _Seed ("Sand variation", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }
        Pass
        {
            Name "Footprint"
            Tags { "LightMode" = "UniversalForward" }
            // Modulate the existing sand, preserving its grain and lighting.
            Blend DstColor Zero
            ZWrite Off
            Cull Off
            Offset -1, -1
            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float _Mirror;
                float _Seed;
            CBUFFER_END
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
            };
            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv = IN.uv;
                OUT.color = IN.color;
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(float3(0, 1, 0));
                return OUT;
            }
            float Ellipse(float2 p, float2 center, float2 radius)
            {
                return length((p - center) / radius) - 1;
            }
            float FootShape(float2 p)
            {
                p.x *= _Mirror;
                float body = min(Ellipse(p, float2(0, -.57), float2(.48, .32)),
                    Ellipse(p, float2(.08, -.13), float2(.43, .48)));
                body = min(body, Ellipse(p, float2(.01, .31), float2(.64, .33)));
                body = max(body, -Ellipse(p, float2(-.62, -.14), float2(.36, .31)));
                body = min(body, Ellipse(p, float2(-.40, .71), float2(.22, .22)));
                body = min(body, Ellipse(p, float2(-.04, .77), float2(.16, .17)));
                body = min(body, Ellipse(p, float2(.24, .69), float2(.14, .16)));
                body = min(body, Ellipse(p, float2(.46, .56), float2(.12, .14)));
                return min(body, Ellipse(p, float2(.62, .41), float2(.10, .12)));
            }
            half4 frag(Varyings IN) : SV_Target
            {
                float2 p = IN.uv * 2.0 - 1.0;
                float grain = frac(sin(dot(floor(IN.positionWS.xz * 190), float2(127.1, 311.7)) + _Seed * 19) * 43758.5453);
                float shape = FootShape(p) + (grain - .5) * .025;
                float feather = max(fwidth(shape) * 1.1, .035);
                float coverage = 1 - smoothstep(-feather, feather, shape);
                float edgeDistance = (shape + .035) / .12;
                float rim = exp(-edgeDistance * edgeDistance);
                float basin = 1 - smoothstep(-.65, -.03, shape);
                float height = -.012 * basin + .003 * rim;
                float3 baseNormal = normalize(IN.normalWS);
                float3 reliefPosition = IN.positionWS + baseNormal * height;
                float3 normal = normalize(cross(ddy(reliefPosition), ddx(reliefPosition)));
                if (dot(normal, baseNormal) < 0) normal = -normal;
                float3 light = GetMainLight().direction;
                float shade = clamp((.55 + .45 * saturate(dot(normal, light))) /
                    (.55 + .45 * saturate(dot(baseNormal, light))), .88, 1.10);
                float3 compressedSand = lerp(1, _Color.rgb, .35) * (.98 + grain * .035);
                float3 modulation = lerp(compressedSand, 1.035, rim * .65) * shade;
                return half4(lerp(1, modulation, saturate(_Color.a * IN.color.a) * coverage), 1);
            }
            ENDHLSL
        }
    }
    FallBack Off
}
