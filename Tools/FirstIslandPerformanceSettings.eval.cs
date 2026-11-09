// Run explicitly via the local Development Player's eval_file command.
// The observer has no dependency on these settings or on Pipeline.
if (UnityEngine.Application.isEditor || UnityEngine.Application.productName != "DarkBrineRot_Playtest")
    throw new System.InvalidOperationException("Isolated playtest Player required.");

var pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline;
var quality = UnityEngine.QualitySettings.names[UnityEngine.QualitySettings.GetQualityLevel()];
if (!pipeline || quality != "PC" || UnityEngine.Screen.width != 1920 || UnityEngine.Screen.height != 1080)
    throw new System.InvalidOperationException("Expected PC quality and a 1920x1080 Player.");

// The Runtime Eval compiler doesn't reference URP, but its loaded asset exposes
// these public properties. Keep the authored MSAA setting and other quality options.
var msaa = pipeline.GetType().GetProperty("msaaSampleCount");
if ((int)msaa.GetValue(pipeline) != 4)
    throw new System.InvalidOperationException("Expected the current PC pipeline to use 4x MSAA.");

const string prefix = "DarkBrine.Settings.";
UnityEngine.PlayerPrefs.SetFloat(prefix + "RenderScale", 1);
UnityEngine.PlayerPrefs.SetInt(prefix + "VSync", 0);
UnityEngine.PlayerPrefs.SetInt(prefix + "FrameLimit", -1);
UnityEngine.PlayerPrefs.Save();
UnityEngine.QualitySettings.vSyncCount = 0;
UnityEngine.Application.targetFrameRate = -1;
var renderScale = pipeline.GetType().GetProperty("renderScale");
renderScale.SetValue(pipeline, 1f);

return new {
    quality, UnityEngine.Screen.width, UnityEngine.Screen.height,
    msaa = msaa.GetValue(pipeline), renderScale = renderScale.GetValue(pipeline),
    targetFps = UnityEngine.Application.targetFrameRate, vsync = UnityEngine.QualitySettings.vSyncCount
};
