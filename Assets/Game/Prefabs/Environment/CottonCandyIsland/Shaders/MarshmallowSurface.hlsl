#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

CBUFFER_START(UnityPerMaterial)
    half4 _Vanilla;
    half4 _Strawberry;
    half4 _Lavender;
    half4 _PressedColor;
    float _PatchScale;
    float _FiberScale;
    float _CloudMode;
CBUFFER_END

// Per-renderer state. Never global: other islands cannot inherit these impressions.
float4 _CandyContacts[12]; // xyz = world contact, w = radius
float4 _CandyContactData[12]; // birth time, intensity, lifetime, reserved
float4 _CandyPressure; // xyz = current support, w = pressure depth in metres
float _CandyClock;
int _CandyContactCount;

float CandyHash(float2 p)
{
    p = frac(p * float2(.1031, .11369));
    p += dot(p, p.yx + 19.19);
    return frac(p.x * p.y);
}
float CandyNoise(float2 p)
{
    float2 i = floor(p), f = frac(p);
    f = f * f * (3 - 2 * f);
    return lerp(lerp(CandyHash(i), CandyHash(i + float2(1, 0)), f.x),
        lerp(CandyHash(i + float2(0, 1)), CandyHash(i + 1), f.x), f.y);
}

// Height, pressure blush, rebound wave. Restrict each impression vertically too,
// so a footprint on the summit cannot deform another layer below it.
float3 CandyResponse(float3 p)
{
    float3 response = 0;
    if (_CloudMode > .5) return response;
    float2 pressureDelta = p.xz - _CandyPressure.xz;
    float pressureFalloff = exp2(-dot(pressureDelta, pressureDelta) * .28);
    float pressureLayer = saturate(1 - abs(p.y - _CandyPressure.y) / 3.5);
    response.x -= _CandyPressure.w * pressureFalloff * pressureLayer;
    response.y += saturate(_CandyPressure.w * 3.2) * pressureFalloff * pressureLayer;
    [loop] for (int i = 0; i < _CandyContactCount; i++)
    {
        float age = _CandyClock - _CandyContactData[i].x;
        float lifetime = _CandyContactData[i].z;
        float radius = _CandyContacts[i].w;
        float2 delta = p.xz - _CandyContacts[i].xz;
        float d2 = dot(delta, delta);
        if (age < 0 || age > lifetime || d2 > (radius + 6) * (radius + 6)) continue;
        float layer = saturate(1 - abs(p.y - _CandyContacts[i].y) / 4);
        float energy = _CandyContactData[i].y * layer;
        float recovery = (1 - smoothstep(.12, lifetime, age));
        float bowl = exp2(-d2 / (radius * radius) * 3);
        float distance = sqrt(d2 + .0001);
        float ring = exp2(-pow((distance - age * 5.2) / .8, 2) * 2) * exp2(-age * 2.8);
        response.x += (-.30 * bowl * recovery + .095 * ring) * energy;
        response.y += bowl * recovery * energy;
        response.z += ring * energy;
    }
    response.x = clamp(response.x, -.48, .12);
    return response;
}

struct CandyAttributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
struct CandyVaryings
{
    float4 positionCS : SV_POSITION;
    float3 positionWS : TEXCOORD0;
    half3 normalWS : TEXCOORD1;
    float3 restWS : TEXCOORD2;
    half fog : TEXCOORD3;
};
CandyVaryings CandyVertex(CandyAttributes input)
{
    CandyVaryings o;
    o.restWS = TransformObjectToWorld(input.positionOS.xyz);
    o.normalWS = TransformObjectToWorldNormal(input.normalOS);
    o.positionWS = o.restWS;
    o.positionWS.y += CandyResponse(o.restWS).x * smoothstep(.15, .75, o.normalWS.y);
    o.positionCS = TransformWorldToHClip(o.positionWS);
    o.fog = ComputeFogFactor(o.positionCS.z);
    return o;
}
