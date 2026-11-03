using UnityEditor;
public static class DeferredCompilation060
{
    static int old;
    public static object Start(){old=EditorPrefs.GetInt("ScriptCompilationDuringPlay",0);EditorPrefs.SetInt("ScriptCompilationDuringPlay",2);EditorApplication.playModeStateChanged+=Restore;return new{previous=old};}
    static void Restore(PlayModeStateChange state){if(state!=PlayModeStateChange.ExitingPlayMode)return;EditorPrefs.SetInt("ScriptCompilationDuringPlay",old);EditorApplication.playModeStateChanged-=Restore;}
}
