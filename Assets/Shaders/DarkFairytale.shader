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
            float4 _Style, _Palette, _InkSettings;
            half4 _Ink, _ShadowTint, _HighlightTint;
            float Hash(float2 p) { return frac(sin(dot(p, float2(127.1,311.7))) * 43758.5453); }
            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 uv=input.texcoord;
                half4 source=SAMPLE_TEXTURE2D_X(_BlitTexture,sampler_LinearClamp,uv);
                float depth=LinearEyeDepth(SampleSceneDepth(uv),_ZBufferParams);
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
