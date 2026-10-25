#ifndef DARK_BRINE_OCEAN_WAVES_INCLUDED
#define DARK_BRINE_OCEAN_WAVES_INCLUDED

// Shared by the water and its shoreline ribbon. Work in undisplaced world X/Z
// so neither surface resamples an already displaced crest.
float OceanHash21(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

// Value and analytic spatial gradient from the same four noise corners.
float3 OceanNoiseGradient(float2 p)
{
    float2 cell = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    float2 du = 6.0 * f * (1.0 - f);
    float a = OceanHash21(cell);
    float b = OceanHash21(cell + float2(1, 0));
    float c = OceanHash21(cell + float2(0, 1));
    float d = OceanHash21(cell + float2(1, 1));
    return float3(lerp(lerp(a, b, u.x), lerp(c, d, u.x), u.y),
        lerp(b - a, d - c, u.y) * du.x,
        lerp(c - a, d - b, u.x) * du.y);
}

float OceanNoise(float2 p) { return OceanNoiseGradient(p).x; }

float2 OceanDetailWeights(float distanceXZ)
{
    return float2(
        1.0 - smoothstep(_NearDetailDistance * 0.65, _NearDetailDistance, distanceXZ),
        1.0 - smoothstep(_MidDetailDistance * 0.75, _MidDetailDistance, distanceXZ));
}

float3 OceanGerstner(float4 wave, float4 motion, float weight, float2 weightGradient,
    float phaseOffset, float2 phaseOffsetGradient,
    float2 sampleXZ, float2 sampleDx, float2 sampleDz, float time,
    inout float3 tangent, inout float3 binormal)
{
    float2 direction = normalize(wave.xy);
    float k = TWO_PI / max(wave.w, 0.001);
    float phase = k * (dot(direction, sampleXZ) - motion.x * time) + phaseOffset;
    float sine, cosine;
    sincos(phase, sine, cosine);
    // A restrained second harmonic sharpens the crest and broadens the trough,
    // retaining the swell's period and mean sea level.
    float verticalShape = sine - 0.08 * (cosine * cosine - sine * sine);
    float verticalDerivative = cosine + 0.32 * sine * cosine;
    float2 phaseGradient = k * float2(dot(direction, sampleDx), dot(direction, sampleDz))
        + phaseOffsetGradient;
    float amplitude = wave.z * weight;
    float horizontalScale = min(motion.y, 0.95) * wave.z;
    float2 horizontalGradient = horizontalScale *
        (weightGradient * cosine - weight * sine * phaseGradient);
    float2 verticalGradient = wave.z *
        (weightGradient * verticalShape + weight * verticalDerivative * phaseGradient);
    tangent += float3(direction.x * horizontalGradient.x, verticalGradient.x,
        direction.y * horizontalGradient.x);
    binormal += float3(direction.x * horizontalGradient.y, verticalGradient.y,
        direction.y * horizontalGradient.y);
    return float3(direction.x * horizontalScale * weight * cosine, amplitude * verticalShape,
        direction.y * horizontalScale * weight * cosine);
}

void OceanEvaluateWaves(float2 baseXZ, float time, float2 detailWeights,
    out float3 displacement, out float3 normal, out float crest, out float primaryPhase)
{
    // Wave groups travel more slowly than their crests. Advect the existing
    // independent fields at group speed so sets build, cross and disperse in
    // seconds rather than remaining a nearly stationary sinusoidal grid.
    float3 broad = OceanNoiseGradient(baseXZ * 0.011 + float2(17.3, -8.1)
        + time * float2(-0.041, -0.027));
    float3 small = OceanNoiseGradient(baseXZ * 0.023 + float2(-12.7, 21.4)
        + time * float2(0.024, -0.043));
    float3 regional = OceanNoiseGradient(baseXZ * 0.0058 + float2(38.4, 5.7)
        + time * float2(-0.018, -0.012));
    float3 crossSea = OceanNoiseGradient(baseXZ * 0.039 + float2(-29.6, -17.2)
        + time * float2(-0.049, 0.031));
    float2 broadGradient = broad.yz * 0.011;
    float2 smallGradient = small.yz * 0.023;
    float2 regionalGradient = regional.yz * 0.0058;
    float2 crossGradient = crossSea.yz * 0.039;
    float warp = 19.0 * _WaveIrregularity;
    float2 sampleXZ = baseXZ + (float2(broad.x, small.x) - 0.5) * warp;
    float2 sampleDx = float2(1.0 + broadGradient.x * warp, smallGradient.x * warp);
    float2 sampleDz = float2(broadGradient.y * warp, 1.0 + smallGradient.y * warp);
    float swell = 1.0 + _WaveIrregularity * ((broad.x - 0.5)
        + (regional.x - 0.5) * 0.45 + (small.x - 0.5) * 0.25);
    float chop = 1.0 + _WaveIrregularity * ((small.x - 0.5) * 0.85
        - (broad.x - 0.5) * 0.25 + (crossSea.x - 0.5) * 0.60);
    float2 swellGradient = _WaveIrregularity * (broadGradient
        + regionalGradient * 0.45 + smallGradient * 0.25);
    float2 chopGradient = _WaveIrregularity * (smallGradient * 0.85
        - broadGradient * 0.25 + crossGradient * 0.60);
    // Independent, slowly drifting phase fields break the straight, equally
    // spaced Gerstner crests without making the water and shore foam disagree.
    float phaseA = _WaveIrregularity * (6.0 * (regional.x - 0.5) + 2.5 * (small.x - 0.5));
    float2 phaseAGradient = _WaveIrregularity * (6.0 * regionalGradient + 2.5 * smallGradient);
    float phaseB = _WaveIrregularity * (4.7 * (broad.x - 0.5) - 3.3 * (crossSea.x - 0.5));
    float2 phaseBGradient = _WaveIrregularity * (4.7 * broadGradient - 3.3 * crossGradient);
    float phaseC = _WaveIrregularity * (2.6 * (crossSea.x - 0.5) + 1.2 * (regional.x - 0.5));
    float2 phaseCGradient = _WaveIrregularity * (2.6 * crossGradient + 1.2 * regionalGradient);
    float phaseD = _WaveIrregularity * (2.4 * (small.x - 0.5) - 1.7 * (regional.x - 0.5));
    float2 phaseDGradient = _WaveIrregularity * (2.4 * smallGradient - 1.7 * regionalGradient);
    primaryPhase = TWO_PI / max(_Wave1.w, 0.001) *
        (dot(normalize(_Wave1.xy), sampleXZ) - _Wave1Motion.x * time) + phaseA;
    float3 tangent = float3(1, 0, 0);
    float3 binormal = float3(0, 0, 1);
    float3 a = OceanGerstner(_Wave1, _Wave1Motion, swell, swellGradient, phaseA, phaseAGradient,
        sampleXZ, sampleDx, sampleDz, time, tangent, binormal);
    float3 b = OceanGerstner(_Wave2, _Wave2Motion, chop, chopGradient, phaseB, phaseBGradient,
        sampleXZ, sampleDx, sampleDz, time + 1.9, tangent, binormal);
    float3 c = 0, d = 0;
    if (detailWeights.y > 0.001)
        c = OceanGerstner(_Wave3, _Wave3Motion, chop * detailWeights.y,
            chopGradient * detailWeights.y, phaseC, phaseCGradient,
            sampleXZ, sampleDx, sampleDz, time + 4.2, tangent, binormal);
    if (detailWeights.x > 0.001)
        d = OceanGerstner(_Wave4, _Wave4Motion, swell * detailWeights.x,
            swellGradient * detailWeights.x, phaseD, phaseDGradient,
            sampleXZ, sampleDx, sampleDz, time + 2.7, tangent, binormal);
    displacement = a + b + c + d;
    normal = normalize(cross(binormal, tangent));
    float compression = 1.0 - (tangent.x * binormal.z - tangent.z * binormal.x);
    crest = smoothstep(0.36, 0.72, a.y * 0.34 + b.y * 0.40 + c.y * 0.50 + d.y * 0.65)
        * lerp(0.30, 1.0, smoothstep(0.015, 0.13, compression));
}

void OceanEvaluateWaves(float2 baseXZ, float time, float2 detailWeights,
    out float3 displacement, out float3 normal, out float crest)
{
    float primaryPhase;
    OceanEvaluateWaves(baseXZ, time, detailWeights, displacement, normal, crest, primaryPhase);
}

#endif
