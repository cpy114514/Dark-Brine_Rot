Shader "UI/DarkBrine Pixel Style"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite Texture",2D)="white"{}
        _Color("Tint",Color)=(1,1,1,1)
        _StencilComp("Stencil Comparison",Float)=8
        _Stencil("Stencil ID",Float)=0
        _StencilOp("Stencil Operation",Float)=0
        _StencilWriteMask("Stencil Write Mask",Float)=255
        _StencilReadMask("Stencil Read Mask",Float)=255
        _ColorMask("Color Mask",Float)=15
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip("Use Alpha Clip",Float)=0
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" "CanUseSpriteAtlas"="True"}
        Stencil {Ref [_Stencil] Comp [_StencilComp] Pass [_StencilOp] ReadMask [_StencilReadMask] WriteMask [_StencilWriteMask]}
        Cull Off Lighting Off ZWrite Off ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"
            #include "../../Shaders/PixelStyleUI.cginc"
            struct appdata {float4 vertex:POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;UNITY_VERTEX_INPUT_INSTANCE_ID};
            struct v2f {float4 vertex:SV_POSITION;float4 color:COLOR;float2 uv:TEXCOORD0;float4 local:TEXCOORD1;float4 pixelScreen:TEXCOORD2;UNITY_VERTEX_OUTPUT_STEREO};
            sampler2D _MainTex;
            float4 _MainTex_ST,_Color,_TextureSampleAdd,_ClipRect;
            v2f vert(appdata input)
            {
                v2f output;UNITY_SETUP_INSTANCE_ID(input);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.local=input.vertex;output.vertex=PixelUIVertex(UnityObjectToClipPos(input.vertex),input.uv,PixelUIEnabled(_TextureSampleAdd));
                output.pixelScreen=ComputeScreenPos(output.vertex);
                output.uv=TRANSFORM_TEX(input.uv,_MainTex);output.color=input.color*_Color;return output;
            }
            float4 frag(v2f input):SV_Target
            {
                float2 delta=PixelUIDelta(input.pixelScreen,PixelUIEnabled(_TextureSampleAdd));
                float2 uv=PixelUIUV(input.uv,delta);float4 color=(tex2D(_MainTex,uv)+_TextureSampleAdd)*PixelUIColor(input.color,delta);
                #ifdef UNITY_UI_CLIP_RECT
                    color.a*=UnityGet2DClipping(PixelUIUV(input.local.xy,delta),_ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                    clip(color.a-.001);
                #endif
                return color;
            }
            ENDCG
        }
    }
}
