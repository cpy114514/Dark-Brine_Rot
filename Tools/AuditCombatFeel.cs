using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Mavis;
public static class AuditCombatFeel
{
    public static object Read()
    {
        var p=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var a=p.GetComponent<SahurAttack>();
        return new {attack=EditorJsonUtility.ToJson(a),animators=p.GetComponentsInChildren<Animator>(true).Select(x=>new{path=AssetDatabase.GetAssetPath(x.runtimeAnimatorController),avatar=AssetDatabase.GetAssetPath(x.avatar)}).ToArray(),controllers=new[]{"Assets/Resources/Encounter/v006/SahurGameplay.controller","Assets/Game/Prefabs/Characters/Sahur/Animations/SahurGrounded.controller"}.Select(path=>new{path,layers=AssetDatabase.LoadAssetAtPath<AnimatorController>(path).layers.Select(l=>new{l.name,mask=AssetDatabase.GetAssetPath(l.avatarMask),states=l.stateMachine.states.Where(s=>s.state.name.Contains("Combo")||s.state.name.Contains("Heavy")||s.state.name.Contains("Charge")).Select(s=>new{s.state.name,s.state.speed,s.state.speedParameter,s.state.speedParameterActive,clip=AssetDatabase.GetAssetPath(s.state.motion),length=(s.state.motion as AnimationClip)?.length,exit=s.state.transitions.Select(t=>new{t.exitTime,t.duration}).ToArray()}).ToArray()}).ToArray()}).ToArray()};
    }
}
