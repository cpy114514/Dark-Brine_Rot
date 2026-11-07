Shader "DarkBrine/Brine Thermal Volume"
{
    Properties
    {
        _DensityTex("Periodic three-dimensional density",3D)="white"{}
        _Age("Normalized age",Float)=0
        _Variation("Density variation",Float)=0
        _Fade("Envelope",Float)=1
        _Steps("Ray samples",Range(20,48))=36
        _DepthAvailable("Scene depth available",Float)=0
        _Dust("Non-emissive contact dust",Float)=0
        _ColorAvailable("Opaque scene color available",Float)=0
        _RefractionPixels("Heat refraction limit in pixels",Range(0,3))=1.5
        [HDR] _FireCore("Core radiance",Color)=(2.2,1.05,.25,1)
        [HDR] _FireBody("Burning gas",Color)=(.95,.28,.035,1)
        _Smoke("Cooled soot",Color)=(.065,.07,.08,1)
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "RenderType"="Transparent" "Queue"="Transparent+16"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Blend One OneMinusSrcAlpha
            ZWrite Off Cull Front ZTest Always
            HLSLPROGRAM
            #pragma target 3.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Assets/ThirdParty/NoiseShader/SimplexNoise3D.hlsl"
            TEXTURE3D(_DensityTex);SAMPLER(sampler_DensityTex);
            CBUFFER_START(UnityPerMaterial)
                half4 _FireCore,_FireBody,_Smoke;
                float _Age,_Fade,_Steps,_DepthAvailable,_Dust,_ColorAvailable,_RefractionPixels,_Variation;
            CBUFFER_END
            struct A{float4 position:POSITION;};
            struct V{float4 position:SV_POSITION;float3 local:TEXCOORD0;};
            V Vert(A i){V o;o.position=TransformObjectToHClip(i.position.xyz);o.local=i.position.xyz;return o;}
            float SmoothMin(float a,float b,float k){float h=saturate(.5+.5*(b-a)/k);return lerp(b,a,h)-k*h*(1-h);}
            float Density(float3 p,out float heat,out float noise)
            {
                float expand=.055+.945*(1-pow(1-saturate(_Age*4),3));
                float3 q=p/float3(expand,.25+.75*expand,expand);
                q.y-=_Age*.45;
                float core=length((q-float3(0,.46,0))/float3(.75,.65,.75))-1;
                float crown=length((q-float3(.12,.94,.07))/float3(.61,.47,.59))-1;
                if(_Dust>.5)
                {
                    core=length((q-float3(-.10,.26,0))/float3(.62,.30,.55))-1;
                    crown=length((q-float3(.23,.40,.08))/float3(.45,.30,.45))-1;
                }
                float shape=SmoothMin(core,crown,.45);
                float angle=q.y*1.6+_Age*2.5,s=sin(angle),c=cos(angle);
                float3 flow=float3(q.x*c-q.z*s,q.y-_Age*.65,q.x*s+q.z*c);
                flow+=float3(_Variation*3.17,0,_Variation*5.31);
                noise=SAMPLE_TEXTURE3D_LOD(_DensityTex,sampler_DensityTex,flow*.54+float3(.13,_Age*.12,.37),0).r;
                float detail=SAMPLE_TEXTURE3D_LOD(_DensityTex,sampler_DensityTex,flow*1.6+noise*.18-float3(0,_Age*.38,0),0).r;
                float erosion=(noise-.48)*1.05+(detail-.5)*.42;
                float density=pow(saturate(-shape*.9+erosion),1.45);
                density*=smoothstep(-.09,.06,p.y)*(1-smoothstep(.48,1,_Age));
                // Density reaches zero before the finite marching box. This
                // keeps cooling smoke from exposing straight proxy-box edges.
                density*=1-smoothstep(.90,1.22,length(p.xz));
                density*=1-smoothstep(1.7,1.98,p.y);
                float coreDistance=length((q-float3(.02,.50,0))/float3(.8,.7,.8));
                heat=saturate(1.20-coreDistance*.50-_Age*1.55+(noise-.5)*.48+(detail-.5)*.24);
                if(_Dust>.5){heat=0;density*=smoothstep(0,.09,_Age);}
                return density*(_Dust>.5?2.8:3.8);
            }
            half4 Frag(V i):SV_Target
            {
                float3 origin=TransformWorldToObject(GetCameraPositionWS());float3 ray=normalize(i.local-origin);
                float3 inv=rcp(ray+float3(.000001,.000001,.000001));
                float3 a=(float3(-1.25,-.1,-1.25)-origin)*inv,b=(float3(1.25,2,1.25)-origin)*inv;
                float3 lo=min(a,b),hi=max(a,b);float near=max(0,max(lo.x,max(lo.y,lo.z))),far=min(hi.x,min(hi.y,hi.z));
                if(_DepthAvailable>.5)
                {
                    float3 start=TransformObjectToWorld(origin+ray*near);
                    float eye=-TransformWorldToView(start).z;
                    float scene=LinearEyeDepth(SampleSceneDepth(GetNormalizedScreenSpaceUV(i.position)),_ZBufferParams);
                    float3 worldRay=mul((float3x3)unity_ObjectToWorld,ray);
                    float eyeStep=-mul((float3x3)UNITY_MATRIX_V,worldRay).z;
                    far=min(far,near+max(0,scene-eye)/max(.0001,eyeStep));
                }
                if(far<=near)return 0;
                float stepSize=(far-near)/_Steps;
                float jitter=frac(sin(dot(floor(i.position.xy),float2(12.9898,78.233)))*43758.5453);
                float t=near+(.35+jitter*.3)*stepSize,transmission=1;float3 color=0;
                Light light=GetMainLight();
                float3 localLight=normalize(mul((float3x3)unity_WorldToObject,light.direction));
                [loop]for(int sample=0;sample<48;sample++)
                {
                    if(sample>=_Steps||transmission<.025)break;
                    float3 p=origin+ray*t;float heat,noise;
                    float density=Density(p,heat,noise)*_Fade;
                    float alpha=1-exp(-density*stepSize*3.2);
                    float3 normal=normalize(p-float3(0,.65+_Age*.45,0)+.0001);
                    float lighting=.3+.7*saturate(dot(normal,localLight)+(noise-.5)*.35);
                    // One short light-path sample supplies internal absorption.
                    // Skip on the lowest volume quality tier.
                    float shadow=1;
                    if(_Steps>24){float h2,n2;shadow=exp(-Density(p+localLight*.24,h2,n2)*.65);}
                    float3 soot=_Smoke.rgb*(.65+noise*.65)*(lighting*light.color*shadow+.32);
                    float3 fire=lerp(float3(.24,.12,.045),_FireBody.rgb,smoothstep(.25,.72,heat));
                    fire=lerp(fire,_FireCore.rgb,smoothstep(.76,.96,heat));
                    float3 scatter=lerp(soot,fire,smoothstep(.48,.85,heat));
                    color+=transmission*scatter*alpha;transmission*=1-alpha;t+=stepSize;
                }
                float opacity=1-transmission;
                float fog=ComputeFogFactor(TransformWorldToHClip(TransformObjectToWorld(origin+ray*near)).z);
                color=MixFog(color/max(opacity,.001),fog)*opacity;
                if(_ColorAvailable>.5&&_Dust<.5&&_RefractionPixels>0)
                {
                    float2 screen=GetNormalizedScreenSpaceUV(i.position);
                    float3 probe=origin+ray*(near+far)*.5;
                    float3 gradient=SimplexNoiseGrad(probe*4+float3(0,-_Age*3,0)).xyz;
                    float2 offset=clamp(gradient.xy*.12,-1,1)*_RefractionPixels/_ScaledScreenParams.xy;
                    offset*=opacity*(1-smoothstep(.15,.75,_Age));
                    float2 warped=clamp(screen+offset,.001,.999);
                    // Never pull an occluding foreground object into the heat.
                    float sceneEye=LinearEyeDepth(SampleSceneDepth(warped),_ZBufferParams);
                    float entryEye=-TransformWorldToView(TransformObjectToWorld(origin+ray*near)).z;
                    float valid=step(entryEye,sceneEye);
                    color+=(SampleSceneColor(warped)-SampleSceneColor(screen))*opacity*valid;
                }
                return half4(max(0,color),opacity);
            }
            ENDHLSL
        }
    }
}
