using System;
using System.IO;
using System.Reflection;
using UnityEngine;
public static class VerifyBoatTimingV248
{
 public static object Run()
 {
  var seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
  if(!Application.isPlaying||seq==null)throw new Exception("Native story Play required");
  var battle=seq.GetComponent<Story1WreckBattle>();
  float phase=(float)typeof(Story1WreckBattle).GetField("redShipIdleStart",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(battle);
  float alert=Story1SahurPassenger.OpeningFidgetSeconds-Story1SahurPassenger.OpeningLeadInSeconds;
  var data=new{passed=battle.RedesignActive&&seq.VoyageTime<=.4f&&phase+alert>=2.9f&&phase+alert<=3.1f,
   requestedSeconds=Story1SahurPassenger.OpeningFidgetSeconds,leadIn=seq.VoyageTime,passengerPhaseAtHandoff=phase,warningFidgetSeconds=alert,totalFidgetSeconds=phase+alert,
   stage=battle.RedesignStage.ToString(),clock=battle.RedesignClock,events=battle.RedesignEvents,
   note="Natural native voyage-to-warning handoff, not a manually forced BeginImpact. Existing4.4s source is sampled only through this3s action; subsequent guard blend and shark timing retained."};
  string json=Newtonsoft.Json.JsonConvert.SerializeObject(data,Newtonsoft.Json.Formatting.Indented);
  File.WriteAllText(".codex/boat-fidget-20261024/three-second-v248/timing.json",json);
  if(!data.passed)throw new Exception(json);return json;
 }
}
