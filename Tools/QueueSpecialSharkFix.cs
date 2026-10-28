using System;
using System.IO;
using UnityEditor;
using UnityEngine;
public static class QueueSpecialSharkFix {
 const string FilePath="Assets/Scripts/Story/Chapter1/Story1WreckBattle.Tutorial.cs";
 const string Status=".codex/tutorial-special-shark/status.txt";
 static void Commit(PlayModeStateChange state){
  if(state!=PlayModeStateChange.EnteredEditMode)return;
  try{
   string s=File.ReadAllText(FilePath);
   const string field="    Vector3 redDodgeDirection;";
   const string oldNose="        Vector3 nose=feet-redForward*30;nose.y=RedWater(nose)-8;";
   const string oldMove="        RedTutorialShark(\"Swim\",redesignTime,nose,redForward);RedKeepTutorialSharkClear();";
   const string oldFinish="            redReleased=false;lessonExecuting=false;lessonTime=0;stowReady=false;CurrentLesson=Lesson.Jump;";
   if(!s.Contains(field)||!s.Contains(oldNose)||!s.Contains(oldMove)||!s.Contains(oldFinish)||s.Contains("redSpecialStartCaptured"))throw new Exception("Source changed; pending fix was not applied");
   string nl=s.Contains("\r\n")?"\r\n":"\n";
   s=s.Replace(field,field+nl+"    Vector3 redSpecialStartLocal;"+nl+"    bool redSpecialStartCaptured;");
   s=s.Replace(oldNose,"        if(!redSpecialStartCaptured)"+nl+"        {"+nl+"            redSpecialStartLocal=redCombatBoard.transform.InverseTransformPoint(sharkAnimator.NoseWorldPoint);"+nl+"            redSpecialStartCaptured=true;"+nl+"        }"+nl+"        // Keep the shark visible in open water and move from its real previous pose."+nl+"        Vector3 destination=feet+redForward*14;destination.y=RedWater(destination)+1.2f;"+nl+"        Vector3 start=redCombatBoard.transform.TransformPoint(redSpecialStartLocal);"+nl+"        Vector3 nose=Vector3.Lerp(start,destination,REase(seconds,0,.6f));");
   s=s.Replace(oldMove,"        RedTutorialShark(\"Swim\",redesignTime,nose,-redForward);RedKeepTutorialSharkClear();");
   s=s.Replace(oldFinish,"            redReleased=false;redSpecialStartCaptured=false;lessonExecuting=false;lessonTime=0;stowReady=false;CurrentLesson=Lesson.Jump;");
   File.WriteAllText(FilePath,s,new System.Text.UTF8Encoding(false));File.WriteAllText(Status,"applied; native gameplay verification still required");Debug.Log("Queued E-tutorial shark visibility fix applied; gameplay verification pending.");
  }catch(Exception e){File.WriteAllText(Status,"not applied: "+e.Message);Debug.LogError("Queued E-tutorial shark fix was not applied: "+e.Message);}
  finally{EditorApplication.playModeStateChanged-=Commit;}
 }
 public static object Run(){Directory.CreateDirectory(Path.GetDirectoryName(Status));if(EditorApplication.isPlaying){EditorApplication.playModeStateChanged-=Commit;EditorApplication.playModeStateChanged+=Commit;File.WriteAllText(Status,"queued until Edit mode; source unchanged");return new{queued=true,reason="Preserve current gameplay",path=FilePath};}Commit(PlayModeStateChange.EnteredEditMode);return new{queued=false,status=File.ReadAllText(Status)};}
}
