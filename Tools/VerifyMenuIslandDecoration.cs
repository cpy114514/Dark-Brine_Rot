using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class VerifyMenuIslandDecoration
{
    static void Check(bool ok, string error) { if (!ok) throw new Exception(error); }
    public static async Task<object> RunRuntime()
    {
        Check(Application.isPlaying, "Requires Play Mode.");
        var source = UnityEngine.Object.FindObjectsByType<MenuIslandDecoration>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .First(d => d.gameObject.scene.name == "MainMenu");
        var root = new GameObject("Temporary runtime island test", typeof(RectTransform), typeof(Canvas));
        var viewport = UnityEngine.Object.Instantiate(source.transform.parent.gameObject, root.transform);
        float previousTimeScale = Time.timeScale;
        try
        {
            viewport.SetActive(true);
            var rotation = viewport.GetComponentInChildren<MenuIslandDecoration>();
            var rect = (RectTransform)rotation.transform;
            var hover = rotation.GetComponent<SahurDecorationHover>();
            var image = rotation.GetComponent<Image>();
            float start = rect.localEulerAngles.z;
            Check(Mathf.Abs(Mathf.DeltaAngle(start, -90f)) < .1f, "Runtime initial terrain incorrect.");
            var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            Time.timeScale = 0;
            hover.OnPointerEnter(click); rotation.OnPointerClick(click);
            await Task.Delay(1000);
            Check(!rotation.IsRotating && Mathf.Abs(Mathf.Abs(Mathf.DeltaAngle(start,rect.localEulerAngles.z))-180f)<.1f,
                "Natural Update did not complete rotation while paused.");
            Check(image.sprite == hover.hoverSprite, "Rotation lost hover colour.");
            hover.OnPointerExit(click); Check(image.sprite == hover.normalSprite, "Runtime exit failed.");
            rotation.OnPointerClick(click); await Task.Delay(1000);
            Check(!rotation.IsRotating && Mathf.Abs(Mathf.DeltaAngle(start,rect.localEulerAngles.z))<.1f, "Runtime second click failed.");
            return new { naturalUpdate = true, pausedRotation = true, hoverRetained = true, twoClicksRestore = true };
        }
        finally { Time.timeScale = previousTimeScale; UnityEngine.Object.DestroyImmediate(root); }
    }
    public static async Task<object> Run()
    {
        var source = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None)
            .Single(c => c.name == "Main Menu" && c.gameObject.scene.name == "MainMenu");
        var clone = UnityEngine.Object.Instantiate(source.gameObject);
        var cameraObject = new GameObject("Temporary island menu capture");
        var target = new RenderTexture(1920,1080,24);
        var pixels = new Texture2D(1920,1080,TextureFormat.RGB24,false);
        var previous = RenderTexture.active;
        try
        {
            clone.SetActive(true);
            foreach (var t in clone.GetComponentsInChildren<Transform>(true)) t.gameObject.layer = 31;
            var camera = cameraObject.AddComponent<Camera>(); camera.enabled = false; camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = Color.black;
            camera.orthographic = true; camera.orthographicSize = 540;
            camera.nearClipPlane = .01f; camera.farClipPlane = 100; camera.targetTexture = target;
            camera.transform.position = new Vector3(0,0,-10);
            var canvas = clone.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera; canvas.planeDistance = 1;
            var rotation = clone.GetComponentInChildren<MenuIslandDecoration>(true);
            var hover = rotation.GetComponent<SahurDecorationHover>();
            var image = rotation.GetComponent<Image>(); var rect = (RectTransform)rotation.transform;
            var mask = rotation.GetComponentInParent<RectMask2D>();
            Check(mask && hover.normalSprite && hover.hoverSprite, "Missing island setup.");
            Check(hover.normalSprite.rect.size == hover.hoverSprite.rect.size, "Colour swap changes dimensions.");
            // Edit-mode clones do not invoke the normal runtime OnEnable automatically.
            rotation.SendMessage("OnEnable"); hover.OnPointerExit(null);
            float start = rect.localEulerAngles.z;
            var click = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            await Capture("Tools/MenuIslandGrass.png");
            Canvas.ForceUpdateCanvases();
            var inside = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(-180,0,0)));
            Check(image.Raycast(inside, camera), "Visible grass cannot receive pointer input.");
            var hidden = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(180,0,0)));
            Check(!image.Raycast(hidden, camera), "Hidden terrain intercepts clicks.");
            var corner = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(new Vector3(-440,350,0)));
            Check(!image.Raycast(corner, camera), "Transparent padding intercepts clicks.");
            hover.OnPointerEnter(click);
            Check(image.sprite == hover.hoverSprite && image.material == hover.hoverMaterial, "Hover does not colour island.");
            await Capture("Tools/MenuIslandGrassColored.png");
            rotation.OnPointerClick(click); rotation.AdvanceRotation(.325f);
            Check(rotation.IsRotating && Mathf.Abs(Mathf.DeltaAngle(start,rect.localEulerAngles.z)-90f)<.1f, "Rotation does not animate halfway.");
            rotation.OnPointerClick(click); // No extra spin from double-clicking during the animation.
            rotation.AdvanceRotation(.4f);
            Check(!rotation.IsRotating && Mathf.Abs(Mathf.Abs(Mathf.DeltaAngle(start,rect.localEulerAngles.z))-180f)<.1f, "First click not exactly 180 degrees.");
            await Capture("Tools/MenuIslandBeachColored.png");
            hover.OnPointerExit(null); Check(image.sprite == hover.normalSprite, "Exit does not restore black-and-white.");
            await Capture("Tools/MenuIslandBeach.png");
            click.button = PointerEventData.InputButton.Right;
            rotation.OnPointerClick(click); Check(!rotation.IsRotating, "Right click rotates island.");
            click.button = PointerEventData.InputButton.Left;
            rotation.OnPointerClick(click); rotation.AdvanceRotation(1f);
            Check(Mathf.Abs(Mathf.DeltaAngle(start, rect.localEulerAngles.z))<.1f, "Second click does not restore grass.");
            int buttons = 0;
            foreach (var button in clone.GetComponentsInChildren<Button>().Where(b => b.isActiveAndEnabled))
            {
                var point = RectTransformUtility.WorldToScreenPoint(camera, ((RectTransform)button.transform).TransformPoint(((RectTransform)button.transform).rect.center));
                Check(!image.Raycast(point,camera), "Island blocks menu button: " + button.name);
                buttons++;
            }
            return new { hover = true, visibleHalfOnly = true, transparentPadding = true, rotation180 = true, twoClicksRestore = true, doubleClickSafe = true, menuButtonsUnblocked = buttons };

            async Task Capture(string path)
            {
                Canvas.ForceUpdateCanvases(); await Task.Delay(80); Canvas.ForceUpdateCanvases();
                camera.Render(); RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0,0,1920,1080),0,0); pixels.Apply(); File.WriteAllBytes(Path.GetFullPath(path),pixels.EncodeToPNG());
            }
        }
        finally
        {
            RenderTexture.active = previous; UnityEngine.Object.DestroyImmediate(clone);
            UnityEngine.Object.DestroyImmediate(cameraObject); UnityEngine.Object.DestroyImmediate(target); UnityEngine.Object.DestroyImmediate(pixels);
        }
    }
}
