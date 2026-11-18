Shader "Dark Brine/Second Island/Sahur Chocolate Coating"
{
    Properties
    {
        [MainTexture] _BaseMap("Sahur albedo", 2D) = "white" {}
        [MainColor] _BaseColor("Sahur tint", Color) = (1,1,1,1)
        [Normal] _BumpMap("Sahur normals", 2D) = "bump" {}
        _BumpScale("Normal strength", Float) = .7
        _MetallicGlossMap("Sahur metallic smoothness", 2D) = "white" {}
        _Metallic("Metallic", Range(0,1)) = 0
        _Smoothness("Smoothness", Range(0,1)) = 1
        _OcclusionStrength("Occlusion", Range(0,1)) = 1
        _Cutoff("Cutoff", Range(0,1)) = .5
        _Cull("Cull", Float) = 2
        [HideInInspector] _Surface("Opaque surface", Float) = 0
        [NoScaleOffset] _ChocolateBodyMask("Coating by body region", 2D) = "black" {}
        _ChocolateCoating("Immersion and splash coverage", Vector) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_ATLAS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _SCREEN_SPACE_IRRADIANCE
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Fog.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ProbeVolumeVariants.hlsl"

            #define InitializeStandardLitSurfaceData InitializeDrySahurSurface
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #undef InitializeStandardLitSurfaceData

            TEXTURE2D(_ChocolateBodyMask);
            SAMPLER(sampler_ChocolateBodyMask);
            float4 _ChocolateCoating;

            inline void InitializeStandardLitSurfaceData(float2 uv, out SurfaceData surface)
            {
                InitializeDrySahurSurface(uv, surface);
                half2 mask = SAMPLE_TEXTURE2D(_ChocolateBodyMask, sampler_ChocolateBodyMask, uv).rg;
                half wetCoverage = smoothstep(.08, .26, mask.r * _ChocolateCoating.x);
                half splashCoverage = smoothstep(.29, .63, mask.g * _ChocolateCoating.y);
                half coating = saturate(max(wetCoverage, splashCoverage));
                half3 cocoa = lerp(half3(.077,.023,.009), half3(.139,.051,.019), mask.r);
                surface.albedo = lerp(surface.albedo, cocoa * _BaseColor.rgb, coating * .95);
                surface.metallic *= 1 - coating;
                surface.smoothness = lerp(surface.smoothness, .87, coating);
                surface.normalTS = normalize(lerp(surface.normalTS, half3(0,0,1), coating * .8));
            }

            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitForwardPass.hlsl"
            ENDHLSL
        }
        UsePass "Universal Render Pipeline/Lit/ShadowCaster"
        UsePass "Universal Render Pipeline/Lit/DepthOnly"
        // Match the prepass tag used for forward-only objects in URP.
        Pass
        {
            Name "DepthNormalsOnly"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma shader_feature_local _NORMALMAP
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitInput.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }
}
