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
            float4 _Comic; // ink weight, contrast, screentone, shadow ink
            half4 _Ink, _ShadowTint, _HighlightTint;
            TEXTURE2D_X_FLOAT(_ComicDepthTexture);
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            float Luma(half3 c) { return max(0,dot(c,float3(.2126,.7152,.0722))); }
            float ComicDepth(float2 uv) { return LinearEyeDepth(SAMPLE_TEXTURE2D_X(_ComicDepthTexture,sampler_PointClamp,saturate(uv)).r,_ZBufferParams); }
            // Screen-anchored print cells: no random frame seed or time jitter.
            float PrintDots(float2 uv, float coverage)
            {
                float spacing = _InkSettings.y>.5 ? 3 : max(3.5,6*_ScaledScreenParams.y/1080);
                float2 p = uv*_ScaledScreenParams.xy/spacing;
                p = float2(p.x+p.y,p.y-p.x)*.70710678;
                float2 cell = frac(p)-.5;
                float distance = length(cell);
                float radius = sqrt(saturate(coverage)*.31830989);
                float aa = max(fwidth(distance)*.65,.025);
                return (1-smoothstep(radius-aa,radius+aa,distance))*step(.001,coverage);
            }
            float ArtNoise(float2 p)
            {
                float2 cell=floor(p), f=frac(p);f=f*f*(3-2*f);
                return lerp(lerp(Hash(cell),Hash(cell+float2(1,0)),f.x),lerp(Hash(cell+float2(0,1)),Hash(cell+1),f.x),f.y);
            }
            half3 PaintSample(float2 uv,float2 offset,float depth,half3 center)
            {
                float2 at=saturate(uv+offset);
                float other=ComicDepth(at);
                float same=1-smoothstep(.04+depth*.002,.18+depth*.01,abs(other-depth));
                return lerp(center,SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,at).rgb,same);
            }
            half4 InkWashColor(half4 source,float2 uv)
            {
                float depth=ComicDepth(uv);
                float pixelScale=max(.65,_ScaledScreenParams.y/1080);
                float radius=_InkSettings.y>.5?1:lerp(1,4.5,_Comic.x)*pixelScale;
                float2 stepUV=radius/_ScaledScreenParams.xy;
                half3 right=PaintSample(uv,float2(stepUV.x,0),depth,source.rgb);
                half3 left=PaintSample(uv,float2(-stepUV.x,0),depth,source.rgb);
                half3 up=PaintSample(uv,float2(0,stepUV.y),depth,source.rgb);
                half3 down=PaintSample(uv,float2(0,-stepUV.y),depth,source.rgb);
                half3 wet=(source.rgb*2+right+left+up+down)/6;
                float luminance=saturate(Luma(lerp(source.rgb,wet,.85*_Comic.x)));
                float value=lerp(sqrt(luminance),pow(max(.0001,luminance),.28),1-smoothstep(.03,.18,luminance));
                float gradient=abs(Luma(right)-Luma(left))+abs(Luma(up)-Luma(down));
                float4 neighbors=float4(ComicDepth(uv+float2(stepUV.x,0)),ComicDepth(uv-float2(stepUV.x,0)),ComicDepth(uv+float2(0,stepUV.y)),ComicDepth(uv-float2(0,stepUV.y)));
                float curvature=(abs(neighbors.x+neighbors.y-2*depth)+abs(neighbors.z+neighbors.w-2*depth))/max(.4,depth);
                float boundary=max(smoothstep(.04,.25,curvature),smoothstep(.05,.3,gradient)*.4);
                float2 pixel=uv*_ScaledScreenParams.xy;
                float brush=ArtNoise(pixel/float2(13,3));
                float bloom=ArtNoise(pixel/32);
                float wash=pow(saturate(1-value*1.05),.8)*1.35;
                wash=lerp(wash,floor(wash*5+.5)/5,.4);
                wash*=lerp(1,.65+.6*brush,_Comic.y);
                wash+=boundary*lerp(.16,.42,_Comic.x)*(.65+.35*bloom);
                float mist=smoothstep(35,190,depth)*_Comic.w;
                wash*=1-mist*.6;
                float sky=smoothstep(.52,.8,uv.y)*step(_ProjectionParams.z*.98,depth);
                wash*=1-sky*.82;
                half3 paper=half3(.96,.938,.88);
                half3 ink=half3(.035,.052,.06);
                half3 color=lerp(paper,ink,saturate(wash));
                float grain=ArtNoise(pixel*.7)-.5;
                color+=grain*_Comic.z*(_InkSettings.y>.5?.003:.025);
                return half4(lerp(source.rgb,color,_Style.x),source.a);
            }
            half3 OilPalette(half3 paint)
            {
                float lum=max(.0001,Luma(paint)), rich=_Comic.w;
                half3 color=lerp(lum.xxx,paint,lerp(.92,1.4,rich));
                float blue=saturate((paint.b-paint.r)*4);
                float green=saturate((paint.g-max(paint.r,paint.b))*5);
                color=lerp(color,lum*half3(.45,.7,1.55),blue*rich*.65);
                color=lerp(color,lum*half3(1.45,1.22,.40),green*rich*.55);
                return color*lerp(half3(.88,.86,1.07),half3(1.12,1.04,.86),sqrt(saturate(lum)));
            }
            half4 VanGoghColor(half4 source,float2 uv)
            {
                float depth=ComicDepth(uv);
                float2 resolution=_ScaledScreenParams.xy, pixel=uv*resolution;
                float size=_InkSettings.y>.5?lerp(1.2,2.8,_Comic.x):lerp(4,18,_Comic.x)*max(.65,resolution.y/1080);
                float spacing=size*1.35;
                float2 grid=floor(pixel/spacing);
                half3 total=OilPalette(source.rgb)*.12;
                float sum=.12;
                // Local dabs rotate about their own centers. Rotating absolute
                // pixel coordinates produced dense interference rings in the rejected prototype.
                [unroll]for(int y=-1;y<=1;y++)
                [unroll]for(int x=-1;x<=1;x++)
                {
                    float2 cell=grid+float2(x,y);
                    float seed=Hash(cell), seed2=Hash(cell+17.7);
                    float2 center=(cell+.5+float2(seed-.5,seed2-.5)*.7)*spacing;
                    float2 at=saturate(center/resolution);
                    float2 whorl=(at-float2(.72,.74))*float2(resolution.x/resolution.y,1);
                    float polar=atan2(whorl.y,whorl.x), radial=length(whorl);
                    float sky=step(_ProjectionParams.z*.98,depth)*smoothstep(.5,.72,uv.y);
                    float angle=lerp(.25+(ArtNoise(at*9)-.5)*1.8,polar+1.5708,_Comic.y*sky);
                    angle+=(seed2-.5)*.45;
                    float2 direction=float2(cos(angle),sin(angle)), side=float2(-direction.y,direction.x);
                    float2 local=pixel-center;
                    float along=dot(local,direction)/(size*2.1), across=dot(local,side)/(size*.65);
                    float shape=along*along+across*across;
                    float weight=pow(saturate(1-shape),2);
                    half3 dab=PaintSample(uv,at-uv,depth,source.rgb);
                    // Color-range rejection protects eyes and small face marks
                    // while broad, similar-color areas keep the coarse dabs.
                    float similar=1-smoothstep(.06,.28,length(dab-source.rgb));
                    dab=lerp(source.rgb,dab,similar*.9);
                    half3 paint=OilPalette(dab);
                    float groove=sin(across*8+seed*6.2831853)*.5+.5;
                    paint*=1+(groove-.5)*.28*_Comic.z+(seed-.5)*.35*_Comic.z;
                    paint+=half3(.035,.028,.013)*groove*_Comic.z*sqrt(saturate(Luma(dab)));
                    float curl=sin(radial*20-polar*2+ArtNoise(at*6))*.5+.5;
                    paint+=half3(.65,.5,.16)*pow(curl,3)*sky*_Comic.y*.14*sqrt(saturate(Luma(dab)));
                    total+=paint*weight;sum+=weight;
                }
                half3 color=total/sum;
                half3 result=lerp(source.rgb,max(0,color),_Style.x);
                result*=max(1,Luma(source.rgb)*.78/max(.0001,Luma(result)));
                return half4(result,source.a);
            }
            half4 ComicColor(half4 source,float2 uv)
            {
                bool manga = _StyleMode>2.5;
                float width = _InkSettings.y>.5 ? 1 : lerp(.8,2.5,_Comic.x)*_ScaledScreenParams.y/1080;
                float2 stepUV = max(.75,width)/_ScaledScreenParams.xy;
                float depth = ComicDepth(uv);
                float4 depths = float4(ComicDepth(uv+float2(stepUV.x,0)),ComicDepth(uv-float2(stepUV.x,0)),ComicDepth(uv+float2(0,stepUV.y)),ComicDepth(uv-float2(0,stepUV.y)));
                half3 right = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,saturate(uv+float2(stepUV.x,0))).rgb;
                half3 left = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,saturate(uv-float2(stepUV.x,0))).rgb;
                half3 up = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,saturate(uv+float2(0,stepUV.y))).rgb;
                half3 down = SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,saturate(uv-float2(0,stepUV.y))).rgb;
                float curvature = (abs(depths.x+depths.y-2*depth)+abs(depths.z+depths.w-2*depth))/max(.4,depth);
                float silhouette = smoothstep(.035,.16,curvature);
                float l = Luma(source.rgb);
                float gradient = (abs(Luma(right)-Luma(left))+abs(Luma(up)-Luma(down)))/max(.2,l+.2);
                float detail = smoothstep(.32,.85,gradient);
                float distanceFade = 1-smoothstep(_Style.w*.45,_Style.w,depth);
                // Far vegetation micro-edges fade while color/tones remain fully stylized.
                float edge = max(silhouette,detail*.65)*distanceFade;
                float ink = edge*lerp(.3,.95,_Comic.x);
                float4 weights = 1-smoothstep(.03+depth*.001,.12+depth*.006,abs(depths-depth));
                half3 paint = (source.rgb*3+right*weights.x+left*weights.y+up*weights.z+down*weights.w)/(3+dot(weights,1.0.xxxx));
                paint = lerp(source.rgb,paint,.7*_Comic.y);
                float lum = max(.0001,Luma(paint));
                float perceptual = sqrt(saturate(lum));
                float contrast = lerp(.85,1.65,_Comic.y);
                float tone = manga ? saturate((perceptual-.42)*contrast+.48) : saturate((perceptual-.5)*contrast+.5);
                float bands = manga ? 4 : lerp(7,3,_Comic.y);
                float quantized = floor(tone*bands+.5)/bands;
                float dots = PrintDots(uv,saturate((.78-tone)*.7)*_Comic.z);
                half4 result = source;
                if(manga)
                {
                    float gray = lerp(.12,.97,smoothstep(.05,.9,quantized));
                    float black = (1-smoothstep(.08,.28,tone))*_Comic.w;
                    float2 pixel = uv*_ScaledScreenParams.xy;
                    float hatchSpacing = _InkSettings.y>.5 ? 5 : max(5,9*_ScaledScreenParams.y/1080);
                    float hatchCoordinate = (pixel.x-pixel.y)/hatchSpacing;
                    float lineDistance = abs(frac(hatchCoordinate)-.5);
                    float hatch = 1-smoothstep(.045,.045+max(fwidth(hatchCoordinate)*.6,.03),lineDistance);
                    hatch *= (1-smoothstep(.2,.55,tone))*_Comic.z*.35;
                    gray = lerp(gray,.012,saturate(dots*.8+black+ink+hatch));
                    // Minimum intensity retains a grayscale base; no scene hue leaks back.
                    gray = lerp(saturate(lum),gray,_Style.x);
                    result = half4(gray.xxx,source.a);
                }
                else
                {
                half3 color = paint/lum*max(.012,quantized*quantized);
                float satL = Luma(color);
                color = lerp(satL.xxx,color,.94);
                color *= lerp(half3(.86,.91,1.04),half3(1.06,1.02,.91),tone);
                float shadow = (1-smoothstep(.08,.4,tone))*_Comic.w*.42;
                color = lerp(color,half3(.015,.022,.04),saturate(ink+shadow+dots*.32));
                // Keep low-light face/terrain information instead of crushing it
                // into the lowest printed band at night.
                float inkExposure = lerp(.3,1,smoothstep(.12,.38,perceptual));
                half3 finalColor = lerp(source.rgb,max(0,color),_Style.x*inkExposure);
                float nightDetail = 1-smoothstep(.15,.4,l);
                float minimumLuma = l*lerp(.2,.88,nightDetail);
                finalColor *= max(1,minimumLuma/max(.0001,Luma(finalColor)));
                result = half4(finalColor,source.a);
                }
                return result;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                half4 source=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                if (_StyleMode > 4.5) return VanGoghColor(source,uv);
                if (_StyleMode > 3.5) return InkWashColor(source,uv);
                if (_StyleMode > 1.5) return ComicColor(source,uv);
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
