Shader "Dark Brine/Second Island/Gravel Trail"
{
    Properties
    {
        _BaseMap ("Stone albedo", 2D) = "white" {}
        _BaseColor ("Trail tint", Color) = (.86,.72,.76,1)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent-24" "RenderType"="Transparent" }
        Pass
        {
            ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile_fragment _ VAN_GOGH_SURFACE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/EnvironmentVisualStyle.hlsl"
            #if defined(VAN_GOGH_SURFACE)
            #include "Assets/Shaders/VanGoghPigment.hlsl"
            #endif

            TEXTURE2D(_BaseMap); SAMPLER(sampler_BaseMap);
            CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; float2 edge : TEXCOORD1; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 normalWS : TEXCOORD0; float2 uv : TEXCOORD1; float2 edge : TEXCOORD2; half fog : TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.edge = input.edge;
                output.fog = ComputeFogFactor(output.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half3 stone = SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, input.uv).rgb;
                half luma = dot(stone, half3(.2126,.7152,.0722));
                half3 normal = normalize(input.normalWS);
                Light sun = GetMainLight();
                half3 illumination = max(SampleSH(normal), .27) + sun.color * saturate(dot(normal, sun.direction)) * .65;
                half3 colour = lerp(stone, luma.xxx, .77) * _BaseColor.rgb * illumination;
                half shoulder = 1 - smoothstep(.27 + luma * .14, .5, abs(input.edge.x - .5));
                half ends = smoothstep(0,.075,input.edge.y) * (1-smoothstep(.89,1,input.edge.y));
                colour = StyleEnvironmentColor(MixFog(colour, input.fog), GetNormalizedScreenSpaceUV(input.positionCS), 0);
                #if defined(VAN_GOGH_SURFACE)
                colour = VGSurface(colour, input.uv * 4, .2, 5);
                #endif
                return half4(colour, shoulder * ends * .88);
            }
            ENDHLSL
        }
    }
}
