using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
public static class AuditLightV062
{
    public static object Inspect()
    {
        return new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller",
            "Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller"}
            .Select(path=>{var c=AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
                return new{path,layers=c.layers.Select(l=>new{l.name,l.defaultWeight,
                    mask=AssetDatabase.GetAssetPath(l.avatarMask),
                    states=l.stateMachine.states.Where(s=>s.state.name.StartsWith("Combo "))
                    .Select(s=>new{s.state.name,s.state.speed,motion=s.state.motion.name,
                        path=AssetDatabase.GetAssetPath(s.state.motion)}).ToArray()}).ToArray()};}).ToArray();
    }
}
