using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
public static class FocusChargeGameViewV049
{
    public static object Focus()
    {
        var view=Resources.FindObjectsOfTypeAll<EditorWindow>().FirstOrDefault(w=>w.GetType().Name=="GameView");
        if(view==null)
        {
            var type=typeof(EditorWindow).Assembly.GetType("UnityEditor.GameView");
            if(type==null)throw new Exception("Native Game View type unavailable");
            view=EditorWindow.GetWindow(type);
        }
        view.Focus();Cursor.lockState=CursorLockMode.Locked;
        return new{view=view.GetType().Name,Application.isFocused,cursor=Cursor.lockState.ToString()};
    }
}
