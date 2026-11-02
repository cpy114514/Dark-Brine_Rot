Shader "DarkBrine/Brine Surface"
{
    Properties
    {
        _Body("Smoke and air tint",Color)=(.15,.14,.12,1)
        [HDR] _Edge("Brief contact highlight",Color)=(.72,.62,.44,1)
        _Fade("Visibility",Float)=1
        _Age("Normalized life",Float)=0
        _Clock("Simulation time",Float)=0
        _Mode("Surface mode",Float)=0
        _Heat("Thermal accent",Float)=0
        _DepthAvailable("Depth available",Float)=0
    }
    SubShader
    {
        Tags {"RenderPipeline"="UniversalPipeline" "Queue"="Transparent+15" "RenderType"="Transparent"}
        Pass
        {
            Tags {"LightMode"="UniversalForward"}
            Blend One OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/ThirdParty/NoiseShader/SimplexNoise3D.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Body,_Edge;
                float _Fade,_Age,_Clock,_Mode,_Heat,_DepthAvailable;
            CBUFFER_END
            float Hash(float2 p){p=frac(p*float2(123.34,456.21));p+=dot(p,p+45.32);return frac(p.x*p.y);}
            float Noise(float2 p)
            {
                float2 g=floor(p),f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(g),Hash(g+float2(1,0)),f.x),lerp(Hash(g+float2(0,1)),Hash(g+1),f.x),f.y);
            }
            struct A{float4 position:POSITION;float3 normal:NORMAL;float2 uv:TEXCOORD0;float2 extra:TEXCOORD1;half4 color:COLOR;};
            struct V{float4 position:SV_POSITION;float3 world:TEXCOORD0;half3 normal:TEXCOORD1;float2 uv:TEXCOORD2;half4 color:COLOR;float seed:TEXCOORD3;half fog:TEXCOORD4;};
            V Vert(A i)
            {
                V o;float3 p=i.position.xyz;
                if(_Mode>.5&&_Mode<1.5)
                {
                    float travel=1-pow(1-saturate(_Age),3);
                    p.xy*=.25+travel*.9;
                    p.z*=.15+travel*.85;
                    p.y-=_Age*_Age*.18;
                }
                if(_Mode>2.5&&_Mode<3.5)
                {
                    float3 normal=normalize(i.normal);
                    float fold=sin(i.uv.x*TWO_PI*7-i.uv.y*8+_Age*12)*sin(i.uv.y*PI);
                    p+=normal*(fold*.085+Noise(i.uv*float2(17,11)+_Age*2)*.13)*smoothstep(0,.2,_Age);
                    p.xz*=.08+pow(saturate(_Age*2.6),.52)*.92;
                    p.y*=.08+sin(saturate(_Age)*PI*.65)*1.12;
                    p.y+=sin(i.uv.x*TWO_PI*3+_Age*5)*.10*sin(i.uv.y*PI);
                }
                if(_Mode>3.5)
                {
                    float angle=-_Clock*.6+p.z*.35;
                    float2 q=p.xy;p.xy=float2(q.x*cos(angle)-q.y*sin(angle),q.x*sin(angle)+q.y*cos(angle));
                    p.xy*=.85+.05*sin(_Clock*3+i.uv.x*8+i.extra.x*TWO_PI);
                    p.z+=sin(_Clock*3-i.uv.x*8)*.008*sin(i.uv.x*PI);
                }
                o.world=TransformObjectToWorld(p);o.position=TransformWorldToHClip(o.world);
                o.normal=TransformObjectToWorldNormal(i.normal);o.uv=i.uv;o.seed=i.extra.x;o.color=i.color;
                o.fog=ComputeFogFactor(o.position.z);return o;
            }
            half4 Frag(V i,FRONT_FACE_TYPE facing:FRONT_FACE_SEMANTIC):SV_Target
            {
                float2 uv=i.uv;
                // Stretched turbulence breaks up air and grit without a bright
                // outline around the entire weapon sweep.
                float n=Noise(uv*float2(5,12)+float2(-_Clock*1.8,_Clock*.11)+i.seed*13);
                float fine=Noise(uv*float2(21,33)+float2(-_Clock*3.1,.3));
                // World-space detail remains anchored to the actual folded mesh.
                float spatial=.5+.5*SimplexNoise(i.world*5+float3(0,-_Clock*.65,0));
                n=lerp(n,spatial,.38);
                float across=pow(saturate(sin(uv.y*PI)),1.3);
                float fibre=smoothstep(.27,.78,n*.7+fine*.3);
                half3 normal=normalize(i.normal)*IS_FRONT_VFACE(facing,1,-1);
                Light light=GetMainLight();
                half3 illumination=max(SampleSH(normal),half3(.22,.22,.2))+light.color*(.22+.35*abs(dot(normal,light.direction)));
                half3 color=lerp(_Body.rgb,half3(.60,.59,.55),.65)*illumination;
                float alpha=across*(.08+fibre*.3);
                if(_Mode>.5&&_Mode<1.5)
                {
                    float taper=pow(saturate(sin(uv.x*PI)),.6);
                    float breakup=smoothstep(.2,.65,n+fine*.22-_Age*.36);
                    float glint=(.65+.35*step(.65,i.seed))*(1-smoothstep(.12,.55,_Age));
                    alpha=across*taper*(.65+fibre*.35)*breakup*(1-smoothstep(.12,.9,_Age));
                    color=lerp(_Body.rgb*illumination,_Edge.rgb*1.35,glint*(.65+_Heat*.35));
                }
                if(_Mode>1.5&&_Mode<2.5)
                {
                    // Low irregular dust lobes. Never a luminous expanding ring.
                    float patches=Noise(uv*float2(13,5)+float2(_Age*.4,-_Age*1.8));
                    float cloud=smoothstep(.32,.7,patches+n*.18-_Age*.23);
                    alpha=across*cloud*(.16+fibre*.28)*(1-smoothstep(.25,1,_Age));
                    color=lerp(_Body.rgb,_Edge.rgb,.35)*illumination;
                }
                if(_Mode>3.5)
                {
                    float tip=smoothstep(.1,.92,uv.x);
                    float ember=smoothstep(.55,.8,fine*n)*(.75+.25*sin(_Clock*4+i.seed*5));
                    alpha=across*tip*(.09+ember*.62)*(.35+_Heat*.65);
                    color=lerp(_Body.rgb*illumination,half3(.85,.49,.15),ember*(.4+_Heat*.6));
                }
                alpha=saturate(alpha*_Fade*i.color.a)*IS_FRONT_VFACE(facing,1,.45);
                if(_DepthAvailable>.5)
                {
                    float2 screen=GetNormalizedScreenSpaceUV(i.position);
                    float depth=LinearEyeDepth(SampleSceneDepth(screen),_ZBufferParams);
                    alpha*=saturate((depth+TransformWorldToView(i.world).z)/.13);
                }
                color=MixFog(color,i.fog);
                return half4(color*alpha,alpha);
            }
            ENDHLSL
        }
    }
}
