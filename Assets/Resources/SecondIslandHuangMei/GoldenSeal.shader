Shader "DarkBrine/Huang Mei Golden Seal"
{
    Properties
    {
        [HDR] _Color ("Tint", Color) = (1,.65,.16,1)
        _MainTex ("Soft energy texture", 2D) = "white" {}
        _Style ("0 edge, 1 ground, 2 dust", Float) = 0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;
                float _Style;
            CBUFFER_END
            TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex);
            struct Attributes { float4 positionOS : POSITION; half4 color : COLOR; float2 uv:TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; half4 color : COLOR; float2 uv:TEXCOORD0; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.color = input.color;
                output.uv=input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color=input.color*_Color;
                half4 energyTex=SAMPLE_TEXTURE2D(_MainTex,sampler_MainTex,input.uv);
                half energySample=energyTex.r*energyTex.a;
                if(_Style>1.5)color.a*=energySample;
                else if(_Style>.5)
                {
                    // Fine etched spokes keep danger zones readable through the effect.
                    float spoke=pow(abs(sin(input.uv.x*301.5929)),28);
                    float rings=pow(abs(sin(input.uv.y*31.4159)),18);
                    color.a*=.17+.60*max(spoke,rings);
                }
                else
                {
                    float feather=smoothstep(0,.16,input.uv.y)*smoothstep(0,.16,1-input.uv.y);
                    color.rgb*=1+.45*pow(saturate(1-abs(input.uv.y-.5)*2),3);
                    color.a*=feather;
                }
                return color;
            }
            ENDHLSL
        }
    }
}
