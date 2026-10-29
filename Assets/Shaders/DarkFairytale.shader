Shader "Hidden/DarkBrine/DarkFairytale"
{
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" }
        Pass
        {
            Name "Illustrated opaque color"
            ZTest Always ZWrite Off Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fragment _ _GBUFFER_NORMALS_OCT
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            float4 _Style, _Palette, _InkSettings;
            float _StyleMode;
            float4 _Paper, _PaperShape, _Film, _FilmOptions;
            half4 _Ink, _ShadowTint, _HighlightTint;
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            half4 FilmColor(half4 source, float2 uv)
            {
                float l = max(0, dot(source.rgb, float3(.2126,.7152,.0722)));
                // Shoulder and toe retain highlight texture and readable deep shadows.
                float exposed = l / (1 + .18 * l);
                // Curve operates on linear luminance: a low toe preserves dark
                // surface variation without lifting every shadow to middle gray.
                float gray = pow(max(.00001,exposed),_Film.x);
                gray /= 1 + .06 * gray;
                float black = lerp(.016,.004,_Film.w);
                gray = black + (1-black)*gray;
                gray = lerp(gray,gray*gray*(3-2*gray),_Film.w*.12);
                if (_FilmOptions.x > .001)
                {
                    float2 tap = 2 / _ScaledScreenParams.xy;
                    float bright = 0;
                    bright += max(0, dot(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+float2(tap.x,0)).rgb,float3(.2126,.7152,.0722))-.7);
                    bright += max(0, dot(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-float2(tap.x,0)).rgb,float3(.2126,.7152,.0722))-.7);
                    bright += max(0, dot(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv+float2(0,tap.y)).rgb,float3(.2126,.7152,.0722))-.7);
                    bright += max(0, dot(SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv-float2(0,tap.y)).rgb,float3(.2126,.7152,.0722))-.7);
                    gray += min(.025, bright * _FilmOptions.x * .25);
                }
                float2 p = (uv-.5) * float2(1,.8);
                gray *= 1 - _Film.z * smoothstep(.10,.48,dot(p,p));
                float grain = Hash(floor(uv * min(_ScaledScreenParams.xy,float2(1920,1080))))-.5;
                gray += grain * _Film.y * (_InkSettings.y>.5 ? .25 : 1);
                float dither = (frac(dot(floor(uv*_ScaledScreenParams.xy),float2(.754877,.56984)))-.5)*_FilmOptions.y;
                gray = clamp(gray + dither, .003, .985);
                // Intensity blends tonal response, never reintroduces scene hue.
                float natural = saturate(l / (1 + .08*l));
                gray = lerp(natural,gray,_Style.x);
                return half4(gray.xxx,source.a);
            }
            half3 PaperColor(half3 source, float2 uv, float rawDepth, float3 normal, float depth, float edge)
            {
                float3 world = ComputeWorldSpacePosition(uv, rawDepth, UNITY_MATRIX_I_VP);
                float3 n = normalize(normal + float3(0,.00001,0));
                float steps = max(3,_PaperShape.y);
                float3 planar = normalize(floor(n*steps+.5)/steps + float3(0,.00001,0));
                float3 a = abs(n);
                float2 sheet = a.y>=a.x && a.y>=a.z ? world.xz : (a.x>a.z ? world.zy : world.xy);
                sheet *= _PaperShape.x;
                float diagonal = frac(sheet.x + sheet.y);
                float footprint = max(length(ddx(sheet)),length(ddy(sheet)));
                float creaseAA = max(fwidth(diagonal), .015);
                float crease = 1 - smoothstep(.012, .012 + creaseAA, abs(diagonal-.5));
                float resolved = 1-smoothstep(.25,.8,footprint);
                float fold = (step(.5,diagonal)-.5) * .28 * resolved;
                float3 light = normalize(_MainLightPosition.xyz + float3(.0001,.0001,0));
                float smoothLight = .42 + .58*saturate(dot(n,light));
                float face = floor(saturate(dot(planar,light))*4+.5)/4;
                float paperLight = .42 + .58*face + fold;
                float l = max(.0001,dot(source,float3(.2126,.7152,.0722)));
                float softL = l / (1 + .28*l);
                half3 color = source * lerp(1,softL/l,_Paper.z);
                float albedoL = max(.001,softL/max(.3,smoothLight));
                float flatL = floor(albedoL*6+.5)/6;
                color *= lerp(1,clamp(flatL/albedoL,.65,1.3),_Paper.z*.55);
                color *= lerp(1,paperLight/max(.3,smoothLight),_Paper.y);
                float luminance = dot(color,float3(.2126,.7152,.0722));
                color = lerp(luminance.xxx,color,1.04);
                float green = saturate((source.g-max(source.r,source.b))*6);
                color = lerp(color,luminance*half3(.72,1.08,.92),green*.5*_Paper.z);
                color *= lerp(half3(.84,.96,.91),half3(1.10,1.035,.90),saturate(paperLight));
                color = lerp(color,half3(.96,.88,.71),.07*_Paper.z);
                float fiberAA = max(length(ddx(world*30)),length(ddy(world*30)));
                float fiber = Hash(floor(sheet*49))-.5;
                color += fiber * _Paper.x * (1-smoothstep(.35,1.2,fiberAA)) * (_InkSettings.y>.5 ? .2 : 1);
                color *= 1 - crease * .16 * _Paper.y * resolved;
                color = lerp(color,color*.73+half3(.035,.027,.012),edge*_Paper.w*.55);
                return max(0,color);
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                half4 source=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                if (_StyleMode > 2.5) return FilmColor(source,uv);
                float rawDepth=SampleSceneDepth(uv);
                float depth=LinearEyeDepth(rawDepth,_ZBufferParams);
                float3 n=SampleSceneNormals(uv);
                float pixelWidth=max(.75,_Style.y*_ScaledScreenParams.y/1080);
                if(_InkSettings.y>.5)pixelWidth=1;
                float2 stepUV=pixelWidth/_ScaledScreenParams.xy;
                float dz=0,normalEdge=0;
                const float2 axes[4]={float2(1,0),float2(-1,0),float2(0,1),float2(0,-1)};
                [unroll]for(int i=0;i<4;i++)
                {
                    float2 p=saturate(uv+axes[i]*stepUV);
                    float d=LinearEyeDepth(SampleSceneDepth(p),_ZBufferParams);
                    dz=max(dz,abs(d-depth)/max(.25,min(d,depth)));
                    normalEdge=max(normalEdge,1-saturate(dot(n,SampleSceneNormals(p))));
                }
                float distanceFade=1-smoothstep(_Style.w*.45,_Style.w,depth);
                float edge=max(smoothstep(.025,.11,dz),smoothstep(.18,.55,normalEdge)*_Palette.w)*distanceFade;
                if (_StyleMode > 1.5)
                {
                    if (depth > _ProjectionParams.z * .99) return source;
                    half3 paperColor = PaperColor(source.rgb,uv,rawDepth,n,depth,edge);
                    return half4(lerp(source.rgb,paperColor,_Style.x),source.a);
                }
                // Constant screen-space fibres: no time noise, temporal jitter or camera-dependent random seed.
                float2 cells=min(_ScaledScreenParams.xy,float2(960,540));
                if(_InkSettings.y>.5)cells=min(cells,float2(320,180));
                float paper=Hash(floor(uv*cells))-.5;
                float inkVariation=.92+.08*Hash(floor(uv*float2(90,50)));
                float l=max(.0001,dot(source.rgb,float3(.2126,.7152,.0722)));
                float tone=log2(1+l);
                float bands=_Palette.y;
                float bandL=exp2(floor(tone*bands+.5)/bands)-1;
                half3 col=source.rgb*lerp(1,bandL/l,.66*_Style.x);
                col=lerp(dot(col,float3(.2126,.7152,.0722)).xxx,col,_Palette.x);
                col*=lerp(_ShadowTint.rgb,_HighlightTint.rgb,saturate(l));
                float hatch=pow(saturate(sin((uv.x+uv.y)*(_InkSettings.y>.5?200:640))*.5+.5),12)*_InkSettings.x*(1-saturate(l*2));
                col=max(0,col*(1-hatch)+paper*_Palette.z);
                col=lerp(col,_Ink.rgb,saturate(edge*_Style.z*inkVariation));
                return half4(lerp(source.rgb,col,_Style.x),source.a);
            }
            ENDHLSL
        }
    }
}
