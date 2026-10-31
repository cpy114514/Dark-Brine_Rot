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
