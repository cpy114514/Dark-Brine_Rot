using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Mavis;
public static class TestCombatCleanupV245
{
    static void Check(bool value,string text,List<string> rows){if(!value)throw new Exception(text);rows.Add(text);}
    public static async Task<object> Run()
    {
        var rows=new List<string>();GameObject fixture=null;Material material=null;
        try
        {
            CombatFeelPreview.Spawn();await Task.Delay(500);
            fixture=GameObject.CreatePrimitive(PrimitiveType.Cube);fixture.name="Temporary material ownership fixture";fixture.AddComponent<GameSaveExcluded>();
            fixture.transform.position=CombatFeelPreview.Player.transform.position+Vector3.right*8;
            material=new Material(Shader.Find("Universal Render Pipeline/Lit"));material.color=new Color(.25f,.4f,.6f);
            var renderer=fixture.GetComponent<Renderer>();renderer.sharedMaterial=material;
            var flash=fixture.AddComponent<CombatDamageFlash>();var block=new MaterialPropertyBlock();
            renderer.GetPropertyBlock(block,0);Check(block.isEmpty,"Fixture begins without material property overrides",rows);
            flash.Hit(.8f);await Task.Delay(50);renderer.GetPropertyBlock(block,0);
            Check(block.HasProperty("_BaseColor"),"Received contact actually reaches rendered material properties",rows);
            await Task.Delay(180);renderer.GetPropertyBlock(block,0);
            Check(block.isEmpty,"Expired flash restores the original empty property block",rows);
            Color rest=new Color(.2f,.3f,.4f);block.SetColor("_BaseColor",rest);block.SetFloat("_Smoothness",.27f);renderer.SetPropertyBlock(block,0);
            flash.Hit(1);await Task.Delay(35);renderer.GetPropertyBlock(block,0);block.SetFloat("_Smoothness",.51f);renderer.SetPropertyBlock(block,0);
            await Task.Delay(180);renderer.GetPropertyBlock(block,0);
            Check(block.GetColor("_BaseColor").Equals(rest)&&Mathf.Abs(block.GetFloat("_Smoothness")-.51f)<.0001f,"Flash restores pre-existing tint while retaining another system's property update",rows);
            flash.Hit(1);await Task.Delay(35);flash.enabled=false;renderer.GetPropertyBlock(block,0);
            Check(block.GetColor("_BaseColor").Equals(rest),"Disabling flash immediately releases its visual ownership",rows);
            flash.Hit(1);Check(!flash.IsFlashing,"Disabled/dead presentation does not latch a fresh flash",rows);
            var player=CombatFeelPreview.Player;var hp=player.GetComponent<PlayerHealth>();if(!hp)hp=player.AddComponent<PlayerHealth>();
            hp.currentHealth=2;hp.NarrativeHealthFloor=0;hp.NarrativeDefeatHandler=null;
            hp.ApplyAttackDamage(30,player.GetComponent<CharacterController>().bounds.center,player.transform.position+Vector3.forward*3);
            await Task.Delay(70);
            var death=player.GetComponent<PlayerDeathRespawn>();var motor=player.GetComponent<ThirdPersonPlayerController>();
            Check(hp.currentHealth==0&&death&&death.IsDead,"Real hostile fatal hit still enters the native death flow",rows);
            Check(!CombatLocalHitStop.Active(player)&&(Mathf.Approximately(motor.CharacterAnimator.speed,0)||player.GetComponent<SahurRifleAnimation>().DeathAnimationPlaying),"Death owns Animator freeze after local impact stop is released",rows);
            Check(!player.GetComponent<CombatReceivedReaction>().enabled&&!player.GetComponent<CombatDamageFlash>().enabled,"Death detaches hurt overlay and clears material response",rows);
            var data=new{passed=true,checks=rows};File.WriteAllText(".codex/combat-recovery-20261023/cleanup-tests.json",Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented));return data;
        }
        catch(Exception e){File.WriteAllText(".codex/combat-recovery-20261023/cleanup-failure.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{checks=rows,error=e.ToString()},Newtonsoft.Json.Formatting.Indented));throw;}
        finally{if(fixture)UnityEngine.Object.DestroyImmediate(fixture);if(material)UnityEngine.Object.DestroyImmediate(material);CombatFeelPreview.Clear();}
    }
}
