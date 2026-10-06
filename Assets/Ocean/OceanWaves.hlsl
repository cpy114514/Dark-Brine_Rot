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
    out float3 displacement, out float3 normal, out float crest)
{
    float3 broad = OceanNoiseGradient(baseXZ * 0.011 + float2(17.3, -8.1)
        + time * float2(0.0017, -0.0011));
    float3 small = OceanNoiseGradient(baseXZ * 0.023 + float2(-12.7, 21.4)
        + time * float2(-0.0013, 0.0019));
    float3 regional = OceanNoiseGradient(baseXZ * 0.0058 + float2(38.4, 5.7)
        + time * float2(0.0008, -0.0005));
    float3 crossSea = OceanNoiseGradient(baseXZ * 0.039 + float2(-29.6, -17.2)
        + time * float2(-0.0021, 0.0014));
    float2 broadGradient = broad.yz * 0.011;
    float2 smallGradient = small.yz * 0.023;
    float2 regionalGradient = regional.yz * 0.0058;
    float2 crossGradient = crossSea.yz * 0.039;
    float warp = 19.0 * _WaveIrregularity;
    float2 sampleXZ = baseXZ + (float2(broad.x, small.x) - 0.5) * warp;
    float2 sampleDx = float2(1.0 + broadGradient.x * warp, smallGradient.x * warp);
    float2 sampleDz = float2(broadGradient.y * warp, 1.0 + smallGradient.y * warp);
    float swell = 1.0 + _WaveIrregularity * ((broad.x - 0.5) * 0.85
        + (regional.x - 0.5) * 0.48 + (small.x - 0.5) * 0.25);
    float chop = 1.0 + _WaveIrregularity * ((small.x - 0.5) * 0.70
        - (broad.x - 0.5) * 0.20 + (crossSea.x - 0.5) * 0.42);
    float2 swellGradient = _WaveIrregularity * (broadGradient * 0.85
        + regionalGradient * 0.48 + smallGradient * 0.25);
    float2 chopGradient = _WaveIrregularity * (smallGradient * 0.70
        - broadGradient * 0.20 + crossGradient * 0.42);
    // Independent, slowly drifting phase fields break the straight, equally
    // spaced Gerstner crests without making the water and shore foam disagree.
    float phaseA = _WaveIrregularity * (4.0 * (regional.x - 0.5) + 1.7 * (small.x - 0.5));
    float2 phaseAGradient = _WaveIrregularity * (4.0 * regionalGradient + 1.7 * smallGradient);
    float phaseB = _WaveIrregularity * (2.9 * (broad.x - 0.5) - 2.2 * (crossSea.x - 0.5));
    float2 phaseBGradient = _WaveIrregularity * (2.9 * broadGradient - 2.2 * crossGradient);
    float phaseC = _WaveIrregularity * (1.6 * (crossSea.x - 0.5) + 0.8 * (regional.x - 0.5));
    float2 phaseCGradient = _WaveIrregularity * (1.6 * crossGradient + 0.8 * regionalGradient);
    float phaseD = _WaveIrregularity * (1.45 * (small.x - 0.5) - 1.2 * (regional.x - 0.5));
    float2 phaseDGradient = _WaveIrregularity * (1.45 * smallGradient - 1.2 * regionalGradient);
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

#endif
