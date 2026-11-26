using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Mavis;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyGenericThreat043
{
    public static object Run()
    {
        if(Application.isPlaying)throw new Exception("Stopped handoff required");
        var scene=EditorSceneManager.NewPreviewScene();
        var controller=new AnimatorController();var clip=new AnimationClip{name="Owned generic attack clock"};
        try
        {
            var enemy=new GameObject("Owned generic forecast regression",typeof(Animator),typeof(EnemyAttack));SceneManager.MoveGameObjectToScene(enemy,scene);
            enemy.transform.position=new Vector3(5000,1000,5000);
            var player=new GameObject("Owned previous victim",typeof(CharacterController),typeof(PlayerHealth));SceneManager.MoveGameObjectToScene(player,scene);
            player.transform.position=enemy.transform.position+Vector3.forward*1.1f;
            var body=player.GetComponent<CharacterController>();body.center=Vector3.up;body.height=2;body.radius=.3f;
            var hitbox=new GameObject("Owned attack volume",typeof(BoxCollider));SceneManager.MoveGameObjectToScene(hitbox,scene);hitbox.transform.SetParent(enemy.transform,false);hitbox.transform.localPosition=new Vector3(0,1,1);
            controller.AddLayer("Base Layer");var machine=controller.layers[0].stateMachine;var state=machine.AddState("Attack");machine.defaultState=state;
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Transform),"m_LocalPosition.x"),AnimationCurve.Linear(0,5000,1,5000));
            state.motion=clip;
            var animator=enemy.GetComponent<Animator>();animator.runtimeAnimatorController=controller;animator.Rebind();animator.Update(0);animator.Play("Attack",0,.02f);animator.Update(0);
            var attack=enemy.GetComponent<EnemyAttack>();attack.animator=animator;attack.attackHitbox=hitbox.GetComponent<Collider>();
            var health=player.GetComponent<PlayerHealth>();
            var flags=BindingFlags.Instance|BindingFlags.NonPublic;
            ((HashSet<IDamageable>)typeof(EnemyAttack).GetField("victims",flags).GetValue(attack)).Add(health);
            enemy.transform.position=new Vector3(5000,1000,5000);Physics.SyncTransforms();
            bool newWindup=attack.ForecastDodge(health,.28f,out var threat);
            if(!newWindup){var st=animator.GetCurrentAnimatorStateInfo(0);throw new Exception("New windup rejected: state="+st.fullPathHash+" attack="+st.IsName("Attack")+" phase="+st.normalizedTime+" length="+st.length+" active="+attack.isActiveAndEnabled+" root="+enemy.transform.position+" feet="+body.bounds.min.y+" player="+player.transform.position);}
            typeof(EnemyAttack).GetField("windowActive",flags).SetValue(attack,true);
            animator.Play("Attack",0,.2f);animator.Update(0);enemy.transform.position=new Vector3(5000,1000,5000);Physics.SyncTransforms();
            bool alreadyHit=attack.ForecastDodge(health,.28f,out _);
            if(alreadyHit)throw new Exception("Current strike victim incorrectly received a second opportunity");
            var result=new{passed=true,newWindup,alreadyHit,seconds=threat.seconds};
            File.WriteAllText(".codex/perfect-dodge043/generic-cycle-regression.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));return result;
        }
        finally{EditorSceneManager.ClosePreviewScene(scene);UnityEngine.Object.DestroyImmediate(controller);UnityEngine.Object.DestroyImmediate(clip);}
    }
}
