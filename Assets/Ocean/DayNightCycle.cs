using UnityEngine;
using UnityEngine.Rendering;

/// <summary>Drives the island sun, atmospheric palette, and live ocean reflections.</summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Light))]
public sealed class DayNightCycle : MonoBehaviour
{
    [Header("Clock")]
    [Tooltip("Real-world minutes for one complete 24-hour in-game day.")]
    [Min(2f)] public float fullDayMinutes = 24f;
    [Range(0f, 24f)] public float startHour = 8f;
    public bool clockRuns = true;

    [Header("Cloud Weather")]
    [Range(0f, 1f)] public float minimumCloudCoverage = 0.62f;
    [Range(0f, 1f)] public float maximumCloudCoverage = 0.98f;
    [Tooltip("Weather fronts per second. 0.0045 changes between sparse and dense clouds about every 3.7 minutes.")]
    [Min(0.0001f)] public float cloudWeatherFrequency = 0.0045f;

    [Header("Sun")]
    [Min(10f)] public float noonElevation = 70f;
    [Min(0f)] public float noonIntensity = 1.55f;
    [SerializeField] ReflectionProbe oceanReflectionProbe;
    [Min(5f)] public float reflectionRefreshSeconds = 20f;
    [Range(64, 512)] public int reflectionResolution = 256;

    [Header("Moon")]
    [Range(0.006f, 0.035f)] public float moonApparentSize = 0.016f;
    [Range(0.5f, 3f)] public float moonSurfaceBrightness = 1.6f;
    [Range(0.05f, 0.8f)] public float fullMoonLightIntensity = 0.55f;

    Light sun;
    ProceduralSun sunVisual;
    OceanWorld ocean;
    float elapsedGameHours;
    float nextReflectionUpdate;
    int reflectionRenderId = -1;
    float lastReflectionRequest = float.NegativeInfinity;
    Vector3 reflectedLightDirection;
    Vector3 reflectedCameraPosition;
    Color reflectedLightRadiance;
    float reflectedCloudCoverage;
    int dailyVariationIndex = int.MinValue;
    float dailySunTimeShift;
    float dailySunAzimuthOffset;
    float dailySunElevationScale = 1f;
    float dailyPalette;
    float dailySunGlowScale = 1f;
    float currentNightFactor;
    float currentTwilightFactor;
    float currentCloudCoverage;
    Vector3 moonSurfaceLightDirection;
    GameObject moonObject;
    MeshRenderer moonRenderer;
    Material moonMaterial;
    bool originalFog;
    float originalFogStart;
    float originalFogEnd;
    FogMode originalFogMode;
    Color originalFogColor;
    AmbientMode originalAmbientMode;
    Color originalAmbientSkyColor;
    Color originalAmbientEquatorColor;
    Color originalAmbientGroundColor;
    float originalAmbientIntensity;
    Color originalSunColor;
    float originalSunIntensity;
    float originalShadowStrength;
    Quaternion originalSunRotation;
    bool originalColorTemperature;
    LightShadows originalShadows;
    bool originalRealtimeReflectionProbes;

    public float CurrentHour { get; private set; }
    public Vector3 SunDirection { get; private set; }
    public Vector3 MoonDirection { get; private set; }
    public float MoonIllumination { get; private set; }
    public float CurrentCloudCoverage => currentCloudCoverage;

    public float EvaluateCloudCoverage(float weatherSeconds)
    {
        // Weather is independent of the day number, so midnight cannot abruptly
        // replace the clouds. Alternating fronts actually reach both extremes;
        // the old daily bias kept the noise compressed around a narrow middle.
        float position = Mathf.Max(0f, weatherSeconds) * Mathf.Max(0.0001f, cloudWeatherFrequency);
        int front = Mathf.FloorToInt(position);
        float blend = SmoothThreshold(position - front, 0.12f, 0.88f);
        float low = Mathf.Min(minimumCloudCoverage, maximumCloudCoverage);
        float high = Mathf.Max(minimumCloudCoverage, maximumCloudCoverage);
        return Mathf.Lerp(low, high, Mathf.Lerp(CloudFront(front), CloudFront(front + 1), blend));
    }

    static float CloudFront(int front)
    {
        if (front == 0) return 0f;
        float variation = Hash01(front * 31 + 7);
        return (front & 1) == 0 ? variation * 0.12f : Mathf.Lerp(0.86f, 1f, variation);
    }

    const float LunarCycleDays = 29.53059f;

    static float SmoothThreshold(float value, float edge0, float edge1)
    {
        return Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(edge0, edge1, value));
    }

    public void ConfigureOceanReflectionProbe(ReflectionProbe probe) => oceanReflectionProbe = probe;

    void Awake()
    {
        sun = GetComponent<Light>();
        originalShadows = sun.shadows;
        originalSunColor = sun.color;
        originalSunIntensity = sun.intensity;
        originalShadowStrength = sun.shadowStrength;
        originalSunRotation = sun.transform.rotation;
        originalColorTemperature = sun.useColorTemperature;
        originalFog = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogStart = RenderSettings.fogStartDistance;
        originalFogEnd = RenderSettings.fogEndDistance;
        originalFogColor = RenderSettings.fogColor;
        originalAmbientMode = RenderSettings.ambientMode;
        originalAmbientSkyColor = RenderSettings.ambientSkyColor;
        originalAmbientEquatorColor = RenderSettings.ambientEquatorColor;
        originalAmbientGroundColor = RenderSettings.ambientGroundColor;
        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalRealtimeReflectionProbes = QualitySettings.realtimeReflectionProbes;
        sun.useColorTemperature = false;

        if (oceanReflectionProbe != null)
        {
            oceanReflectionProbe.mode = UnityEngine.Rendering.ReflectionProbeMode.Realtime;
            oceanReflectionProbe.refreshMode = ReflectionProbeRefreshMode.ViaScripting;
            oceanReflectionProbe.timeSlicingMode = ReflectionProbeTimeSlicingMode.IndividualFaces;
            oceanReflectionProbe.resolution = reflectionResolution;
            oceanReflectionProbe.hdr = true;
            oceanReflectionProbe.boxProjection = false;
            oceanReflectionProbe.shadowDistance = 120f;
            oceanReflectionProbe.cullingMask = ~LayerMask.GetMask("Water", "UI", "Ignore Raycast");
            oceanReflectionProbe.importance = 20;
        }

        if (!Application.isMobilePlatform)
            QualitySettings.realtimeReflectionProbes = true;
    }

    void Start()
    {
        CreateMoonVisual();
        ApplyTimeOfDay();
        RefreshOceanReflections();
    }

    void Update()
    {
        if (clockRuns && fullDayMinutes > 0f)
            elapsedGameHours += Time.deltaTime * 24f / (fullDayMinutes * 60f);

        ApplyTimeOfDay();
        if (oceanReflectionProbe != null)
        {
            Camera camera = Camera.main;
            Color radiance = sun.color * sun.intensity;
            float colorChange = Mathf.Abs(radiance.r - reflectedLightRadiance.r) +
                Mathf.Abs(radiance.g - reflectedLightRadiance.g) + Mathf.Abs(radiance.b - reflectedLightRadiance.b);
            bool changed = Vector3.Angle(-sun.transform.forward, reflectedLightDirection) > 5f ||
                colorChange > 0.12f || Mathf.Abs(currentCloudCoverage - reflectedCloudCoverage) > 0.08f ||
                (camera != null && Vector3.Distance(camera.transform.position, reflectedCameraPosition) > 30f);
            // The configured interval remains the upper bound. Capture earlier
            // through dawn/dusk, but never queue overlapping cubemap renders.
            if (Time.time >= nextReflectionUpdate || (changed && Time.time - lastReflectionRequest >= 1.5f))
                RefreshOceanReflections();
        }
    }

    void ApplyTimeOfDay()
    {
        float absoluteGameHours = startHour + elapsedGameHours;
        CurrentHour = Mathf.Repeat(absoluteGameHours, 24f);
        int gameDay = Mathf.FloorToInt(absoluteGameHours / 24f);
        UpdateDailyVariation(gameDay);

        // Keep the low sun in the island's main play-space view at dawn and dusk.
        // The 24-hour clock still completes a full cycle; this is the camera-facing
        // solar arc, not the clock rate.
        float solarHour = CurrentHour + dailySunTimeShift;
        float hourAngle = ((solarHour - 12f) * 4f + dailySunAzimuthOffset) * Mathf.Deg2Rad;
        float elevation = Mathf.Sin((solarHour - 6f) * Mathf.PI / 12f) *
                          Mathf.Clamp(noonElevation * dailySunElevationScale, 10f, 88f);
        float elevationRadians = elevation * Mathf.Deg2Rad;
        SunDirection = new Vector3(
            -Mathf.Sin(hourAngle) * Mathf.Cos(elevationRadians),
            Mathf.Sin(elevationRadians),
            Mathf.Cos(hourAngle) * Mathf.Cos(elevationRadians)).normalized;

        float daylight = SmoothThreshold(SunDirection.y, -0.14f, 0.24f);
        float twilight = (1f - SmoothThreshold(Mathf.Abs(SunDirection.y), 0.015f, 0.52f)) *
                         (1f - SmoothThreshold(SunDirection.y, 0.24f, 0.48f));
        float night = 1f - SmoothThreshold(SunDirection.y, -0.24f, 0.025f);
        currentNightFactor = Mathf.Clamp01(night);
        currentTwilightFactor = Mathf.Clamp01(twilight);

        // The moon follows a 29.5-day orbit relative to the sun, so its position
        // and illuminated fraction naturally change from night to night.
        float lunarAge = Mathf.Repeat(absoluteGameHours / (24f * LunarCycleDays) + 0.16f, 1f);
        float lunarAngle = lunarAge * Mathf.PI * 2f;
        Vector3 moonOrbitTangent = Vector3.up - Vector3.Dot(Vector3.up, SunDirection) * SunDirection;
        if (moonOrbitTangent.sqrMagnitude < 0.0001f)
            moonOrbitTangent = Vector3.Cross(SunDirection, Vector3.right);
        moonOrbitTangent.Normalize();
        MoonDirection = (SunDirection * Mathf.Cos(lunarAngle) +
                         moonOrbitTangent * Mathf.Abs(Mathf.Sin(lunarAngle))).normalized;
        MoonIllumination = Mathf.Clamp01((1f - Vector3.Dot(SunDirection, MoonDirection)) * 0.5f);
        moonSurfaceLightDirection = SunDirection;
        if (lunarAge > 0.5f)
        {
            float lightAlongMoon = Vector3.Dot(SunDirection, MoonDirection);
            moonSurfaceLightDirection = (MoonDirection * (2f * lightAlongMoon) - SunDirection).normalized;
        }

        float cloudCoverage = EvaluateCloudCoverage(Time.time);
        currentCloudCoverage = cloudCoverage;
        Vector3 lightingDirection = Vector3.Slerp(SunDirection, MoonDirection, night);
        sun.transform.rotation = Quaternion.LookRotation(-lightingDirection, Vector3.up);

        // The directional sun becomes the moonlight at night. Keep it cool, but
        // bright enough to visibly light terrain and characters under a full moon.
        Color nightSun = new Color(0.52f, 0.65f, 0.92f, 1f);
        Color daySun = new Color(1f, 0.93f, 0.79f, 1f);
        Color warmSun = Color.Lerp(new Color(1f, 0.42f, 0.26f, 1f),
            new Color(1f, 0.58f, 0.23f, 1f), dailyPalette);
        Color lowSunColor = Color.Lerp(daySun, warmSun, 0.72f);
        sun.color = Color.Lerp(Color.Lerp(nightSun, daySun, daylight), lowSunColor, twilight);
        float moonlight = Mathf.Lerp(0.008f, fullMoonLightIntensity, MoonIllumination) * night;
        sun.intensity = Mathf.Lerp(moonlight, noonIntensity, daylight);
        sun.shadows = daylight > 0.16f ? originalShadows : LightShadows.None;
        sun.shadowStrength = Mathf.Lerp(0.15f, 1f, SmoothThreshold(daylight, 0.02f, 0.25f)) * (1f - twilight * 0.38f);

        Color daySky = new Color(0.41f, 0.68f, 0.89f, 1f);
        Color nightSky = new Color(0.008f, 0.018f, 0.055f, 1f);
        Color goldenSky = Color.Lerp(new Color(0.53f, 0.30f, 0.58f, 1f),
            new Color(0.76f, 0.31f, 0.25f, 1f), dailyPalette);
        Color dayHorizon = new Color(0.72f, 0.85f, 0.96f, 1f);
        Color nightHorizon = new Color(0.025f, 0.052f, 0.12f, 1f);
        Color goldenHorizon = Color.Lerp(new Color(1f, 0.52f, 0.39f, 1f),
            new Color(1f, 0.39f, 0.17f, 1f), dailyPalette);
        Color dayCloud = new Color(0.96f, 0.98f, 1f, 1f);
        Color nightCloud = new Color(0.11f, 0.16f, 0.30f, 1f);
        Color goldenCloud = Color.Lerp(new Color(0.98f, 0.45f, 0.60f, 1f),
            new Color(1f, 0.57f, 0.28f, 1f), dailyPalette);
        Color sunDisk = Color.Lerp(new Color(1f, 0.86f, 0.58f, 1f), warmSun, twilight * 0.92f);

        Color zenith = Color.Lerp(nightSky, daySky, daylight);
        Color horizon = Color.Lerp(nightHorizon, dayHorizon, daylight);
        Color cloud = Color.Lerp(nightCloud, dayCloud, daylight);
        zenith = Color.Lerp(zenith, goldenSky, twilight * 0.72f);
        horizon = Color.Lerp(horizon, goldenHorizon, twilight * 0.92f);
        cloud = Color.Lerp(cloud, goldenCloud, twilight * 0.8f);

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = Color.Lerp(new Color(0.20f, 0.28f, 0.44f), new Color(0.53f, 0.65f, 0.76f), daylight);
        RenderSettings.ambientEquatorColor = Color.Lerp(new Color(0.12f, 0.16f, 0.25f), new Color(0.40f, 0.46f, 0.50f), daylight);
        RenderSettings.ambientGroundColor = Color.Lerp(new Color(0.055f, 0.065f, 0.10f), new Color(0.20f, 0.18f, 0.15f), daylight);
        RenderSettings.ambientIntensity = Mathf.Lerp(0.18f + moonlight * 0.55f, 1f, daylight);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 900f;
        RenderSettings.fogEndDistance = 3000f;
        RenderSettings.fogColor = Color.Lerp(new Color(0.035f, 0.052f, 0.105f), new Color(0.58f, 0.70f, 0.80f), daylight);
        RenderSettings.fogColor = Color.Lerp(RenderSettings.fogColor, goldenHorizon, twilight * 0.42f);

        if (ocean == null || !ocean.isActiveAndEnabled)
            ocean = FindFirstObjectByType<OceanWorld>();
        if (ocean != null)
        {
            Color waterWarm = new Color(0.16f, 0.10f, 0.14f, 1f);
            Color shallow = Color.Lerp(new Color(0.018f, 0.044f, 0.105f, 1f), ocean.shallowColor, daylight);
            Color middle = Color.Lerp(new Color(0.009f, 0.024f, 0.065f, 1f), ocean.midColor, daylight);
            Color deep = Color.Lerp(new Color(0.003f, 0.009f, 0.028f, 1f), ocean.deepColor, daylight);
            shallow = Color.Lerp(shallow, waterWarm, twilight * 0.32f);
            middle = Color.Lerp(middle, waterWarm, twilight * 0.22f);
            deep = Color.Lerp(deep, waterWarm, twilight * 0.12f);
            Color waterReflection = Color.Lerp(new Color(0.12f, 0.18f, 0.31f, 1f), horizon, daylight);
            waterReflection = Color.Lerp(waterReflection, goldenHorizon, twilight * 0.86f);
            ocean.ApplyDayNightAppearance(horizon, zenith, cloud, cloudCoverage, sunDisk,
                Mathf.Lerp(0.32f, 1f, Mathf.Max(daylight, twilight * 0.85f)), daylight, Mathf.Clamp01(night),
                MoonDirection, MoonIllumination,
                shallow, middle, deep, waterReflection);
        }

        if (sunVisual == null)
            sunVisual = FindFirstObjectByType<ProceduralSun>();
        if (sunVisual != null)
            sunVisual.SetAtmosphere(sunDisk, Color.Lerp(new Color(0.30f, 0.48f, 0.9f), warmSun, twilight),
                Mathf.Lerp(3.0f, 6.2f, twilight) * dailySunGlowScale, SunDirection.y > -0.035f);
    }

    void UpdateDailyVariation(int gameDay)
    {
        if (dailyVariationIndex == gameDay) return;
        dailyVariationIndex = gameDay;
        dailySunTimeShift = Mathf.Lerp(-0.32f, 0.32f, Hash01(gameDay * 11 + 1));
        dailySunAzimuthOffset = Mathf.Lerp(-5.5f, 5.5f, Hash01(gameDay * 17 + 3));
        dailySunElevationScale = Mathf.Lerp(0.94f, 1.035f, Hash01(gameDay * 23 + 5));
        dailyPalette = Hash01(gameDay * 37 + 11);
        dailySunGlowScale = Mathf.Lerp(0.9f, 1.18f, Hash01(gameDay * 43 + 13));
    }

    static float Hash01(int seed)
    {
        float value = Mathf.Sin(seed * 127.1f + 311.7f) * 43758.5453f;
        return value - Mathf.Floor(value);
    }

    void CreateMoonVisual()
    {
        Shader shader = Shader.Find("DarkBrine/Procedural Moon");
        if (shader == null)
        {
            Debug.LogWarning("Procedural Moon shader is missing; the moon mesh will not be created.", this);
            return;
        }

        moonObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        moonObject.name = "Procedural Moon (Runtime)";
        Collider moonCollider = moonObject.GetComponent<Collider>();
        if (moonCollider != null) Destroy(moonCollider);
        moonRenderer = moonObject.GetComponent<MeshRenderer>();
        moonRenderer.shadowCastingMode = ShadowCastingMode.Off;
        moonRenderer.receiveShadows = false;
        moonMaterial = new Material(shader) { name = "Procedural Moon Material (Runtime)" };
        moonMaterial.hideFlags = HideFlags.DontSave;
        moonRenderer.sharedMaterial = moonMaterial;
    }

    void LateUpdate()
    {
        if (moonObject == null || moonRenderer == null || moonMaterial == null)
            return;

        Camera camera = Camera.main;
        if (camera == null)
            return;

        float distance = Mathf.Max(80f, Mathf.Min(1400f, camera.farClipPlane * 0.72f));
        moonObject.transform.position = camera.transform.position + MoonDirection * distance;
        moonObject.transform.rotation = Quaternion.LookRotation(-MoonDirection, camera.transform.up);
        moonObject.transform.localScale = Vector3.one * (distance * moonApparentSize);

        float visibility = SmoothThreshold(currentNightFactor, 0.015f, 0.42f);
        moonRenderer.enabled = visibility > 0.001f;
        moonMaterial.SetVector("_SunDirection", moonSurfaceLightDirection);
        moonMaterial.SetColor("_MoonTint", Color.Lerp(new Color(0.78f, 0.86f, 1f),
            new Color(1f, 0.86f, 0.70f), currentTwilightFactor * 0.28f));
        moonMaterial.SetFloat("_MoonVisibility", visibility);
        moonMaterial.SetFloat("_MoonIllumination", MoonIllumination);
        moonMaterial.SetFloat("_MoonBrightness", moonSurfaceBrightness);
        moonMaterial.SetFloat("_CloudHaze", currentCloudCoverage);
    }

    void OnEnable()
    {
        if (moonObject != null) moonObject.SetActive(true);
    }

    void RefreshOceanReflections()
    {
        if (oceanReflectionProbe != null)
        {
            if (reflectionRenderId >= 0 && !oceanReflectionProbe.IsFinishedRendering(reflectionRenderId))
                return;
            Camera camera = Camera.main;
            if (ocean != null && camera != null)
            {
                float coverage = ocean.oceanSize + Mathf.Max(300f, ocean.oceanSize * 0.2f);
                oceanReflectionProbe.transform.position = new Vector3(camera.transform.position.x,
                    ocean.oceanHeight + 5f, camera.transform.position.z);
                oceanReflectionProbe.size = Vector3.one * (coverage * 2f);
                // The procedural sky is a sphere around the camera, not a
                // RenderSettings skybox. The probe must actually see its surface.
                oceanReflectionProbe.farClipPlane = coverage + Vector3.Distance(
                    camera.transform.position, oceanReflectionProbe.transform.position);
                reflectedCameraPosition = camera.transform.position;
            }
            oceanReflectionProbe.resolution = reflectionResolution;
            reflectionRenderId = oceanReflectionProbe.RenderProbe();
            reflectedLightDirection = -sun.transform.forward;
            reflectedLightRadiance = sun.color * sun.intensity;
            reflectedCloudCoverage = currentCloudCoverage;
            lastReflectionRequest = Time.time;
        }
        nextReflectionUpdate = Time.time + reflectionRefreshSeconds;
    }

    void OnDisable()
    {
        if (moonObject != null) moonObject.SetActive(false);
        if (!Application.isPlaying) return;
        if (sun != null)
        {
            sun.shadows = originalShadows;
            sun.color = originalSunColor;
            sun.intensity = originalSunIntensity;
            sun.shadowStrength = originalShadowStrength;
            sun.transform.rotation = originalSunRotation;
            sun.useColorTemperature = originalColorTemperature;
        }
        RenderSettings.fog = originalFog;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogStartDistance = originalFogStart;
        RenderSettings.fogEndDistance = originalFogEnd;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.ambientSkyColor = originalAmbientSkyColor;
        RenderSettings.ambientEquatorColor = originalAmbientEquatorColor;
        RenderSettings.ambientGroundColor = originalAmbientGroundColor;
        RenderSettings.ambientIntensity = originalAmbientIntensity;
        QualitySettings.realtimeReflectionProbes = originalRealtimeReflectionProbes;
    }

    void OnDestroy()
    {
        if (moonObject != null) Destroy(moonObject);
        if (moonMaterial != null) Destroy(moonMaterial);
    }
}
