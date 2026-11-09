// The finite world-space sun and its per-camera transform bridge are retired.
// Keep old command names discoverable without reintroducing transform callbacks.
public static class VerifySunCamera
{
    public static object Run() => "Superseded: run VerifySunSky.Run for sky visibility, occlusion and camera regression.";
    public static object ApplyLive() => "No live bridge required. ProceduralSun now controls the sky shader disk.";
}
