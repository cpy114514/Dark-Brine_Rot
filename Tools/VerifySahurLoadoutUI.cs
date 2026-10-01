using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Mavis;

public static class VerifySahurLoadoutUI
{
    public static async System.Threading.Tasks.Task<object> Verify()
    {
        if (!Application.isPlaying) throw new Exception("Requires Play Mode.");
        float originalTime = Time.timeScale;
        var originalCursor = Cursor.lockState;
        bool originalVisible = Cursor.visible;
        var player = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab"), new Vector3(0, 500, 0), Quaternion.identity);
        var cameraObject = new GameObject("Loadout inspection camera");
        var target = new RenderTexture(1280, 720, 24);
        var frame = new Texture2D(1280, 720, TextureFormat.RGB24, false);
        var previous = RenderTexture.active;
        try
        {
            var ui = player.GetComponent<SahurLoadoutUI>();
            if (ui == null || ui.EquipmentSlotCount != 5 || ui.SkillSlotCount != 4 || ui.PanelVisible)
                throw new Exception("Missing slots or initially open panel.");
            for (int i = 0; i < 4; i++) { ui.SelectSkill(i); if (ui.SelectedSkill != i) throw new Exception("Skill selection failed."); }
            ui.SelectSkill(9);
            if (ui.SelectedSkill != 3) throw new Exception("Invalid index accepted.");
            Time.timeScale = .8f;
            ui.SetOpen(true);
            if (!ui.PanelVisible || !SahurLoadoutUI.IsOpen || Time.timeScale != 0 || Cursor.lockState != CursorLockMode.None)
                throw new Exception("Open did not pause and release cursor.");
            var equipment = new[] { "头部     空", "身体     空", "腿部     空", "脚部     空", "武器     空" };
            var canvas = UnityEngine.Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None)
                .Single(c => c.name == "Sahur Equipment and Skills UI");
            var buttons = canvas.GetComponentsInChildren<Button>(true);
            foreach (var title in equipment) buttons.Single(b => b.name == title).onClick.Invoke();
            var inventory = player.GetComponent<EquipmentInventory>();
            foreach (var item in Resources.LoadAll<EquipmentItem>("Equipment/Nailong")) inventory.Collect(item);
            ui.SelectSkillForAssignment(2);
            if (ui.EditingSkill != 2) throw new Exception("Skill assignment selection failed.");
            ui.SelectEquipment(SahurLoadoutUI.EquipmentSlot.Feet);
            if (!canvas.GetComponentsInChildren<Text>(true).Any(t => t.text.Contains("奶龙洞洞鞋")))
                throw new Exception("Collected item is not shown in equipment UI.");
            var attack = player.GetComponent<SahurAttack>();
            attack.TriggerAttack();
            if (attack.IsCombatMotionActive || player.GetComponent<SahurBoomerang>().TryThrow())
                throw new Exception("Combat was not blocked by UI.");
            var camera = cameraObject.AddComponent<Camera>();
            camera.cullingMask = 1 << 31;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(.19f, .24f, .28f);
            camera.targetTexture = target;
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1;
            canvas.GetComponent<CanvasScaler>().enabled = false;
            canvas.scaleFactor = 1280f / 1920f;
            foreach (var child in canvas.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
            await System.Threading.Tasks.Task.Delay(100);
            Canvas.ForceUpdateCanvases();
            camera.Render();
            RenderTexture.active = target;
            frame.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); frame.Apply();
            string path = System.IO.Path.GetFullPath("Tools/SahurLoadoutUIInspection.png");
            System.IO.File.WriteAllBytes(path, frame.EncodeToPNG());
            buttons.Single(b => b.name == "关闭  [I]").onClick.Invoke();
            if (SahurLoadoutUI.IsOpen || ui.PanelVisible || Mathf.Abs(Time.timeScale - .8f) > .001f)
                throw new Exception("Close did not restore time scale.");
            ui.SetOpen(true);
            ui.enabled = false;
            if (SahurLoadoutUI.IsOpen || Mathf.Abs(Time.timeScale - .8f) > .001f)
                throw new Exception("Disable left game paused.");
            return new { equipmentSlots = 5, skillSlots = 4, selection = true, emptyState = true,
                buttons = true, pausesSafely = true, blocksCombat = true, restoresOnCloseAndDisable = true, path,
                rect = ((RectTransform)canvas.transform).rect.ToString(), scale = canvas.scaleFactor, pixelRect = canvas.pixelRect.ToString() };
        }
        finally
        {
            RenderTexture.active = previous;
            UnityEngine.Object.DestroyImmediate(player);
            UnityEngine.Object.DestroyImmediate(cameraObject);
            UnityEngine.Object.DestroyImmediate(target);
            UnityEngine.Object.DestroyImmediate(frame);
            Time.timeScale = originalTime;
            Cursor.lockState = originalCursor; Cursor.visible = originalVisible;
        }
    }
}
