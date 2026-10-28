using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;

public static class AutoQaInspectNativeInputRestore
{
    public static object Run()
    {
        var flags = BindingFlags.Static | BindingFlags.NonPublic;
        var types = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("AutoQaNativeInputSmoke")).Where(t => t != null).ToArray();
        return types.Select(t => {
            object Field(string name) => t.GetField(name, flags).GetValue(null);
            var expected = ((ValueTuple<int, bool>[])Field("roster")) ?? Array.Empty<ValueTuple<int, bool>>();
            var actual = InputSystem.devices.Select(d => (d.deviceId, d.enabled)).ToArray();
            var oldKeyboard = Field("oldKeyboard") as Keyboard; var oldMouse = Field("oldMouse") as Mouse;
            var originalSettings = Field("oldSettings") as InputSettings;
            return new {
                assembly = t.Assembly.GetName().Name, armed = Field("armed"),
                takeFolder = Field("folder"),
                expected = expected.Select(d => new { id = d.Item1, enabled = d.Item2 }).ToArray(),
                actual = InputSystem.devices.Select(d => new { id = d.deviceId, d.name, d.enabled }).ToArray(),
                orderedMatch = expected.SequenceEqual(actual), membershipMatch = expected.OrderBy(d => d.Item1).SequenceEqual(actual.OrderBy(d => d.Item1)),
                settingsMatch = InputSystem.settings == Field("oldSettings") as InputSettings,
                originalSettingsId = originalSettings ? originalSettings.GetInstanceID() : 0,
                originalSettingsName = originalSettings ? originalSettings.name : null,
                originalSettingsPath = originalSettings ? UnityEditor.AssetDatabase.GetAssetPath(originalSettings) : null,
                currentSettingsId = InputSystem.settings ? InputSystem.settings.GetInstanceID() : 0,
                currentSettingsName = InputSystem.settings ? InputSystem.settings.name : null,
                currentSettingsPath = InputSystem.settings ? UnityEditor.AssetDatabase.GetAssetPath(InputSystem.settings) : null,
                originalSettingsFlags = originalSettings ? originalSettings.hideFlags.ToString() : null,
                currentSettingsFlags = InputSystem.settings ? InputSystem.settings.hideFlags.ToString() : null,
                expectedSettingsFlags = t.GetField("oldSettingsFlags", flags)?.GetValue(null)?.ToString(),
                originalBackground = originalSettings ? originalSettings.backgroundBehavior.ToString() : null,
                currentBackground = InputSystem.settings ? InputSystem.settings.backgroundBehavior.ToString() : null,
                keyboard = Keyboard.current?.deviceId, expectedKeyboard = oldKeyboard?.deviceId,
                mouse = Mouse.current?.deviceId, expectedMouse = oldMouse?.deviceId,
                captureDelta = Time.captureDeltaTime, expectedCapture = Field("oldCapture"),
                previewCleared = CombatFeelPreview.Player == null
            };
        }).ToArray();
    }
}
