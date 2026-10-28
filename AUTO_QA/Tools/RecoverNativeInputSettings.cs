using System;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;

public static class AutoQaRecoverNativeInputSettings
{
    public static object Run()
    {
        if (InputSystem.settings) return new { recovered = false, reason = "Current native settings are valid; preserve them" };
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var type = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AutoQaNativeInputSmoke")).FirstOrDefault(t => t != null &&
            (string)t.GetField("folder", flags).GetValue(null) == "AUTO_QA/Evidence/native-input/batch005-take01" &&
            !ReferenceEquals(t.GetField("oldSettings", flags).GetValue(null), null));
        if (type == null || (bool)type.GetField("armed", flags).GetValue(null)) throw new Exception("No stopped fixture with recoverable prior managed settings");
        var prior = type.GetField("oldSettings", flags).GetValue(null) as InputSettings;
        var recovered = ScriptableObject.CreateInstance<InputSettings>();
        recovered.name = "InputSettings"; recovered.hideFlags = HideFlags.HideAndDontSave;
        var fields = typeof(InputSettings).GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly).Where(f => !f.IsInitOnly).ToArray();
        var expected = new Dictionary<string, object>();
        foreach (var field in fields)
        {
            var value = field.GetValue(prior); expected[field.Name] = value;
            if (value is Array array) value = array.Clone();
            else if (value is HashSet<string> set) value = new HashSet<string>(set);
            field.SetValue(recovered, value);
        }
        var actual = fields.ToDictionary(f => f.Name, f => f.GetValue(recovered));
        bool match = Newtonsoft.Json.Linq.JToken.DeepEquals(Newtonsoft.Json.Linq.JToken.FromObject(expected), Newtonsoft.Json.Linq.JToken.FromObject(actual));
        if (!match) throw new Exception("Managed setting values did not copy exactly");
        InputSystem.settings = recovered;
        var result = new { recovered = true, exactManagedValues = match, fieldCount = fields.Length,
            nativeIdentityRecreated = true, background = recovered.backgroundBehavior.ToString(), editorBehavior = recovered.editorInputBehaviorInPlayMode.ToString(),
            fields = expected, devices = InputSystem.devices.Select(d => new { d.deviceId, d.name, d.enabled }).ToArray() };
        Directory.CreateDirectory("AUTO_QA/Evidence"); File.WriteAllText("AUTO_QA/Evidence/native-input/take01-settings-recovery.json", Newtonsoft.Json.JsonConvert.SerializeObject(result, Newtonsoft.Json.Formatting.Indented));
        return new { recovered = true, exactManagedValues = match, fieldCount = fields.Length, nativeIdentityRecreated = true };
    }
}
