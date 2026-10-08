using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

public static class ProjectAnimationAudit
{
    public static object Inspect()
    {
        string[] models={"Assets/Game/Prefabs/Characters/Sahur/Models/sahur.fbx", "Assets/Game/Prefabs/Characters/Sahur/Models/PbrSahur.fbx",
            "Assets/Game/Prefabs/Characters/Sahur/Animations/Source/StandingMeleeAttackDownward.fbx", "Assets/Resources/SharkAnimation/TralaleroAnimated.fbx"};
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/Characters/Sahur/SahurPlayer.prefab");
        var animator=prefab.GetComponentInChildren<Animator>(true);
        var controller=animator.runtimeAnimatorController as AnimatorController;
        var attack=prefab.GetComponent<Mavis.SahurAttack>();
        return new {unity=Application.unityVersion,
            models=models.Select(path=>{var i=AssetImporter.GetAtPath(path) as ModelImporter;
                return new{path,exists=i!=null,type=i!=null?i.animationType.ToString():null,
                    scale=i!=null?i.globalScale:0,avatar=i!=null?i.avatarSetup.ToString():null,
                    clips=AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__"))
                        .Select(c=>new{c.name,c.frameRate,c.length,c.isLooping}).ToArray()};}).ToArray(),
            player=new{prefab=prefab.name,avatar=animator.avatar!=null?animator.avatar.name:null,
                avatarValid=animator.avatar!=null&&animator.avatar.isValid,avatarHuman=animator.avatar!=null&&animator.avatar.isHuman,
                avatarSource=animator.avatar!=null?AssetDatabase.GetAssetPath(animator.avatar):null,
                animator.applyRootMotion,controller=AssetDatabase.GetAssetPath(controller),
                layers=controller.layers.Select(l=>new{l.name,l.defaultWeight,mask=l.avatarMask!=null?AssetDatabase.GetAssetPath(l.avatarMask):null,
                    states=l.stateMachine.states.Select(s=>new{s.state.name,motion=s.state.motion!=null?AssetDatabase.GetAssetPath(s.state.motion):null}).ToArray()}).ToArray(),
                charge=AssetDatabase.GetAssetPath(attack.chargeClip),strike=AssetDatabase.GetAssetPath(attack.chargedStrikeClip),
                attack.fullChargeTime,attack.chargeWindupTime,attack.maxChargePosePhase,
                attack.heavySwingWindowStart,attack.heavySwingWindowEnd},
            clips=AssetDatabase.FindAssets("t:AnimationClip",new[]{"Assets/Game/Prefabs/Characters/Sahur/Animations/Gameplay"})
                .Select(AssetDatabase.GUIDToAssetPath).Where(p=>p.EndsWith(".anim")).Select(p=>{
                    var c=AssetDatabase.LoadAssetAtPath<AnimationClip>(p);return new{path=p,c.name,c.frameRate,c.length,c.isLooping};}).ToArray()};
    }
}
