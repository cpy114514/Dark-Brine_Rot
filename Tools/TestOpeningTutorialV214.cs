using System;
using System.Collections.Generic;
using System.Reflection;
using Mavis;
using UnityEngine;

/// <summary>Isolated Edit Mode checks; never starts the Editor, a scene, or a save operation.</summary>
public static class TestOpeningTutorialV214
{
    const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    static void Set(object target,string name,object value)
    {
        var type=target.GetType();
        var field=type.GetField(name,Private)??type.GetField("<"+name+">k__BackingField",Private);
        if(field==null)throw new Exception("Missing test seam "+name);
        field.SetValue(target,value);
    }
    static void Call(object target,string name,params object[] args)=>target.GetType().GetMethod(name,Private).Invoke(target,args);
    static void Near(float actual,float expected,string label)
    {
        if(Mathf.Abs(actual-expected)>.0001f)throw new Exception(label+": expected "+expected+", got "+actual);
    }
    public static object Run()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Run these isolated checks in Edit Mode; keep the live game unchanged.");
        if(PauseSettingsMenu.IsOpen||SahurLoadoutUI.BlocksInput)throw new InvalidOperationException("Close existing overlays before isolated pause checks.");
        var passed=new List<string>();float originalScale=Time.timeScale;
        GameObject root=null;
        try
        {
            root=new GameObject("Opening tutorial isolated validation");root.hideFlags=HideFlags.HideAndDontSave;
            var health=root.AddComponent<PlayerHealth>();var battle=root.AddComponent<Story1WreckBattle>();
            Set(battle,"playerHealth",health);
            foreach(float entry in new[]{100f,5f,.01f})
            {
                health.currentHealth=entry;health.maxHealth=100;
                float floor=entry*.35f,budget=entry-floor;
                health.NarrativeHealthFloor=floor;
                Set(battle,"TutorialHealthFloor",floor);Set(battle,"TutorialDamageBudget",budget);Set(battle,"TutorialDamage",0f);
                int deaths=0;UnityEngine.Events.UnityAction death=()=>deaths++;health.OnDeath.AddListener(death);
                for(int i=0;i<100;i++)Call(battle,"RedTutorialDamage",1000f,Vector3.zero);
                Near(health.currentHealth,floor,"Surviving repeated hits at "+entry+" HP");
                Near(battle.TutorialDamage,budget,"Cumulative damage at "+entry+" HP");
                if(deaths!=0)throw new Exception("Tutorial fired death at "+entry+" HP");
                health.OnDeath.RemoveListener(death);
            }
            passed.Add("100 repeated oversized hits preserve positive entry-based floor at 100, 5 and 0.01 HP");

            health.currentHealth=100;health.NarrativeHealthFloor=35;
            Set(battle,"TutorialHealthFloor",35f);Set(battle,"TutorialDamageBudget",65f);Set(battle,"TutorialDamage",0f);
            Call(battle,"RedTutorialDamage",40f,Vector3.zero);
            health.currentHealth=100; // An independent healing event must not refund the shark's spent budget.
            Call(battle,"RedTutorialDamage",40f,Vector3.zero);
            Near(health.currentHealth,75,"Healing does not refund spent budget");Near(battle.TutorialDamage,65,"Budget across healing");
            passed.Add("Healing does not allow cumulative tutorial damage to exceed entry budget");

            Set(battle,"TutorialDamage",0f);health.currentHealth=100;health.GrantProtection(100);
            Call(battle,"RedTutorialDamage",12f,Vector3.zero);
            Near(health.currentHealth,100,"Existing damage protection");Near(battle.TutorialDamage,0,"Protected hit budget");
            health.GrantProtection(0);
            health.currentHealth=5;Call(battle,"RedTutorialDamage",12f,Vector3.zero);
            Near(health.currentHealth,5,"Health below floor must not be raised by damage");
            passed.Add("Protected hits spend no damage budget and damage cannot heal an already lower HP value");

            Set(battle,"ownsPause",true);Set(battle,"storedTimeScale",.4f);Time.timeScale=0;
            Call(battle,"ReleaseRedesignPause");Near(Time.timeScale,.4f,"Owned pause release");
            Time.timeScale=0;Call(battle,"ReleaseRedesignPause");Near(Time.timeScale,0,"Repeated release must not clear someone else's pause");
            passed.Add("Pause release restores the captured speed once and never releases an unowned pause");
            return new{passed=true,checks=passed};
        }
        finally
        {
            if(root!=null)UnityEngine.Object.DestroyImmediate(root);
            Time.timeScale=originalScale;
        }
    }
}
