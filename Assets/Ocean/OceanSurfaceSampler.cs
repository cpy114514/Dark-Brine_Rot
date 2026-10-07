using UnityEngine;

/// <summary>CPU counterpart of OceanWaves.hlsl for buoyancy and camera water contact.</summary>
public static class OceanSurfaceSampler
{
    public static float Height(OceanWorld ocean, Vector3 position, Camera camera)
        => Capture(ocean, camera, Time.time).Height(position);

    // Ship buoyancy passes its simulation clock; character swimming passes
    // its own camera. Both use the same wave implementation.
    public static float Height(OceanWorld ocean, Vector3 position, float gameTime)
        => Capture(ocean, Camera.main, gameTime).Height(position);

    // A buoyant body captures camera/configuration once for all its contact points.
    // The snapshot contains only values; sampling never calls into Unity objects.
    public static Snapshot Capture(OceanWorld ocean, Camera camera, float gameTime)
        => ocean == null ? default : new Snapshot(ocean, camera, gameTime);

    public readonly struct Snapshot
    {
        readonly float level, time, irregularity, near, mid, cameraX, cameraZ;
        readonly bool hasCamera;
        readonly PreparedWave wave1, wave2, wave3, wave4;
        internal Snapshot(OceanWorld ocean, Camera camera, float gameTime)
        {
            level = ocean.oceanHeight; time = gameTime * ocean.waveMotionSpeed;
            irregularity = ocean.waveIrregularity;
            ocean.GetShaderDetailDistances(out near, out mid);
            hasCamera = camera != null;
            Vector3 at = hasCamera ? camera.transform.position : Vector3.zero;
            cameraX = at.x; cameraZ = at.z;
            wave1 = new PreparedWave(ocean.wave1); wave2 = new PreparedWave(ocean.wave2);
            wave3 = new PreparedWave(ocean.wave3); wave4 = new PreparedWave(ocean.wave4);
        }
        public float Height(Vector3 position)
        {
            float x = position.x, z = position.z;
            // Keep the same three inverse-displacement iterations as the shader counterpart.
            for (int i = 0; i < 3; i++)
            {
                Displacement(x, z, false, out float dx, out _, out float dz);
                x = position.x - dx; z = position.z - dz;
            }
            Displacement(x, z, true, out _, out float height, out _);
            return level + height;
        }
        void Displacement(float x, float z, bool needHeight, out float dx, out float height, out float dz)
        {
            float broad = Noise(x * .011f + 17.3f + time * .0017f, z * .011f - 8.1f - time * .0011f);
            float small = Noise(x * .023f - 12.7f - time * .0013f, z * .023f + 21.4f + time * .0019f);
            float regional = Noise(x * .0058f + 38.4f + time * .0008f, z * .0058f + 5.7f - time * .0005f);
            float cross = Noise(x * .039f - 29.6f - time * .0021f, z * .039f - 17.2f + time * .0014f);
            float sx = x + (broad - .5f) * (19f * irregularity);
            float sz = z + (small - .5f) * (19f * irregularity);
            float swell = 1f + irregularity * ((broad - .5f) * .85f + (regional - .5f) * .48f + (small - .5f) * .25f);
            float chop = 1f + irregularity * ((small - .5f) * .70f - (broad - .5f) * .20f + (cross - .5f) * .42f);
            float distance = hasCamera ? Mathf.Sqrt((x - cameraX) * (x - cameraX) + (z - cameraZ) * (z - cameraZ)) : 0;
            float detailNear = 1f - Smooth(near * .65f, near, distance);
            float detailMid = 1f - Smooth(mid * .75f, mid, distance);
            wave1.Sample(sx, sz, time, swell, irregularity * (4f * (regional - .5f) + 1.7f * (small - .5f)), needHeight, out dx, out height, out dz);
            wave2.Sample(sx, sz, time + 1.9f, chop, irregularity * (2.9f * (broad - .5f) - 2.2f * (cross - .5f)), needHeight, out float bx, out float by, out float bz);
            dx += bx; height += by; dz += bz;
            wave3.Sample(sx, sz, time + 4.2f, chop * detailMid, irregularity * (1.6f * (cross - .5f) + .8f * (regional - .5f)), needHeight, out bx, out by, out bz);
            dx += bx; height += by; dz += bz;
            wave4.Sample(sx, sz, time + 2.7f, swell * detailNear, irregularity * (1.45f * (small - .5f) - 1.2f * (regional - .5f)), needHeight, out bx, out by, out bz);
            dx += bx; height += by; dz += bz;
        }
    }
    readonly struct PreparedWave
    {
        readonly float x, z, number, speed, amplitude, horizontal;
        public PreparedWave(OceanGerstnerWave wave)
        {
            Vector2 direction = wave.direction.sqrMagnitude < .0001f ? Vector2.right : wave.direction.normalized;
            x = direction.x; z = direction.y;
            number = Mathf.PI * 2f / Mathf.Max(.001f, wave.wavelength);
            speed = wave.speed; amplitude = wave.wavelength > 0 ? Mathf.Max(0, wave.amplitude) : 0;
            horizontal = Mathf.Min(wave.steepness, .95f) * amplitude;
        }
        public void Sample(float px, float pz, float time, float weight, float offset, bool needHeight, out float dx, out float height, out float dz)
        {
            if (amplitude <= 0 || weight == 0) { dx = height = dz = 0; return; }
            float phase = number * (x * px + z * pz - speed * time) + offset;
            float cosine = Mathf.Cos(phase);
            float sideways = horizontal * weight * cosine;
            dx = x * sideways; dz = z * sideways;
            height = 0;
            if (needHeight)
            {
                float sine = Mathf.Sin(phase);
                height = amplitude * weight * (sine - .08f * (cosine * cosine - sine * sine));
            }
        }
    }

    static float Smooth(float min, float max, float value) => Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(min, max, value));
    static float Frac(float value) => value - Mathf.Floor(value);
    static float Hash(float x, float y)
    {
        x = Frac(x * 123.34f); y = Frac(y * 456.21f);
        float dot = x * (x + 45.32f) + y * (y + 45.32f);
        x += dot; y += dot;
        return Frac(x * y);
    }
    static float Noise(float x, float y)
    {
        float cx = Mathf.Floor(x), cy = Mathf.Floor(y);
        float fx = x - cx, fy = y - cy;
        float ux = fx * fx * (3f - 2f * fx), uy = fy * fy * (3f - 2f * fy);
        return Mathf.Lerp(Mathf.Lerp(Hash(cx, cy), Hash(cx + 1, cy), ux),
            Mathf.Lerp(Hash(cx, cy + 1), Hash(cx + 1, cy + 1), ux), uy);
    }
}
