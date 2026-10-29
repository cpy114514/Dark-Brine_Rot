Shader "DarkBrine/Marshmallow Surface"
{
    Properties
    {
        _Vanilla ("Vanilla cream", Color) = (1,.94,.90,1)
        _Strawberry ("Strawberry floss", Color) = (1,.72,.82,1)
        _Lavender ("Lavender sugar", Color) = (.84,.79,.98,1)
        _PressedColor ("Compressed strawberry", Color) = (.96,.50,.68,1)
        _PatchScale ("World palette scale", Float) = .035
        _FiberScale ("Fine sugar texture scale", Float) = 10
        _CloudMode ("Distant cloud mode", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry" }
        HLSLINCLUDE
        #include "MarshmallowSurface.hlsl"
        ENDHLSL
        Pass
        {
            Name "FluffyForward"
            Tags { "LightMode"="UniversalForward" }
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CandyVertex
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fog
            half4 frag(CandyVaryings i) : SV_Target
            {
                float3 response = CandyResponse(i.restWS);
                float2 p = i.restWS.xz * _PatchScale;
                float broad = CandyNoise(p + float2(CandyNoise(p * .61), CandyNoise(p * .7 + 31)) * 2);
                float swirls = .5 + .5 * sin(p.x * .8 + p.y * .55 + broad * 5);
                half3 albedo = lerp(_Vanilla.rgb, _Strawberry.rgb, smoothstep(.24, .8, broad) * .76);
                albedo = lerp(albedo, _Lavender.rgb, smoothstep(.38, .9, swirls) * .42);
                // Stable fine fibres and tiny sugar highlights, softened with distance.
                float fibre = CandyNoise(i.restWS.xz * _FiberScale + i.restWS.y * .13);
                float detailFade = 1 - smoothstep(18, 65, distance(GetCameraPositionWS(), i.positionWS));
                albedo *= 1 + (fibre - .5) * .065 * detailFade;
                albedo = lerp(albedo, _PressedColor.rgb, saturate(response.y) * .46);
                albedo = lerp(albedo, _Vanilla.rgb * 1.12, saturate(response.z) * .55);
                float epsilon = .13;
                float dx = (CandyResponse(i.restWS + float3(epsilon,0,0)).x - response.x) / epsilon;
                float dz = (CandyResponse(i.restWS + float3(0,0,epsilon)).x - response.x) / epsilon;
                half3 n = normalize(i.normalWS - float3(dx,0,dz) * smoothstep(.1,.8,i.normalWS.y));
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS));
                half wrapped = saturate((dot(n, sun.direction) + .4h) / 1.4h);
                half shadow = lerp(.57h, 1.h, sun.shadowAttenuation);
                half3 ambient = max(SampleSH(n), half3(.22,.19,.26));
                half3 color = albedo * (ambient * .7 + sun.color * wrapped * shadow * .62 + .13);
                half rim = pow(1 - saturate(dot(n, GetWorldSpaceNormalizeViewDir(i.positionWS))), 3);
                color += _Vanilla.rgb * rim * .065;
                color += pow(saturate(fibre), 18) * detailFade * .035 * wrapped;
                return half4(MixFog(color, i.fog), 1);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0 Cull Back
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex shadowVertex
            #pragma fragment shadowFragment
            float3 _LightDirection;
            CandyVaryings shadowVertex(CandyAttributes v)
            {
                CandyVaryings o = CandyVertex(v);
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(o.positionWS,o.normalWS,_LightDirection));
                #if UNITY_REVERSED_Z
                    o.positionCS.z = min(o.positionCS.z, UNITY_NEAR_CLIP_VALUE * o.positionCS.w);
                #else
                    o.positionCS.z = max(o.positionCS.z, UNITY_NEAR_CLIP_VALUE * o.positionCS.w);
                #endif
                return o;
            }
            half4 shadowFragment(CandyVaryings i) : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode"="DepthOnly" }
            ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CandyVertex
            #pragma fragment depthFragment
            half4 depthFragment(CandyVaryings i) : SV_Target { return i.positionCS.z; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex CandyVertex
            #pragma fragment normalsFragment
            half4 normalsFragment(CandyVaryings i) : SV_Target { return half4(normalize(i.normalWS),0); }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Lit"
}
