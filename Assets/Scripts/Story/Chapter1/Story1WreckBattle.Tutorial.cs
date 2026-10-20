using System.Collections.Generic;
using UnityEngine;

public sealed partial class Story1WreckBattle
{
    Vector3 redDodgeStartLocal,redDodgeEndLocal,redDodgeAttackLocal;
    Vector3 redDodgeDirection;
    float redSwimControlSeconds;
    bool redTutorialEnvelopeReady;
    IEnumerator<object> redTutorialEnvelopeWork;
    Vector3[] redTutorialSharkVertices;
    readonly List<RedWaterCapsule> redTutorialBodySpace=new List<RedWaterCapsule>(13);
    public float RedesignTutorialHeroGap { get; private set; }=float.PositiveInfinity;
    public int RedesignDodgeCandidates { get; private set; }
    public float RedesignDodgeTravel { get; private set; }
    public float RedesignSwimControlSeconds=>redSwimControlSeconds;
    public double RedesignTutorialClearanceMs { get; private set; }
    public double RedesignDodgePlanMs { get; private set; }
    public double RedesignTutorialPreparationMs { get; private set; }

    void RedPrepareTutorialBody()
    {
        if(redTutorialEnvelopeReady)return;
        if(redTutorialEnvelopeWork==null)redTutorialEnvelopeWork=RedMeasureTutorialBody().GetEnumerator();
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        do{if(!redTutorialEnvelopeWork.MoveNext()){redTutorialEnvelopeWork.Dispose();redTutorialEnvelopeWork=null;break;}}
        while(RedRiggingElapsed(started)<1.5);
        RedesignTutorialPreparationMs=System.Math.Max(RedesignTutorialPreparationMs,RedRiggingElapsed(started));
    }
    IEnumerable<object> RedMeasureTutorialBody()
    {
        RedWaterReadBody();var body=redWaterCapsules.ToArray();var radii=new float[body.Length];
        for(int i=0;i<body.Length;i++)radii[i]=body[i].radius;
        var skins=player.GetComponentsInChildren<SkinnedMeshRenderer>();
        foreach(var skin in skins)
        {
            RedWaterReadBody();body=redWaterCapsules.ToArray();
            // Keep the bake and the bone envelope in the same sampled pose.
            var mesh=new Mesh();skin.BakeMesh(mesh,true);var matrix=skin.transform.localToWorldMatrix;var vertices=mesh.vertices;Destroy(mesh);
            yield return null;
            for(int vertex=0;vertex<vertices.Length;vertex++)
            {
                Vector3 point=matrix.MultiplyPoint3x4(vertices[vertex]);int nearest=-1;float best=float.PositiveInfinity;
                for(int i=0;i<body.Length;i++)
                {
                    Vector3 axis=body[i].b-body[i].a;
                    Vector3 closest=body[i].a+axis*Mathf.Clamp01(Vector3.Dot(point-body[i].a,axis)/Mathf.Max(.000001f,axis.sqrMagnitude));
                    float distance=(point-closest).sqrMagnitude;if(distance<best){best=distance;nearest=i;}
                }
                if(nearest>=0)radii[nearest]=Mathf.Max(radii[nearest],Mathf.Sqrt(best)+.06f);
                if((vertex&255)==255)yield return null;
            }
        }
        redWaterBodyRadii=radii;redTutorialEnvelopeReady=true;
    }

    void RedTutorialBody()
    {
        RedWaterReadBody();
        if(!redTutorialEnvelopeReady){RedWaterMeasureNativeEnvelope();redTutorialEnvelopeReady=true;RedWaterReadBody();}
    }
    void RedKeepTutorialSharkClear(float margin=.3f)
    {
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        try{RedSeparateTutorialShark(margin);}
        finally{RedesignTutorialClearanceMs=RedRiggingElapsed(started);}
    }
    static readonly Unity.Profiling.ProfilerMarker RedSharkHeroMarker=new Unity.Profiling.ProfilerMarker("Opening.SharkHero");
    void RedSeparateTutorialShark(float margin,Vector3? escape=null)
    {
        using var measured=RedSharkHeroMarker.Auto();
        var skin=RedContactSkin();if(skin==null||redRiggingSkinTree==null)return;
        RedTutorialBody();
        if(redTutorialSharkVertices==null||redTutorialSharkVertices.Length!=redPosedVertices.Count)
            redTutorialSharkVertices=new Vector3[redPosedVertices.Count];
        var matrix=skin.transform.localToWorldMatrix;var inverse=skin.transform.worldToLocalMatrix;
        Vector3 x=matrix.MultiplyVector(Vector3.right),y=matrix.MultiplyVector(Vector3.up),z=matrix.MultiplyVector(Vector3.forward);
        float scale=x.magnitude;
        bool local=scale>.00001f&&Mathf.Abs(y.magnitude-scale)<scale*.00001f&&Mathf.Abs(z.magnitude-scale)<scale*.00001f&&
            Mathf.Abs(Vector3.Dot(x,y))<scale*scale*.00001f&&Mathf.Abs(Vector3.Dot(x,z))<scale*scale*.00001f&&Mathf.Abs(Vector3.Dot(y,z))<scale*scale*.00001f;
        // A uniform rigid transform preserves all capsule/triangle distances.
        // Keeping the tree local avoids rebuilding it for root travel/turns.
        if(!local){scale=1;inverse=Matrix4x4.identity;}
        margin/=scale;
        redTutorialBodySpace.Clear();
        foreach(var capsule in redWaterCapsules)redTutorialBodySpace.Add(new RedWaterCapsule(inverse.MultiplyPoint3x4(capsule.a),inverse.MultiplyPoint3x4(capsule.b),capsule.radius/scale));
        Bounds bounds=new Bounds(local?redPosedVertices[0]:matrix.MultiplyPoint3x4(redPosedVertices[0]),Vector3.zero);
        for(int i=0;i<redPosedVertices.Count;i++){redTutorialSharkVertices[i]=local?redPosedVertices[i]:matrix.MultiplyPoint3x4(redPosedVertices[i]);bounds.Encapsulate(redTutorialSharkVertices[i]);}
        // A conservative bound can prove separation without refitting and
        // traversing the full triangle tree while the shark is retreating.
        float lowerGap=float.PositiveInfinity;
        foreach(var capsule in redTutorialBodySpace)
        {
            Vector3 lo=Vector3.Min(capsule.a,capsule.b),hi=Vector3.Max(capsule.a,capsule.b);
            Vector3 delta=Vector3.Max(Vector3.zero,Vector3.Max(lo-bounds.max,bounds.min-hi));
            lowerGap=Mathf.Min(lowerGap,delta.magnitude-capsule.radius);
        }
        if(lowerGap>=margin){RedesignTutorialHeroGap=lowerGap*scale;return;}
        RedRiggingTree tree;
        if(local)tree=RedSolidIndex(skin);
        else
        {
            tree=redRiggingSkinTree;tree.RefitArticulated(redTutorialSharkVertices,.15f);
            redSolidSkinFrame=-1; // The nonuniform-scale fallback uses world space.
        }
        Vector3 offset=Vector3.zero,forward=inverse.MultiplyVector(shark.forward).normalized;
        Vector3 away=escape.HasValue&&escape.Value.sqrMagnitude>.01f?inverse.MultiplyVector(escape.Value).normalized:-forward;
        // The shark can continue its committed attack only while the actual
        // posed skin stays clear of the hero's measured full-body envelope.
        // Query translations against this one posed tree, avoiding extra bakes.
        for(int attempt=0;attempt<32;attempt++)
        {
            float gap=float.PositiveInfinity;
            foreach(var capsule in redTutorialBodySpace)
                gap=Mathf.Min(gap,tree.CapsuleGapUpTo(capsule.a-offset,capsule.b-offset,capsule.radius,margin));
            RedesignTutorialHeroGap=gap*scale;
            if(gap>=margin)break;
            offset+=away*Mathf.Clamp(margin-gap+.03f/scale,.15f/scale,3/scale);
        }
        if(RedesignTutorialHeroGap<margin*scale)
        {
            float front=float.NegativeInfinity,near=float.PositiveInfinity;
            foreach(var point in redTutorialSharkVertices)front=Mathf.Max(front,Vector3.Dot(point,forward));
            foreach(var capsule in redTutorialBodySpace)near=Mathf.Min(near,Mathf.Min(Vector3.Dot(capsule.a,forward),Vector3.Dot(capsule.b,forward))-capsule.radius);
            offset-=forward*Mathf.Max(0,front+Vector3.Dot(offset,forward)-near+margin+.03f/scale);
            RedesignTutorialHeroGap=float.PositiveInfinity;
            foreach(var capsule in redTutorialBodySpace)RedesignTutorialHeroGap=Mathf.Min(RedesignTutorialHeroGap,tree.CapsuleGap(capsule.a-offset,capsule.b-offset,capsule.radius)*scale);
        }
        shark.position+=local?matrix.MultiplyVector(offset):offset;
    }
    void RedStoreTutorialStance(Vector3 feet)
    {
        redStanceLocal=redCombatBoard.transform.InverseTransformPoint(feet);redHasStance=true;
    }
    void RedPlanTutorialDodge()
    {
        long started=System.Diagnostics.Stopwatch.GetTimestamp();
        Vector3 feet=RedBoardPoint(redCombatBoard);RedTutorialBody();RedWaterCollectShapes();
        redWaterShapes.RemoveAll(s=>s.mesh==redCombatBoard.Collision);
        redDodgeStartLocal=redCombatBoard.transform.InverseTransformPoint(feet);
        redDodgeAttackLocal=redDodgeStartLocal;redDodgeEndLocal=redDodgeStartLocal;
        RedesignDodgeCandidates=0;RedesignDodgeTravel=0;
        var candidates=new List<(Vector3 point,float score)>();
        // Destinations and the complete travel segment come from the current
        // real deck and surrounding native compound timber, not a fixed side.
        for(int ring=0;ring<3;ring++)for(int bearing=0;bearing<16;bearing++)
        {
            Vector3 direction=Quaternion.AngleAxis(bearing*22.5f,Vector3.up)*redForward;
            Vector3 candidate=redCombatBoard.BoardingPoint(feet+direction*(2.5f+ring*1.25f))+Vector3.up*.08f;
            Vector3 delta=candidate-feet;float distance=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;
            if(distance<1.5f)continue;
            float score=Mathf.Abs(Vector3.Dot(delta,redRight))*2+distance*.3f;
            bool duplicate=false;foreach(var previous in candidates)if((previous.point-candidate).sqrMagnitude<.01f){duplicate=true;break;}
            if(!duplicate)candidates.Add((candidate,score));
        }
        candidates.Sort((a,b)=>b.score.CompareTo(a.score));
        // Check the most useful open direction first. Stop at the first fully
        // validated path rather than solving every other usable destination.
        foreach(var option in candidates)
        {
            RedesignDodgeCandidates++;Vector3 candidate=option.point,delta=candidate-feet;
            float distance=Vector3.ProjectOnPlane(delta,Vector3.up).magnitude;bool clear=true;
            for(int sample=1;sample<=12&&clear;sample++)
            {
                float u=sample/12f;Vector3 point=Vector3.Lerp(feet,candidate,u);
                // Cover the rolling silhouette and the gap between probes.
                // A legal standing position near the cut edge need not fit
                // the full rolling silhouette before the roll has moved
                // inboard. Grow support coverage over the entry quarter.
                float radius=Mathf.Lerp(.35f,.8f+distance/24,REase(u,0,.25f));
                for(int edge=0;edge<8;edge++)
                    if(!redCombatBoard.ContainsFootprint(point+Quaternion.AngleAxis(edge*45,Vector3.up)*Vector3.forward*radius)){clear=false;break;}
                if(clear&&RedWaterBodyCost(redWaterCapsules,delta*u,u*.8f,.12f,out _)>0)clear=false;
            }
            if(!clear)continue;
            redDodgeEndLocal=redCombatBoard.transform.InverseTransformPoint(candidate);redDodgeDirection=delta.normalized;RedesignDodgeTravel=distance;
            break;
        }
        if(RedesignDodgeTravel<.1f)redDodgeDirection=-redRight;
        RedEvent("Adaptive dodge; candidates="+RedesignDodgeCandidates+"; travel="+RedesignDodgeTravel.ToString("F2")+"; local target="+redDodgeEndLocal.ToString("F2"));
        RedesignDodgePlanMs=RedRiggingElapsed(started);
    }
    void RedTutorialDodge(float seconds)
    {
        Vector3 start=redCombatBoard.transform.TransformPoint(redDodgeStartLocal),end=redCombatBoard.transform.TransformPoint(redDodgeEndLocal);
        float u=REase(seconds,.05f,.95f);
        Vector3 point=Vector3.Lerp(start,end,u);
        if(seconds<1.05f){RedPose("Rig|Roll",Mathf.Min(seconds,.95f));RedFeet(point,Quaternion.LookRotation(redDodgeDirection));RedSolidSupport();}
        else{RedStoreTutorialStance(end);RedOnBoard("CH1_SahurGuard_v050",seconds-1.05f);}
        Vector3 attack=redCombatBoard.transform.TransformPoint(redDodgeAttackLocal)+Vector3.up*(redHeroHeight*.45f);
        // The bite remains aimed at the position committed before the roll.
        float run=seconds<.9f?Mathf.Lerp(10,-1,REase(seconds,0,.9f)):Mathf.Lerp(-1,18,REase(seconds,.9f,2.2f));
        RedShark(seconds<1.1f?"Bite_Lunge":"Swim",seconds<1.1f?seconds:seconds-1.1f,attack+redForward*run,-redForward);
        RedKeepTutorialSharkClear(.4f);
        if(seconds>=2.3f&&RedesignTutorialHeroGap>=.39f)
        {RedOnBoard("CH1_SahurGuard_v050",0);RedKeepTutorialSharkClear(.4f);RedGate(Lesson.ChargeHold);}
    }
    Vector3 RedTutorialNose(Vector3 feet,float distance,float height=2.25f)
    {
        Vector3 nose=feet+redForward*distance;nose.y=RedWater(nose)+height;return nose;
    }
    void RedFitTutorialFrontToWeapon(float weight)
    {
        var skin=RedContactSkin();if(skin==null)return;
        float front=float.NegativeInfinity;var matrix=skin.transform.localToWorldMatrix;
        foreach(int i in redHeadVertices)front=Mathf.Max(front,Vector3.Dot(matrix.MultiplyPoint3x4(redPosedVertices[i]),shark.forward));
        Vector3 target=RedWeaponPoint,surface=sharkAnimator.NoseWorldPoint;float best=float.PositiveInfinity;
        foreach(int i in redHeadVertices)
        {
            Vector3 p=matrix.MultiplyPoint3x4(redPosedVertices[i]);
            if(Vector3.Dot(p,shark.forward)<front-.6f)continue;
            float distance=(p-target).sqrMagnitude;if(distance<best){best=distance;surface=p;}
        }
        Story1CapsuleContact.Segment(redWeapon,out _,out _,out float radius);
        Vector3 approach=(target-surface).normalized;
        shark.position+=(target-approach*radius-surface)*weight;
    }
}
