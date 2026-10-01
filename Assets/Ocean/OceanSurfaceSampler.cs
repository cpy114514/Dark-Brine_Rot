using UnityEngine;

/// <summary>CPU counterpart of OceanWaves.hlsl for buoyancy and camera water contact.</summary>
public static class OceanSurfaceSampler
{
    public static float Height(OceanWorld ocean, Vector3 position, Camera camera)
        => SampleHeight(ocean, position, camera, Time.time);

    // Ship buoyancy passes its simulation clock; character swimming passes
    // its own camera. Both use the same wave implementation.
    public static float Height(OceanWorld ocean, Vector3 position, float gameTime)
        => SampleHeight(ocean, position, Camera.main, gameTime);

    static float SampleHeight(OceanWorld ocean, Vector3 position, Camera camera, float gameTime)
    {
        if (ocean == null) return 0f;
        float time = gameTime * ocean.waveMotionSpeed;
        Vector2 target = new Vector2(position.x, position.z);
        Vector2 point = target;
        Vector3 displacement = Vector3.zero;
        // Invert horizontal Gerstner displacement so height is sampled at the
        // swimmer's actual world XZ, not an undisplaced mesh vertex.
        for (int i = 0; i < 3; i++)
        {
            displacement = Displacement(ocean, point, time, camera);
            point = target - new Vector2(displacement.x, displacement.z);
        }
        return ocean.oceanHeight + Displacement(ocean, point, time, camera).y;
    }

    static Vector3 Displacement(OceanWorld ocean, Vector2 p, float time, Vector2 detail)
    {
        float broad = Noise(p * 0.011f + new Vector2(17.3f, -8.1f) + time * new Vector2(0.0017f, -0.0011f));
        float small = Noise(p * 0.023f + new Vector2(-12.7f, 21.4f) + time * new Vector2(-0.0013f, 0.0019f));
        float regional = Noise(p * 0.0058f + new Vector2(38.4f, 5.7f) + time * new Vector2(0.0008f, -0.0005f));
        float cross = Noise(p * 0.039f + new Vector2(-29.6f, -17.2f) + time * new Vector2(-0.0021f, 0.0014f));
        float irregularity = ocean.waveIrregularity;
        Vector2 sample = p + (new Vector2(broad, small) - Vector2.one * 0.5f) * (19f * irregularity);
        float swell = 1f + irregularity * ((broad - 0.5f) * 0.85f + (regional - 0.5f) * 0.48f + (small - 0.5f) * 0.25f);
        float chop = 1f + irregularity * ((small - 0.5f) * 0.70f - (broad - 0.5f) * 0.20f + (cross - 0.5f) * 0.42f);
        float phaseA = irregularity * (4f * (regional - 0.5f) + 1.7f * (small - 0.5f));
        float phaseB = irregularity * (2.9f * (broad - 0.5f) - 2.2f * (cross - 0.5f));
        float phaseC = irregularity * (1.6f * (cross - 0.5f) + 0.8f * (regional - 0.5f));
        float phaseD = irregularity * (1.45f * (small - 0.5f) - 1.2f * (regional - 0.5f));
        return Wave(ocean.wave1, sample, time, swell, phaseA) + Wave(ocean.wave2, sample, time + 1.9f, chop, phaseB) +
            Wave(ocean.wave3, sample, time + 4.2f, chop * detail.y, phaseC) +
            Wave(ocean.wave4, sample, time + 2.7f, swell * detail.x, phaseD);
    }

    static Vector2 DetailWeights(OceanWorld ocean, Vector2 p, Camera camera)
    {
        ocean.GetShaderDetailDistances(out float near, out float mid);
        float distance = camera == null ? 0f : Vector2.Distance(p, new Vector2(camera.transform.position.x, camera.transform.position.z));
        return new Vector2(1f - Smooth(near * 0.65f, near, distance),
            1f - Smooth(mid * 0.75f, mid, distance));
    }

    static Vector3 Displacement(OceanWorld ocean, Vector2 p, float time, Camera camera) =>
        Displacement(ocean, p, time, DetailWeights(ocean, p, camera));

    static Vector3 Wave(OceanGerstnerWave wave, Vector2 p, float time, float weight, float offset)
    {
        if (wave.amplitude <= 0f || wave.wavelength <= 0f) return Vector3.zero;
        Vector2 direction = wave.direction.sqrMagnitude < 0.0001f ? Vector2.right : wave.direction.normalized;
        float phase = Mathf.PI * 2f / Mathf.Max(0.001f, wave.wavelength) * (Vector2.Dot(direction, p) - wave.speed * time) + offset;
        float horizontal = Mathf.Min(wave.steepness, 0.95f) * wave.amplitude * weight * Mathf.Cos(phase);
        return new Vector3(direction.x * horizontal, wave.amplitude * weight * Mathf.Sin(phase), direction.y * horizontal);
    }

    static float Smooth(float min, float max, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
    static float Frac(float value) => value - Mathf.Floor(value);
    static float Hash(Vector2 p)
    {
        p = new Vector2(Frac(p.x * 123.34f), Frac(p.y * 456.21f));
        float dot = Vector2.Dot(p, p + Vector2.one * 45.32f);
        p += Vector2.one * dot;
        return Frac(p.x * p.y);
    }
    static float Noise(Vector2 p)
    {
        Vector2 cell = new Vector2(Mathf.Floor(p.x), Mathf.Floor(p.y));
        Vector2 f = p - cell;
        Vector2 u = new Vector2(f.x * f.x * (3f - 2f * f.x), f.y * f.y * (3f - 2f * f.y));
        return Mathf.Lerp(Mathf.Lerp(Hash(cell), Hash(cell + Vector2.right), u.x),
            Mathf.Lerp(Hash(cell + Vector2.up), Hash(cell + Vector2.one), u.x), u.y);
    }
}
