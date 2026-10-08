using UnityEngine;

public enum Story1EncounterAction { Guard,Parry,Slash,Surf,Hop,Throw,Dive,Brace,Stagger,Miss,Fall }
[System.Serializable]
public struct Story1EncounterContact
{
    public string label,actorMarker,targetMarker;
    public float time,window;
    public Story1EncounterContact(string label,float time,string actorMarker,string targetMarker)
    {this.label=label;this.time=time;window=.16f;this.actorMarker=actorMarker;this.targetMarker=targetMarker;}
}
[System.Serializable]
public struct Story1EncounterBeat
{
    public string label,clip;
    public float start,end;
    public Story1EncounterAction action;
    public Story1EncounterBeat(string label,float start,float end,Story1EncounterAction action,string clip)
    {this.label=label;this.start=start;this.end=end;this.action=action;this.clip=clip;}
}
/// <summary>Shared pose, traversal and contact clock for the approved paired film.</summary>
public sealed class Story1EncounterDefinition : ScriptableObject
{
    public float duration=20.8f;
    public int fps=60;
    public Story1EncounterBeat[] beats;
    public Story1EncounterContact[] contacts=ApprovedContacts();
    public static Story1EncounterContact[] ApprovedContacts()=>new[]
    {
        new Story1EncounterContact("Probe parry",1.184f,"Right-hand weapon capsule","Posed shark head capsule"),
        new Story1EncounterContact("First diagonal counter",2.43f,"Right-hand weapon capsule","Posed shark head capsule"),
        new Story1EncounterContact("Landing diagonal counter",7.03f,"Right-hand weapon capsule","Posed shark head capsule"),
        new Story1EncounterContact("Thrown stick",8.94f,"Actual thrown weapon shape","Posed shark head capsule"),
        new Story1EncounterContact("Reverse the advantage",14.572f,"Tail_Marker","BodyHitbox_Torso"),
        new Story1EncounterContact("Break guard and support",18.05f,"Tail_Marker","BodyHitbox_Torso")
    };
    public static Story1EncounterBeat[] ApprovedBeats()=>new[]
    {
        new Story1EncounterBeat("Measure the bite",0,.7f,Story1EncounterAction.Guard,"CH1_SahurGuard_v006"),
        new Story1EncounterBeat("Sidestep and parry",.7f,1.8f,Story1EncounterAction.Parry,"CH1_SahurParry_v006"),
        new Story1EncounterBeat("Answer the probe",1.8f,3.2f,Story1EncounterAction.Slash,"CH1_SahurSlash_v006"),
        new Story1EncounterBeat("Recover the guard",3.2f,4,Story1EncounterAction.Guard,"CH1_SahurGuard_v006"),
        new Story1EncounterBeat("Drive clear of the bite",4,4.55f,Story1EncounterAction.Surf,"PLAYER_BoardDrive_v006"),
        new Story1EncounterBeat("Push off and change support",4.55f,6.4f,Story1EncounterAction.Hop,"PLAYER_JumpStart_v006"),
        new Story1EncounterBeat("Landing diagonal counter",6.4f,7.8f,Story1EncounterAction.Slash,"CH1_SahurSlash_v006"),
        new Story1EncounterBeat("Throw and retrieve",7.8f,9.8f,Story1EncounterAction.Throw,"PLAYER_Throw_v006"),
        new Story1EncounterBeat("Hold the advantage",9.8f,10,Story1EncounterAction.Guard,"CH1_SahurGuard_v006"),
        new Story1EncounterBeat("Track the submerged flank",10,11.6f,Story1EncounterAction.Dive,"CH1_SahurGuard_v006"),
        new Story1EncounterBeat("Leap away from the breach",11.6f,14,Story1EncounterAction.Hop,"PLAYER_JumpStart_v006"),
        new Story1EncounterBeat("Defend before settling",14,14.65f,Story1EncounterAction.Brace,"CH1_SahurBrace_v006"),
        new Story1EncounterBeat("Chest yields then hips",14.65f,15.4f,Story1EncounterAction.Stagger,"PLAYER_Stagger_v006"),
        new Story1EncounterBeat("Recover after the tail",15.4f,16,Story1EncounterAction.Guard,"CH1_SahurGuard_v006"),
        new Story1EncounterBeat("Final counter misses",16,17.4f,Story1EncounterAction.Miss,"CH1_SahurSlash_v006"),
        new Story1EncounterBeat("Tail overwhelms the guard",17.4f,18.05f,Story1EncounterAction.Brace,"CH1_SahurBrace_v006"),
        new Story1EncounterBeat("Lose the stick and fall",18.05f,20.8f,Story1EncounterAction.Fall,"PLAYER_Fall_v006")
    };
}
