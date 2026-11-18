Shader "Dark Brine/Second Island/Chocolate Flow"
{
    Properties
    {
        _Cocoa("Dark cocoa", Color) = (.225,.084,.045,1)
        _Mode("Lake, channel, curtain", Float) = 0
        _Gloss("Cocoa butter gloss", Range(0,1)) = .72
        _LakeOutlet("Lake outlet direction (XZ)", Vector) = (-1,0,0,0)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+5" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
        #include "Assets/Shaders/EnvironmentVisualStyle.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _Cocoa;
            float4 _LakeOutlet;
            half _Mode, _Gloss;
        CBUFFER_END
        float4 _ChocolateImpacts[6];
        float4 _ChocolateImpactCurrents[6];

        struct Attributes
        {
            float4 positionOS : POSITION;
            float3 normalOS : NORMAL;
            float4 tangentOS : TANGENT;
            float2 uv : TEXCOORD0;
            float4 flow : TEXCOORD1;
        };
        struct Varyings
        {
            float4 positionCS : SV_POSITION;
            float3 positionWS : TEXCOORD0;
            half3 normalWS : TEXCOORD1;
            half3 tangentWS : TEXCOORD2;
            float2 uv : TEXCOORD3;
            float4 flow : TEXCOORD4;
            half fog : TEXCOORD5;
        };

        float Phase(float4 flow, float distance)
        {
            // Baked upstream travel time prevents indefinitely stretching detail at changes in speed.
            return (flow.w > 0 ? flow.w : distance * .18) - _Time.y;
        }

        float AcrossBank(float u)
        {
            return smoothstep(.42,1,abs(u*2-1));
        }

        Varyings Vert(Attributes v)
        {
            Varyings o;
            o.positionWS = TransformObjectToWorld(v.positionOS.xyz);
            o.normalWS = TransformObjectToWorldNormal(v.normalOS);
            o.tangentWS = TransformObjectToWorldDir(v.tangentOS.xyz);
            if(_Mode>.5 && v.flow.w>0)
            {
                float phase=Phase(v.flow,v.uv.y);
                float spread=1-AcrossBank(v.uv.x);
                float wave=sin(phase*4.4+v.uv.x*13)*sin(phase*1.9-v.uv.x*7);
                float fall=_Mode<1.5 ? 0 : v.flow.x;
                // Both surfaces share the vertical swell at the spill lip.
                o.positionWS.y+=.045*smoothstep(0,14,v.uv.y)*spread*(1-fall)*wave;
                o.positionWS.z+=.41*fall*wave;
            }
            o.positionCS = TransformWorldToHClip(o.positionWS);
            o.uv = v.uv;
            o.flow = v.flow;
            o.fog = ComputeFogFactor(o.positionCS.z);
            return o;
        }

        float Hash(float2 p)
        {
            p = frac(p * float2(.1031,.11369));
            p += dot(p, p.yx + 19.19);
            return frac(p.x * p.y);
        }
        // Value and analytical gradient from one lattice lookup.
        float3 Field(float2 p)
        {
            float2 cell=floor(p), f=frac(p), u=f*f*(3-2*f), du=6*f*(1-f);
            float a=Hash(cell), b=Hash(cell+float2(1,0));
            float c=Hash(cell+float2(0,1)), d=Hash(cell+1);
            float e=d-c-b+a;
            return float3(a+(b-a)*u.x+(c-a)*u.y+e*u.x*u.y,
                du.x*(b-a+e*u.y), du.y*(c-a+e*u.x));
        }

        float3 LakeFlow(float2 world, float2 outlet, float pull)
        {
            float3 calm=Field(world*.103+_Time.y*float2(.008,.004));
            float2 p=world-outlet*_Time.y*2.85;
            float3 draw=Field(p*.26)+Field(p*.52+18)*.29;
            return lerp(calm,draw,pull);
        }
        float2 Ripples(float3 world)
        {
            float2 slope=0;
            [unroll] for(int j=0;j<6;j++)
            {
                float4 impulse=_ChocolateImpactCurrents[j];
                float age=_Time.y-_ChocolateImpacts[j].w;
                float3 delta=world-_ChocolateImpacts[j].xyz;
                if(impulse.z<=0 || age>=1.5 || abs(delta.y)>1.5 || dot(delta.xz,delta.xz)>64) continue;
                delta.xz-=impulse.xy*age;
                float dist=length(delta.xz);
                float ring=dist-age*2.4;
                float envelope=exp2(-ring*ring*2.7) * saturate(1-age/1.5) * saturate(1-abs(delta.y)*.7);
                slope+=delta.xz/max(.22,dist) * sin(ring*6.2) * envelope * impulse.z * .28;
            }
            return slope;
        }
        half4 Frag(Varyings i, FRONT_FACE_TYPE face : FRONT_FACE_SEMANTIC) : SV_Target
        {
            float3 n=normalize(i.normalWS)*IS_FRONT_VFACE(face,1,-1);
            float3 tangent=normalize(i.tangentWS);
            float3 longitudinal=cross(n,tangent);
            float bank=AcrossBank(i.uv.x);
            float pull=saturate(i.flow.w);
            float3 field;
            float strength;
            float crest;
            if(_Mode<.5)
            {
                field=LakeFlow(i.positionWS.xz,_LakeOutlet.xy,pull);
                float2 ripple=Ripples(i.positionWS);
                n=normalize(n-float3(field.y*.135+ripple.x,0,field.z*.135+ripple.y)*lerp(.5,1.4,pull));
                strength=lerp(.10,.68,pull);
                crest=smoothstep(.52,.91,field.x);
                bank=0;
            }
            else
            {
                float fall=_Mode<1.5?0:i.flow.x;
                float phase=Phase(i.flow,i.uv.y);
                float2 p=float2(i.uv.x*5.8,phase*.88+sin(i.uv.x*10)*.48);
                p.x+=sin(i.uv.y*.079+i.uv.x*9)*.38+sin(phase*1.26+i.uv.x*6)*.17;
                float3 fold=Field(p);
                float3 crease=Field(p*float2(1.85,1.33)+17);
                field=fold+crease*.27;
                strength=lerp(1,.45,bank)*(1+fall*.39);
                n=normalize(n-(tangent*(fold.y*.092+crease.y*.059)+
                    longitudinal*(fold.z*.32+crease.z*.16))*strength);
                crest=smoothstep(.26,.70,fold.x)*smoothstep(.09,1.07,-fold.z+crease.x*.3);
                crest=crest*.73+smoothstep(.5,.84,crease.x)*smoothstep(.19,.91,-crease.z)*.4;
                if(_Mode<1.5)
                {
                    float turn=i.flow.z*(i.uv.x*2-1);
                    if(bank>.03)
                    {
                        // Broad, weak return eddies stay local to the inside bank.
                        float eddyTime=_Time.y*lerp(.45,-.23,smoothstep(0,.37,turn));
                        float3 eddy=Field(float2(i.uv.x*7,i.uv.y*.12-eddyTime)+21);
                        n=normalize(n-(tangent*eddy.y*.12+longitudinal*eddy.z*.21)*bank);
                    }
                    float mouth=1-smoothstep(0,14,i.uv.y);
                    if(mouth>.001)
                    {
                        float3 lake=LakeFlow(i.positionWS.xz,i.flow.xy,1);
                        n=normalize(lerp(n,normalize(float3(-lake.y*.189,1,-lake.z*.189)),mouth));
                        field=lerp(field,lake,mouth);
                        crest=lerp(crest,smoothstep(.52,.91,lake.x),mouth);
                        strength=lerp(strength,.68,mouth);
                        bank*=1-mouth;
                    }
                    float2 ripple=Ripples(i.positionWS);
                    n=normalize(n-float3(ripple.x,0,ripple.y));
                }
            }
            float3 view=GetWorldSpaceNormalizeViewDir(i.positionWS);
            Light sun=GetMainLight(TransformWorldToShadowCoord(i.positionWS));
            half lit=saturate(dot(n,sun.direction)*.72+.28);
            half3 ambient=max(SampleSH(n),half3(.19,.17,.17));
            half3 body=_Cocoa.rgb*(ambient*.54+.37+lit*sun.shadowAttenuation*.40);
            body*=1+(field.x-.61)*strength*.23-bank*.15;
            half fresnel=pow(1-saturate(dot(n,view)),3);
            half sky=saturate(reflect(-view,n).y*.6+.4);
            half band=(sky-.53)*5.2;
            half horizon=exp2(-band*band);
            half3 sheen=lerp(half3(.13,.076,.045),half3(.46,.269,.143),sky);
            sheen+=half3(.23,.14,.081)*horizon;
            half spec=saturate(dot(n,SafeNormalize(view+sun.direction)));
            half gloss=_Gloss*(1-bank*.30);
            half warmth=lerp(.67,1.31,smoothstep(.21,1.15,field.x));
            half3 color=body+sheen*(.085+fresnel*.62)*gloss*warmth;
            half swash=smoothstep(.25,.78,crest*.94+sky*.19);
            color+=half3(.54,.28,.139)*swash*gloss*strength;
            color+=half3(1,.73,.52)*(pow(spec,38)*.36+pow(spec,12)*.11)*gloss*sun.shadowAttenuation;
            return half4(StyleEnvironmentColor(MixFog(color,i.fog),GetNormalizedScreenSpaceUV(i.positionCS),0),1);
        }
        half4 Depth(Varyings i) : SV_Target { return 0; }
        half4 Normals(Varyings i) : SV_Target
        {
            float3 normal=normalize(i.normalWS);
            #if defined(_GBUFFER_NORMALS_OCT)
                return half4(PackFloat2To888(saturate(PackNormalOctQuadEncode(normal)*.5+.5)),0);
            #else
                return half4(normal,0);
            #endif
        }
        ENDHLSL
        Pass
        {
            Name "ChocolateForward"
            Tags { "LightMode"="UniversalForwardOnly" }
            Cull Off
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ VAN_GOGH_SURFACE
            ENDHLSL
        }
        Pass
        {
            Name "ChocolateDepth"
            Tags { "LightMode"="DepthOnly" }
            Cull Off ZWrite On ColorMask R
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Depth
            ENDHLSL
        }
        Pass
        {
            Name "ChocolateDepthNormals"
            Tags { "LightMode"="DepthNormalsOnly" }
            Cull Off ZWrite On
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Normals
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            ENDHLSL
        }
    }
}
