using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Shared keyboard bindings for Sahur; stored separately from the menu prefab.</summary>
public static class GameInputSettings
{
    public enum Action { Forward, Back, Left, Right, Sprint, Jump, Dodge, Heal }

    static readonly Key[] Defaults =
    {
        Key.W, Key.S, Key.A, Key.D, Key.LeftShift, Key.Space, Key.LeftCtrl, Key.H
    };

    static readonly Key[] Current = (Key[])Defaults.Clone();
    static bool loaded;

    public static Key Default(Action action) => Defaults[(int)action];

    public static Key Get(Action action)
    {
        Load();
        return Current[(int)action];
    }

    public static void Set(Action action, Key key)
    {
        Load();
        Current[(int)action] = key;
        PlayerPrefs.SetInt("DarkBrine.Settings.Key." + action, (int)key);
    }

    public static bool Pressed(Action action)
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard[Get(action)].isPressed;
    }

    public static bool PressedThisFrame(Action action)
    {
        var keyboard = Keyboard.current;
        return keyboard != null && keyboard[Get(action)].wasPressedThisFrame;
    }

    static void Load()
    {
        if (loaded) return;
        loaded = true;
        for (int i = 0; i < Current.Length; i++)
        {
            Key candidate = (Key)PlayerPrefs.GetInt("DarkBrine.Settings.Key." + (Action)i, (int)Defaults[i]);
            Current[i] = candidate != Key.None && Enum.IsDefined(typeof(Key), candidate) ? candidate : Defaults[i];
        }
    }
}
