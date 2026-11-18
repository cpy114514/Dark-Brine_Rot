Shader "Dark Brine/Second Island/Viscous Chocolate"
{
    Properties
    {
        _BaseColor("Couverture cocoa", Color)=(.35,.135,.055,1)
        _FoldColor("Milk chocolate folds", Color)=(.5,.23,.11,1)
        _FlowSpeed("Flow metres per second", Range(0,20))=1.6
        _Smoothness("Cocoa butter gloss", Range(0,1))=.76
        _WaveHeight("Heavy surface displacement", Range(0,.2))=.075
        _LakeMode("Broad lake circulation", Range(0,1))=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+5"}
        HLSLINCLUDE
            #define _CLEARCOAT 1
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/EnvironmentVisualStyle.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor, _FoldColor;
                float _FlowSpeed, _Smoothness, _WaveHeight, _LakeMode;
            CBUFFER_END
            float4 _ChocolateImpacts[6]; // xyz = contact on the surface, w = start time
            float4 _ChocolateImpactCurrents[6]; // xy = horizontal current, z = impulse
            struct A {float4 positionOS:POSITION;float3 normalOS:NORMAL;float4 tangentOS:TANGENT;float2 uv:TEXCOORD0;};
            struct V {float4 positionCS:SV_POSITION;float3 positionWS:TEXCOORD0;half3 normalWS:TEXCOORD1;half3 tangentWS:TEXCOORD2;float2 uv:TEXCOORD3;half fog:TEXCOORD4;};
            float Hash(float2 p){return frac(sin(dot(p,float2(127.1,311.7)))*43758.5453);}
            float Noise(float2 p){float2 i=floor(p),f=frac(p);f=f*f*(3-2*f);return lerp(lerp(Hash(i),Hash(i+float2(1,0)),f.x),lerp(Hash(i+float2(0,1)),Hash(i+1),f.x),f.y);}
            float Fold(float2 p)
            {
                // The sideways detail is much finer than the downstream detail.
                float warp = Noise(p * float2(.46,.13)) - .5;
                float broad = Noise(float2(p.x * .73 + warp * 1.5, p.y * .21));
                float streak = Noise(float2(p.x * 1.9 + warp, p.y * .32));
                return (broad - .5) * 1.3 + (streak - .5) * .38;
            }
            float2 FlowUV(float2 uv)
            {
                float2 river=float2(uv.x*10,uv.y-_Time.y*_FlowSpeed);
                float2 offset=uv-float2(0,690);
                float angle=.026*_Time.y;
                float2 lake=float2(cos(angle)*offset.x-sin(angle)*offset.y, sin(angle)*offset.x+cos(angle)*offset.y);
                lake=lake*.55+float2(sin(lake.y*.24),cos(lake.x*.19))*1.8-float2(.23,.71)*_Time.y*_FlowSpeed;
                lake.y*=1.6;
                return lerp(river,lake,_LakeMode);
            }
            V Vert(A i)
            {
                V o;float edge=lerp(saturate(sin(i.uv.x*PI)),1,_LakeMode);
                float2 flow=FlowUV(i.uv);
                i.positionOS.xyz+=i.normalOS*(Fold(flow)*_WaveHeight*edge);
                o.positionWS=TransformObjectToWorld(i.positionOS.xyz);o.positionCS=TransformWorldToHClip(o.positionWS);
                o.normalWS=TransformObjectToWorldNormal(i.normalOS);o.tangentWS=TransformObjectToWorldDir(i.tangentOS.xyz);
                o.uv=i.uv;o.fog=ComputeFogFactor(o.positionCS.z);return o;
            }
            half4 Frag(V i):SV_Target
            {
                float2 p=FlowUV(i.uv);
                float f=Fold(p),dx=(Fold(p+float2(.05,0))-f)/.05,dy=(Fold(p+float2(0,.05))-f)/.05;
                half3 n=normalize(i.normalWS);
                half3 t=i.tangentWS-n*dot(n,i.tangentWS);
                t=dot(t,t)>.001?normalize(t):half3(1,0,0);
                half3 b=normalize(cross(n,t));
                n=normalize(n-t*dx*.14-b*dy*.16);
                float ripple=0;
                float2 rippleSlope=0;
                [unroll] for (int j=0;j<6;j++)
                {
                    float4 impulse=_ChocolateImpactCurrents[j];
                    float age=_Time.y-_ChocolateImpacts[j].w;
                    float3 delta=i.positionWS-_ChocolateImpacts[j].xyz;
                    if (impulse.z <= 0 || age >= 1.52 || abs(delta.y) >= 1.54 || dot(delta.xz,delta.xz) > 64) continue;
                    delta.xz-=impulse.xy*age;
                    float dist=length(delta.xz);
                    float front=dist-age*2.8;
                    float strength=impulse.z*saturate(1-age*.66)*
                        saturate(1-abs(delta.y)*.65)*saturate(1-abs(front)*.62);
                    float phase=front*5.8;
                    ripple+=sin(phase)*strength*.085;
                    rippleSlope+=delta.xz/max(.13,dist)*cos(phase)*strength*.14;
                }
                n=normalize(n-float3(rippleSlope.x,0,rippleSlope.y));
                half ribbon=smoothstep(.10,.84,Noise(float2(p.x*1.35+f,p.y*.15)))*.35;
                half bank=pow(saturate(abs(i.uv.x*2-1)),8)*(1-_LakeMode);
                SurfaceData s=(SurfaceData)0;
                s.albedo=lerp(_BaseColor.rgb,_FoldColor.rgb,saturate(ribbon+f*.19+ripple))*(1-bank*.16);
                s.specular=half3(.055,.045,.038);s.metallic=0;s.smoothness=_Smoothness-bank*.11;
                s.normalTS=half3(0,0,1);s.occlusion=1;s.alpha=1;s.clearCoatMask=.22;s.clearCoatSmoothness=.83;
                InputData d=(InputData)0;d.positionWS=i.positionWS;d.normalWS=n;d.viewDirectionWS=GetWorldSpaceNormalizeViewDir(i.positionWS);
                d.shadowCoord=TransformWorldToShadowCoord(i.positionWS);d.bakedGI=SampleSH(n);d.normalizedScreenSpaceUV=GetNormalizedScreenSpaceUV(i.positionCS);d.shadowMask=1;
                half3 color=UniversalFragmentPBR(d,s).rgb;
                // Dense cocoa absorbs sky colour; reflections modulate warm chocolate instead
                // of turning a large horizontal lake into silver water.
                half luminance=dot(color,half3(.2126,.7152,.0722));
                color=s.albedo*(.42+min(1.15,luminance*2.0));
                color=lerp(color,half3(.78,.46,.22),smoothstep(.7,1.8,luminance)*.28);
                half3 h=normalize(GetMainLight().direction+d.viewDirectionWS);
                color+=half3(.58,.35,.18)*pow(saturate(dot(n,h)),37)*_Smoothness*.28;
                color=StyleEnvironmentColor(MixFog(color,i.fog),d.normalizedScreenSpaceUV,0);
                return half4(color,1);
            }
            half4 DepthFrag(V i):SV_Target{return 0;}
            half4 NormalsFrag(V i):SV_Target
            {
                float3 n=normalize(i.normalWS);
                #if defined(_GBUFFER_NORMALS_OCT)
                    float2 oct=PackNormalOctQuadEncode(n);
                    return half4(PackFloat2To888(saturate(oct*.5+.5)),0);
                #else
                    return half4(n,0);
                #endif
            }
        ENDHLSL
        Pass
        {
            Name "ChocolateForward"
            Tags {"LightMode"="UniversalForwardOnly"}
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ VAN_GOGH_SURFACE
            ENDHLSL
        }
        Pass
        {
            Name "ChocolateDepth"
            Tags {"LightMode"="DepthOnly"}
            Cull Off ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment DepthFrag
            ENDHLSL
        }
        Pass
        {
            Name "ChocolateDepthNormals"
            Tags {"LightMode"="DepthNormalsOnly"}
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment NormalsFrag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
}
