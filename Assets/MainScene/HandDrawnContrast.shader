Shader "UI/Hand Drawn Contrast"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _Contrast ("Contrast", Range(1,3)) = 1.6
        _Pivot ("Contrast Pivot", Range(0,1)) = 0.65
        _WhiteBalance ("White Balance RGB Gain", Vector) = (1,1,1,0)
        _Saturation ("Saturation", Range(0,2)) = 1
        _LocalCorrection ("Local Paper White Balance", Float) = 0
        _Local00 ("Bottom Left RGB Gain", Vector) = (1,1,1,0)
        _Local10 ("Bottom Middle RGB Gain", Vector) = (1,1,1,0)
        _Local20 ("Bottom Right RGB Gain", Vector) = (1,1,1,0)
        _Local01 ("Middle Left RGB Gain", Vector) = (1,1,1,0)
        _Local11 ("Center RGB Gain", Vector) = (1,1,1,0)
        _Local21 ("Middle Right RGB Gain", Vector) = (1,1,1,0)
        _Local02 ("Top Left RGB Gain", Vector) = (1,1,1,0)
        _Local12 ("Top Middle RGB Gain", Vector) = (1,1,1,0)
        _Local22 ("Top Right RGB Gain", Vector) = (1,1,1,0)
        _AlignmentEnabled ("Align Colored Drawing", Float) = 0
        _AlignedTex ("Colored Drawing", 2D) = "white" {}
        _WarpAffineX ("UV X Affine", Vector) = (0,1,0,0)
        _WarpAffineY ("UV Y Affine", Vector) = (0,0,1,0)
        _Warp0 ("Alignment Landmark 0", Vector) = (0,0,0,0)
        _Warp1 ("Alignment Landmark 1", Vector) = (0,0,0,0)
        _Warp2 ("Alignment Landmark 2", Vector) = (0,0,0,0)
        _Warp3 ("Alignment Landmark 3", Vector) = (0,0,0,0)
        _Warp4 ("Alignment Landmark 4", Vector) = (0,0,0,0)
        _Warp5 ("Alignment Landmark 5", Vector) = (0,0,0,0)
        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True" }
        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            struct appdata
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 localPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };
            sampler2D _MainTex;
            sampler2D _AlignedTex;
            float _AlignmentEnabled;
            float4 _WarpAffineX, _WarpAffineY, _Warp0, _Warp1, _Warp2, _Warp3, _Warp4, _Warp5;
            float4 _MainTex_ST, _Color, _TextureSampleAdd, _ClipRect;
            float _Contrast, _Pivot;
            float4 _WhiteBalance;
            float _Saturation;
            float _LocalCorrection;
            float4 _Local00, _Local10, _Local20, _Local01, _Local11, _Local21, _Local02, _Local12, _Local22;
            float3 PaperGain(float2 uv)
            {
                // Smooth low-frequency lighting compensation in artwork UVs, not screen space.
                float2 grid = saturate((uv - .2) / .6) * 2;
                float2 blend = smoothstep(0, 1, frac(grid));
                blend = lerp(blend, float2(1, 1), step(2, grid));
                float3 bottom = grid.x < 1 ? lerp(_Local00.rgb, _Local10.rgb, blend.x) : lerp(_Local10.rgb, _Local20.rgb, blend.x);
                float3 middle = grid.x < 1 ? lerp(_Local01.rgb, _Local11.rgb, blend.x) : lerp(_Local11.rgb, _Local21.rgb, blend.x);
                float3 top = grid.x < 1 ? lerp(_Local02.rgb, _Local12.rgb, blend.x) : lerp(_Local12.rgb, _Local22.rgb, blend.x);
                return grid.y < 1 ? lerp(bottom, middle, blend.y) : lerp(middle, top, blend.y);
            }
            float2 LandmarkWarp(float2 uv, float4 landmark)
            {
                float2 delta = uv - landmark.xy;
                float radius2 = dot(delta, delta);
                return landmark.zw * radius2 * log(max(radius2, 0.000001));
            }
            v2f vert(appdata input)
            {
                v2f output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.localPosition = input.vertex;
                output.vertex = UnityObjectToClipPos(input.vertex);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _Color;
                return output;
            }
            float4 frag(v2f input) : SV_Target
            {
                float4 color = tex2D(_MainTex, input.uv) + _TextureSampleAdd;
                if (_AlignmentEnabled > 0.5)
                {
                    // Display-only registration: original texture, silhouette and hit area stay intact.
                    float3 basis = float3(1, input.uv);
                    float2 alignedUV = float2(dot(_WarpAffineX.xyz, basis), dot(_WarpAffineY.xyz, basis));
                    alignedUV += LandmarkWarp(input.uv, _Warp0) + LandmarkWarp(input.uv, _Warp1)
                        + LandmarkWarp(input.uv, _Warp2) + LandmarkWarp(input.uv, _Warp3)
                        + LandmarkWarp(input.uv, _Warp4) + LandmarkWarp(input.uv, _Warp5);
                    float4 colored = tex2D(_AlignedTex, saturate(alignedUV));
                    float inside = step(0, alignedUV.x) * step(alignedUV.x, 1)
                        * step(0, alignedUV.y) * step(alignedUV.y, 1);
                    color.rgb = lerp(color.rgb, colored.rgb, saturate(colored.a * 10) * inside);
                }
                // Adjust display-referred RGB; never change the drawing's alpha.
                #ifndef UNITY_COLORSPACE_GAMMA
                    color.rgb = LinearToGammaSpace(color.rgb);
                #endif
                color.rgb = saturate(color.rgb * _WhiteBalance.rgb);
                if (_LocalCorrection > .5) color.rgb = saturate(color.rgb * PaperGain(input.uv));
                float luminance = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                color.rgb = lerp(luminance.xxx, color.rgb, _Saturation);
                color.rgb = saturate((color.rgb - _Pivot) * _Contrast + _Pivot);
                #ifndef UNITY_COLORSPACE_GAMMA
                    color.rgb = GammaToLinearSpace(color.rgb);
                #endif
                color *= input.color;
                #ifdef UNITY_UI_CLIP_RECT
                    color.a *= UnityGet2DClipping(input.localPosition.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a - 0.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
