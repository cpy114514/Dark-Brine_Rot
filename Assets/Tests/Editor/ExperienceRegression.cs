using System;
using System.Collections.Generic;
using System.Reflection;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class ExperienceRegression
{
    public static string Run()
    {
        var result = new List<string>();
        float volume = AudioListener.volume;
        var go = new GameObject("Experience regression");
        var scene = SceneManager.GetSceneByPath("Assets/Scenes/First Island/Environment.unity");
        SceneManager.MoveGameObjectToScene(go, scene); go.transform.position = new Vector3(10000, 0, 10000);
        try
        {
            GameAudioPolicy.ApplyMasterVolume(.37f);
            if (!GameAudioPolicy.SoundEnabled || Mathf.Abs(AudioListener.volume - .37f) > .001f) throw new Exception("Master volume did not apply.");
            GameAudioPolicy.ApplyMasterVolume(0);
            if (AudioListener.volume != 0) throw new Exception("Mute failed.");
            result.Add("PASS enabled audio, saved-volume application and mute");
            var warning = go.AddComponent<BossAttackWarning>();
            typeof(BossAttackWarning).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(warning, null);
            warning.Circle(go.transform.position, 4, 1);
            var line = go.GetComponentInChildren<LineRenderer>();
            if (!BossAttackWarning.HiddenOnIsland(go) || (line && line.enabled)) throw new Exception("Hidden warning policy created a footprint.");
            warning.Clear(); if (line && line.enabled) throw new Exception("Warning did not clear.");
            result.Add("PASS current global no-footprint policy and safe warning clear");
            var boss = go.AddComponent<BombardinoBoss>();
            typeof(BombardinoBoss).GetMethod("SetState", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(boss, new object[] { BombardinoBoss.State.Exposed });
            if (!boss.IsExposed || !boss.Hint.Contains("COUNTER")) throw new Exception("Counter state was discarded.");
            result.Add("PASS aircraft counter window state and player hint");
            var objectives = go.AddComponent<FirstIslandObjectives>();
            objectives.Restore(new FirstIslandObjectives.Progress { stage = 4, rewardBaseline = 2, bossId = "test-boss" });
            var snapshot = new GameWorldProgress.Snapshot { tutorial = objectives.Capture() };
            var restored = JsonUtility.FromJson<GameWorldProgress.Snapshot>(JsonUtility.ToJson(snapshot));
            if (restored.tutorial.stage != 4 || restored.tutorial.bossId != "test-boss" || restored.tutorial.rewardBaseline != 2) throw new Exception("Tutorial progress did not persist.");
            result.Add("PASS tutorial stage, boss target and reward baseline round-trip");
            foreach (var beat in Story1EncounterDefinition.ApprovedBeats())
                if (beat.action != Story1EncounterAction.Surf && !Story1EncounterClipCache.Find(beat.clip)) throw new Exception("Stable opening dependency missing: " + beat.clip);
            result.Add("PASS stable opening dependencies; incomplete paired-animation set detected before takeover (" + Story1WreckBattle.MissingRedesignClips().Length + " missing)");
            var frames = new List<IslandPerformanceBaseline.Sample>();
            for (int i = 0; i < 100; i++) frames.Add(new IslandPerformanceBaseline.Sample { phase = "test", ms = i == 99 ? 100 : 16, gcBytes = i });
            var summary = IslandPerformanceBaseline.Summarize("test", frames);
            if (summary.frames != 100 || summary.p95Ms != 16 || summary.p99Ms != 16 || summary.maxMs != 100 ||
                summary.spikes50ms != 1 || summary.maxGcBytes != 99 || Mathf.Abs(summary.onePercentLowFps - 10) > .01f) throw new Exception("Performance summary metrics incorrect.");
            result.Add("PASS performance percentiles, 1% low, spike and allocation metrics");
            return string.Join("\n", result);
        }
        finally
        {
            AudioListener.volume = volume;
            foreach (var renderer in go.GetComponentsInChildren<LineRenderer>()) if (renderer.sharedMaterial) UnityEngine.Object.DestroyImmediate(renderer.sharedMaterial);
            UnityEngine.Object.DestroyImmediate(go);
        }
    }
}
