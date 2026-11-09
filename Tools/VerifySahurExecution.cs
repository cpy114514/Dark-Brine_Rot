using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Mavis;
public static class VerifySahurExecution
{
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Stopped editor required");
        var original=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
        var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);
        var actor=new GameObject("Execution verifier player");
        var enemy=new GameObject("Execution verifier Cappuccino");
        var wall=new GameObject("Execution verifier wall");
        try
        {
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(actor,scene);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(enemy,scene);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(wall,scene);
            actor.transform.position=new Vector3(10000,10000,10000);
            enemy.transform.position=actor.transform.position+new Vector3(0,0,2);
            wall.transform.position=actor.transform.position+new Vector3(0,1,1);
            var cc=actor.AddComponent<CharacterController>();cc.center=Vector3.up;cc.height=2;
            var collider=enemy.AddComponent<CapsuleCollider>();collider.center=Vector3.up;collider.height=2;
            var boss=enemy.AddComponent<CappuccinoAI>();
            typeof(CappuccinoAI).GetProperty("FightActive").GetSetMethod(true).Invoke(boss,new object[]{true});
            var hp=enemy.GetComponent<Health>() ?? enemy.AddComponent<Health>();hp.maxHealth=100;hp.currentHealth=15;
            Physics.SyncTransforms();
            bool threshold=ExecutionTarget.IsEligible(hp,actor.transform,3.2f);
            hp.currentHealth=16;bool above=!ExecutionTarget.IsEligible(hp,actor.transform,3.2f);hp.currentHealth=15;
            var chicken=enemy.AddComponent<ChickenBroAI>();
            bool excludedChicken=!ExecutionTarget.IsEligible(hp,actor.transform,3.2f);
            UnityEngine.Object.DestroyImmediate(chicken);
            enemy.transform.position=actor.transform.position+new Vector3(0,0,8);Physics.SyncTransforms();
            bool distance=!ExecutionTarget.IsEligible(hp,actor.transform,3.2f);
            enemy.transform.position=actor.transform.position+new Vector3(0,0,2);var block=wall.AddComponent<BoxCollider>();block.size=new Vector3(4,4,.2f);Physics.SyncTransforms();
            bool blocked=!ExecutionTarget.IsEligible(hp,actor.transform,3.2f);block.enabled=false;Physics.SyncTransforms();
            int death=0;hp.OnDeath=new UnityEngine.Events.UnityEvent();hp.OnDeath.AddListener(()=>death++);
            bool killed=ExecutionTarget.Finish(hp,enemy.transform.position);bool once=death==1&&!ExecutionTarget.Finish(hp,enemy.transform.position)&&death==1;
            var result=new{threshold,above,excludedChicken,distance,blocked,killed,once,passed=threshold&&above&&excludedChicken&&distance&&blocked&&killed&&once};
            if(!result.passed)throw new Exception(Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result;
        }
        finally {EditorSceneManager.CloseScene(scene,true);UnityEngine.SceneManagement.SceneManager.SetActiveScene(original);}
    }
}

