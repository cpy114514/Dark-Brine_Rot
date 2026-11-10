using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.UI;

public static class VerifyWorldMeasurements
{
    const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    const string Evidence = ".codex/distance-calibration/";

    public static async Task<object> Supply()
    {
        if (!Application.isPlaying) throw new InvalidOperationException("Use isolated First Island Play");
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (AdditiveSceneBootstrap.IsLoading)
        {
            if (DateTime.UtcNow > deadline) throw new InvalidOperationException("First Island is still loading");
            await Task.Delay(25);
        }
        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var arrival = player.GetComponent<FirstIslandArrival>();
        if (arrival) { arrival.enabled = false; arrival.RestoreProgress(true, true); }
        var hp = player.GetComponent<PlayerHealth>();
        var guidance = player.GetComponent<SahurSupplyGuidance>();
        var supply = new GameObject("Isolated ten metre supply marker", typeof(IslandSupplyCache), typeof(GameSaveExcluded));
        supply.GetComponent<IslandSupplyCache>().supportsMelee = true;
        float health = hp.currentHealth, time = Time.timeScale;
        bool locked = player.ExternalControlLock;
        try
        {
            Time.timeScale = 1;
            player.ExternalControlLock = false;
            var terrain = UnityEngine.Object.FindFirstObjectByType<ProceduralIsland>().GetComponent<Collider>();
            if (!terrain.Raycast(new Ray(new Vector3(288, 150, 346), Vector3.down), out var ground, 400))
                throw new InvalidOperationException("Supply review ground missing");
            player.RestoreSavedPose(ground.point + Vector3.up * (.08f - player.LowestFootWorldOffset), Quaternion.identity);
            await Task.Delay(300);
            supply.transform.position = player.transform.position + Vector3.right * 26.5f - Vector3.up * 1.2f;
            hp.currentHealth = hp.maxHealth * .3f;
            typeof(SahurSupplyGuidance).GetMethod("ScanSources", Private).Invoke(guidance, null);
            typeof(SahurSupplyGuidance).GetField("selectAt", Private).SetValue(guidance, 0f);
            typeof(SahurSupplyGuidance).GetField("textAt", Private).SetValue(guidance, 0f);
            typeof(SahurSupplyGuidance).GetMethod("Refresh", Private).Invoke(guidance, null);
            Require(guidance.Target && guidance.CurrentGuidance == SahurSupplyGuidance.GuidanceKind.Recovery,
                "Normal low-health supply guidance did not select the test camp");
            var caption = (Text)typeof(SahurSupplyGuidance).GetField("destination", Private).GetValue(guidance);
            var position = guidance.Target.transform.position + Vector3.up * 1.2f;
            float worldDistance = Vector3.Distance(position, player.transform.position);
            int metres = Mathf.CeilToInt(worldDistance / 2.65f);
            Require(caption.text.EndsWith(metres + " m"), "Supply distance still displays world units");
            var result = new { passed = true, target = guidance.Target.name, worldDistance, text = caption.text };
            File.WriteAllText(Evidence + "supply.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            hp.currentHealth = health;
            player.ExternalControlLock = locked;
            Time.timeScale = time;
            UnityEngine.Object.DestroyImmediate(supply);
            typeof(SahurSupplyGuidance).GetMethod("Refresh", Private).Invoke(guidance, null);
        }
    }

    public static object Run(string label)
    {
        if (!Application.isPlaying || AdditiveSceneBootstrap.IsLoading)
            throw new InvalidOperationException("Use loaded isolated Play");
        var player = UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
        var intro = UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();
        if (intro) UnityEngine.Object.DestroyImmediate(intro.gameObject);
        var arrival = UnityEngine.Object.FindFirstObjectByType<FirstIslandArrival>();
        if (arrival) { arrival.enabled = false; arrival.RestoreProgress(true, true); }
        var map = player.GetComponent<IslandMapUI>();
        var pause = UnityEngine.Object.FindFirstObjectByType<PauseSettingsMenu>();
        var scales = player.GetComponentsInChildren<Transform>(true).ToDictionary(t => t, t => t.localScale);
        var reports = new List<object>();
        float previousTime = Time.timeScale;
        var fixture = new GameObject("Isolated distance marker check");
        fixture.AddComponent<GameSaveExcluded>();
        try
        {
            Time.timeScale = 0;
            Require(Mathf.Abs(WorldMeasurements.ToMetres(5.3f) - 2f) < .0001f, "Sahur reference must measure 2 metres");
            Require(Mathf.Abs(WorldMeasurements.ToMetres(26.5f) - 10f) < .0001f, "Ten-metre marker calibration failed");
            Require(WorldMeasurements.ToMetres(0) == 0 && Mathf.Abs(WorldMeasurements.ToMetres(-26.5f) + 10f) < .0001f, "Map signed coordinates lost their origin");

            fixture.transform.position = player.transform.position;
            var destination = new GameObject("Ten metre destination");
            destination.transform.SetParent(fixture.transform, false);
            destination.transform.localPosition = Vector3.right * 26.5f;
            var sign = fixture.AddComponent<IslandLandmark>();
            sign.englishName = sign.chineseName = "Scale check";
            sign.caption = fixture.AddComponent<TextMesh>();
            sign.destination = destination.transform;
            typeof(IslandLandmark).GetMethod("Refresh", Private).Invoke(sign, null);
            Require(sign.caption.text.EndsWith("10 m"), "Landmark caption still displays world units");
            reports.Add(new { ui = "Landmark", text = sign.caption.text });

            var objectives = player.GetComponent<SecondIslandObjectives>();
            if (objectives)
            {
                string text = (string)typeof(SecondIslandObjectives).GetMethod("DirectionTo", Private)
                    .Invoke(objectives, new object[] { destination.transform.position });
                Require(text.EndsWith("10 m"), "Second Island guidance still displays world units");
                reports.Add(new { ui = "Second Island objective", text });
            }
            Require(map.SetOpen(true) && map.PanelVisible, "Actual map did not open");
            var coordinate = (Text)typeof(IslandMapUI).GetField("coordinates", Private).GetValue(map);
            string expected = GameLocalization.Format("map.coordinates",
                Mathf.Round(player.transform.position.x / 2.65f).ToString("F0"),
                Mathf.Round(player.transform.position.z / 2.65f).ToString("F0"),
                Mathf.Round(map.MapGraphic.WorldSpan / 2.65f).ToString("F0"));
            Require(coordinate.text == expected, "Map coordinates and width use inconsistent units");
            reports.Add(new { ui = "Map", text = coordinate.text, widthWorldUnits = map.MapGraphic.WorldSpan });
            map.SetOpen(false);

            pause.Open();
            var draft = typeof(PauseSettingsMenu).GetField("draft", Private).GetValue(pause);
            foreach (string key in new[] { "cameraDistance", "shadowDistance" })
            {
                float raw = (float)draft.GetType().GetField(key).GetValue(draft);
                string text = (string)typeof(PauseSettingsMenu).GetMethod("ValueText", Private).Invoke(pause, new object[] { key });
                string expectedValue = (key == "cameraDistance" ? (raw / 2.65f).ToString("0.0") : Mathf.RoundToInt(raw / 2.65f).ToString()) + " M";
                Require(text == expectedValue, "Settings display calibration failed " + key);
                reports.Add(new { ui = key, worldUnits = raw, text });
            }
            pause.Resume();
            if (player.GetComponent<FirstIslandObjectives>() is FirstIslandObjectives first)
            {
                string text = (string)typeof(FirstIslandObjectives).GetMethod("BossText", Private).Invoke(first, new object[] { true });
                reports.Add(new { ui = "First Island objectives", text });
            }
            Require(scales.All(pair => pair.Key.localScale == pair.Value), "Distance display changed a model scale");
            var result = new { passed = true, scene = player.gameObject.scene.path, reports,
                rootScale = player.transform.localScale.ToString(), modelScalesPreserved = true,
                capsuleWorldHeight = player.GetComponent<CharacterController>().height * Mathf.Abs(player.transform.lossyScale.y),
                player.moveSpeed, player.cameraDistance };
            Directory.CreateDirectory(Evidence);
            File.WriteAllText(Evidence + label + ".json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
            return result;
        }
        finally
        {
            if (map.PanelVisible) map.SetOpen(false);
            if (PauseSettingsMenu.IsOpen) pause.Resume();
            Time.timeScale = previousTime;
            UnityEngine.Object.DestroyImmediate(fixture);
        }
    }

    static void Require(bool value, string message)
    {
        if (!value) throw new InvalidOperationException(message);
    }
}
