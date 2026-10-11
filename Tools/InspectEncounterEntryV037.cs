using System;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class InspectEncounterEntryV037
{
    public static object Read()
    {
        var battle=UnityEngine.Object.FindFirstObjectByType<Story1WreckBattle>();
        var film=battle!=null ? battle.Finale : null;
        if(film==null)return new{filmExists=false};
        var result=new System.Collections.Generic.Dictionary<string,object>();
        foreach(string name in new[]{"fromWater","swimSeconds","entryWalkSeconds","walkSeconds","entrySharkApproachSeconds","entrySharkRouteLength","initialNose","stageRotation","entrySharkRoute","startFeet","board"})
        {
            var field=typeof(Story1WreckFinale).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic);
            var value=field.GetValue(film);
            if(value is Story1WreckPlank plank)value=new{plank.name,size=plank.Dimensions.ToString(),position=plank.transform.position.ToString()};
            else if(value is Vector3 vector)value=vector.ToString();
            else if(value is Quaternion rotation)value=rotation.eulerAngles.ToString();
            else if(value is Vector3[] points)value=Array.ConvertAll(points,p=>p.ToString());
            result[name]=value;
        }
        result["LeadInSeconds"]=film.LeadInSeconds;result["CinematicTime"]=film.CinematicTime;
        Directory.CreateDirectory(".codex/encounter-v037");
        File.WriteAllText(".codex/encounter-v037/entry-inspection.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
        return result;
    }
}
