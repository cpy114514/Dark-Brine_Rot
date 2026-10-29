#ifndef DARK_BRINE_ENVIRONMENT_VISUAL_STYLE
#define DARK_BRINE_ENVIRONMENT_VISUAL_STYLE
// Runtime globals, deliberately outside UnityPerMaterial: weather retains ownership of its palette.
float4 _EnvironmentStyle; // intensity, saturation, tonal bands, paper grain
half4 _EnvironmentShadow, _EnvironmentHighlight, _EnvironmentInk;
float _PixelStyleUI;
float _EnvironmentMode;
float4 _EnvironmentPaper;

half3 StyleEnvironmentColor(half3 source, float2 screenUV, float inkWeight)
{
    if (_EnvironmentStyle.x <= 0) return source;
    if (_EnvironmentMode > 1.5)
    {
        float l = max(.001,dot(source,float3(.2126,.7152,.0722)));
        float band = floor(l*5+.5)/5;
        half3 paper = source * lerp(1,band/l,.42);
        paper = lerp(dot(paper,float3(.2126,.7152,.0722)).xxx,paper,.88);
        paper *= lerp(half3(.84,.97,.97),half3(1.08,1.02,.88),saturate(l));
        paper = lerp(paper,half3(.96,.91,.78),.045);
        return lerp(source,max(0,paper),_EnvironmentStyle.x);
    }
    float luminance = max(.0001, dot(source, float3(.2126, .7152, .0722)));
    float bands = max(2, _EnvironmentStyle.z);
    float bandL = exp2(floor(log2(1 + luminance) * bands + .5) / bands) - 1;
    half3 color = source * lerp(1, bandL / luminance, .72);
    color = lerp(dot(color, half3(.2126, .7152, .0722)).xxx, color, _EnvironmentStyle.y);
    color *= lerp(_EnvironmentShadow.rgb, _EnvironmentHighlight.rgb, saturate(luminance));
    color = lerp(color, _EnvironmentInk.rgb, saturate(inkWeight));
    float2 cells = min(_ScaledScreenParams.xy, float2(960, 540));
    float grain = frac(sin(dot(floor(screenUV * cells), float2(127.1, 311.7))) * 43758.5453) - .5;
    color = max(0, color + grain * _EnvironmentStyle.w * (1 - step(.5, _PixelStyleUI)));
    return lerp(source, color, _EnvironmentStyle.x);
}
#endif
