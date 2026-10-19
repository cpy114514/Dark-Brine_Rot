using System.Collections.Generic;
using UnityEngine;

public sealed partial class Story1WreckBattle
{
    Mesh redContactMesh;
    int[] redHeadVertices;
    int[] redBellyVertices;
    readonly List<Vector3> redPosedVertices=new List<Vector3>();
    Vector3 redProjectileA,redProjectileB;
    float redProjectileRadius;
    bool redOverheadHit,redThrowHit,redTailHit;
    float redTailHitTime=-1;
    Vector3 redTailExitRoot;
    Quaternion redTailExitRotation;
    Vector3 redCrushContact,redCrushLaunch;
    GameObject redShadow;
    Mesh redShadowMesh;
    Material redShadowMaterial;
    float redBreachBellyY;
    Vector3 redCrushImpactNose;
    Quaternion redCrushImpactRotation;
    Transform[] redCrushBones;
    Vector3[] redCrushBlendPositions;
    Quaternion[] redCrushBlendRotations;
    bool redCrushEntryCaptured,redCrushRecoveryCaptured;
    Transform[] redCrushSecondaryBones;
    Quaternion[] redCrushStrokeRotations;
    void RedReadCrushStroke()
    {
        if(redCrushSecondaryBones==null)
        {
            var selected=new List<Transform>();
            foreach(var bone in ((SkinnedMeshRenderer)sharkAnimator.VisualRenderer).bones)
                if(bone.name.StartsWith("Tail_")||bone.name=="Fin_Left"||bone.name=="Fin_Right")selected.Add(bone);
            redCrushSecondaryBones=selected.ToArray();redCrushStrokeRotations=new Quaternion[selected.Count];
        }
        for(int i=0;i<redCrushSecondaryBones.Length;i++)redCrushStrokeRotations[i]=redCrushSecondaryBones[i].localRotation;
    }
    void RedApplyCrushStroke(float seconds)
    {
        // v214 was authored over a single frozen Swim frame. Retain its spine
        // compression but let the original rig's tail stroke drive takeoff,
        // settle in the air and regain strength as it enters the water.
        float tail=.22f+.53f*(1-REase(seconds,.65f,1.65f))+.48f*REase(seconds,2.52f,3.6f);
        for(int i=0;i<redCrushSecondaryBones.Length;i++)
        {
            var bone=redCrushSecondaryBones[i];float weight=bone.name.StartsWith("Tail_")?tail:.2f;
            bone.localRotation=Quaternion.Slerp(bone.localRotation,redCrushStrokeRotations[i],weight);
        }
    }
    readonly Dictionary<Vector2Int,float> redShadowWater=new Dictionary<Vector2Int,float>();
    public float RedesignSignedContactGap { get; private set; } = -999;

    SkinnedMeshRenderer RedContactSkin()
    {
        var skin=sharkAnimator.VisualRenderer as SkinnedMeshRenderer;
        if(skin==null)return null;
        if(redContactMesh==null)
        {
            redContactMesh=new Mesh{name="Opening contact pose"};
            var indices=new List<int>();var belly=new List<int>();var weights=skin.sharedMesh.boneWeights;
            // Unity returns a copied array for bones. Read it once, not for
            // each weight of every vertex on the first cinematic frame.
            var bones=skin.bones;
            for(int i=0;i<weights.Length;i++)
            {
                var w=weights[i];float total=0;
                void Add(int bone,float amount){if(bone<bones.Length && (bones[bone].name=="Head"||bones[bone].name=="Jaw"))total+=amount;}
                Add(w.boneIndex0,w.weight0);Add(w.boneIndex1,w.weight1);Add(w.boneIndex2,w.weight2);Add(w.boneIndex3,w.weight3);
                if(total>.5f)indices.Add(i);
                float trunk=0;
                void Trunk(int bone,float amount){if(bone<bones.Length&&(bones[bone].name=="Body"||bones[bone].name=="Spine_Mid"))trunk+=amount;}
                Trunk(w.boneIndex0,w.weight0);Trunk(w.boneIndex1,w.weight1);Trunk(w.boneIndex2,w.weight2);Trunk(w.boneIndex3,w.weight3);
                if(trunk>.85f)belly.Add(i);
            }
            redHeadVertices=indices.ToArray();redBellyVertices=belly.ToArray();
        }
        // BakeMesh(true) compensates inherited scale in Unity 6.3; the
        // subsequent TransformPoint applies that scale exactly once.
        skin.BakeMesh(redContactMesh,true);redContactMesh.GetVertices(redPosedVertices);
        return skin;
    }
    Vector3 RedHeadSurface(Vector3 query)
    {
        var skin=RedContactSkin();if(skin==null)return sharkAnimator.NoseWorldPoint;
        float best=float.PositiveInfinity;Vector3 result=sharkAnimator.NoseWorldPoint;
        foreach(int i in redHeadVertices)
        {
            Vector3 point=skin.transform.TransformPoint(redPosedVertices[i]);float distance=(query-point).sqrMagnitude;
            if(distance<best){best=distance;result=point;}
        }
        return result;
    }
    Vector3 RedBellySurface()
    {
        var skin=RedContactSkin();if(skin==null)throw new System.InvalidOperationException("Body crush requires the posed shark mesh");
        float low=float.PositiveInfinity;Vector3 point=shark.position;
        foreach(int i in redBellyVertices)
        {
            Vector3 p=skin.transform.TransformPoint(redPosedVertices[i]);if(p.y<low){low=p.y;point=p;}
        }
        return point;
    }
    void RedCrushShark(float t)
    {
        if(t<3)
        {
            Vector3 centre=redCrushContact+redRight*Mathf.Lerp(115,80,REase(t,0,3));
            float launchPitch=Mathf.Lerp(0,24,REase(t,2.65f,3.4f))*Mathf.Deg2Rad;
            RedShark("Swim",t,centre,-redRight*Mathf.Cos(launchPitch)+Vector3.up*Mathf.Sin(launchPitch));
            var skin=RedContactSkin();float top=float.NegativeInfinity;
            foreach(var vertex in redPosedVertices)top=Mathf.Max(top,skin.transform.TransformPoint(vertex).y);
            Vector3 submerged=shark.position;submerged.y+=RedWater(centre)-2-top;shark.position=submerged;
            Vector3 belly=RedBellySurface();shark.position+=Vector3.ProjectOnPlane(centre-belly,Vector3.up);
            redBreachBellyY=RedBellySurface().y;
            RedWarningShadow(t);return;
        }
        if(redShadow!=null)redShadow.SetActive(false);
        if(!redCrushEntryCaptured){RedCaptureCrushPose();redCrushEntryCaptured=true;}
        // Gain height outside the rigging before crossing over the deck.
        // The earlier path climbed through the masts on the way up.
        Vector3 target=redCrushContact+redRight*(t<4.48f?Mathf.Lerp(80,8,REase(t,3,4.48f)):Mathf.Lerp(8,0,REase(t,4.48f,6)));
        // A fixed additive arc loses its apex when the original large shark
        // starts deep underwater. Author the visible crest independently of
        // the submerged starting height, then accelerate into the hull.
        float crest=redCrushContact.y+65;
        const float apex=4.48f;
        float rise=R01(t,3,apex),fall=R01(t,apex,6);
        target.y=t<apex?crest-(crest-redBreachBellyY)*(1-rise)*(1-rise):Mathf.Lerp(crest,redCrushContact.y,fall*fall);
        float pitch=t<3.5f?Mathf.Lerp(0,24,REase(t,2.65f,3.4f)):
            t<apex?Mathf.Lerp(24,0,REase(t,3.5f,apex)):Mathf.Lerp(0,-20,REase(t,apex,6));
        // The authored compression is at 2.52s. Previously the hull contact
        // sampled the recovered 3.6s pose, making the whole animal look rigid.
        float poseTime=t<4.35f?Mathf.Lerp(0,1.1f,R01(t,3,4.35f)):
            t<4.7f?Mathf.Lerp(1.1f,1.65f,R01(t,4.35f,4.7f)):Mathf.Lerp(1.65f,2.52f,R01(t,4.7f,6));
        RedShark("CH1_SharkLeapCrush_v214",poseTime,target,-redRight*Mathf.Cos(pitch*Mathf.Deg2Rad)+Vector3.up*Mathf.Sin(pitch*Mathf.Deg2Rad));
        RedBlendCrushPose(REase(t,3,3.22f));
        // Fit the complete animal to its animated underside. The source mesh,
        // not a detached marker, supplies the body-to-hull contact.
        shark.position+=target-RedBellySurface();
        RedesignContactGap=Vector3.Distance(RedBellySurface(),redCrushContact);
        redCrushImpactNose=sharkAnimator.NoseWorldPoint;redCrushImpactRotation=shark.rotation;
    }
    void RedCaptureCrushPose()
    {
        if(redCrushBones==null)
        {
            redCrushBones=((SkinnedMeshRenderer)sharkAnimator.VisualRenderer).bones;
            redCrushBlendPositions=new Vector3[redCrushBones.Length];redCrushBlendRotations=new Quaternion[redCrushBones.Length];
        }
        for(int i=0;i<redCrushBones.Length;i++)
        {redCrushBlendPositions[i]=redCrushBones[i].localPosition;redCrushBlendRotations[i]=redCrushBones[i].localRotation;}
    }
    void RedBlendCrushPose(float weight)
    {
        if(weight>=1)return;
        for(int i=0;i<redCrushBones.Length;i++)
        {
            var bone=redCrushBones[i];bone.localPosition=Vector3.Lerp(redCrushBlendPositions[i],bone.localPosition,weight);
            bone.localRotation=Quaternion.Slerp(redCrushBlendRotations[i],bone.localRotation,weight);
        }
    }
    void RedCrushRecovery(float t)
    {
        float age=Mathf.Max(0,t-6);
        Vector3 nose=redCrushImpactNose-redRight*(age*12);
        nose.y-=18*(1-Mathf.Exp(-age*5));
        nose.y=Mathf.Lerp(nose.y,RedWater(nose)-18,REase(age,.7f,2.3f));
        Quaternion rotation=Quaternion.Slerp(redCrushImpactRotation,Quaternion.LookRotation(-redRight),REase(age,.35f,2.3f));
        if(age<.9f)RedShark("CH1_SharkLeapCrush_v214",Mathf.Lerp(2.52f,3.599f,age/.9f),nose,rotation*Vector3.forward);
        else
        {
            if(!redCrushRecoveryCaptured){RedCaptureCrushPose();redCrushRecoveryCaptured=true;}
            RedShark("Swim",1+age-.9f,nose,rotation*Vector3.forward);
            RedBlendCrushPose(REase(age,.9f,1.25f));
            shark.position+=nose-sharkAnimator.NoseWorldPoint;
        }
    }
    void RedWarningShadow(float t)
    {
        var skin=RedContactSkin();if(skin==null)return;
        if(redShadow==null)
        {
            redShadow=new GameObject("Underwater shark silhouette",typeof(MeshFilter),typeof(MeshRenderer));
            redShadowMesh=new Mesh{name="Projected original shark silhouette"};redShadowMesh.indexFormat=skin.sharedMesh.indexFormat;
            redShadowMesh.vertices=new Vector3[redPosedVertices.Count];redShadowMesh.triangles=skin.sharedMesh.triangles;
            redShadow.GetComponent<MeshFilter>().sharedMesh=redShadowMesh;
            redShadowMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            redShadowMaterial.SetFloat("_Surface",1);redShadowMaterial.SetFloat("_SrcBlend",(float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            redShadowMaterial.SetFloat("_DstBlend",(float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);redShadowMaterial.SetFloat("_ZWrite",0);
            redShadowMaterial.SetFloat("_Cull",0);redShadowMaterial.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");redShadowMaterial.renderQueue=2990;
            var renderer=redShadow.GetComponent<MeshRenderer>();renderer.sharedMaterial=redShadowMaterial;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;renderer.receiveShadows=false;
        }
        // The warning follows the displaced ocean. A single flat water height
        // buried the silhouette in troughs and made it look like a card over crests.
        redShadowWater.Clear();var water=OceanSurfaceSampler.Capture(ocean,sequence.storyCamera,Time.time);
        float Height(int x,int z)
        {
            var key=new Vector2Int(x,z);
            if(!redShadowWater.TryGetValue(key,out float h))redShadowWater[key]=h=ocean!=null?water.Height(new Vector3(x,0,z)):center.y;
            return h;
        }
        for(int i=0;i<redPosedVertices.Count;i++)
        {
            var p=skin.transform.TransformPoint(redPosedVertices[i]);int x=Mathf.FloorToInt(p.x),z=Mathf.FloorToInt(p.z);
            p.y=Mathf.Lerp(Mathf.Lerp(Height(x,z),Height(x+1,z),p.x-x),Mathf.Lerp(Height(x,z+1),Height(x+1,z+1),p.x-x),p.z-z)+.08f;
            redPosedVertices[i]=p;
        }
        redShadowMesh.SetVertices(redPosedVertices);redShadowMesh.RecalculateBounds();
        redShadowMaterial.SetColor("_BaseColor",new Color(.002f,.008f,.012f,.62f*REase(t,0,.8f)));
    }
    static Vector3 RedClosestSegment(Vector3 point,Vector3 a,Vector3 b)
        =>a+(b-a)*Mathf.Clamp01(Vector3.Dot(point-a,b-a)/Mathf.Max(.000001f,(b-a).sqrMagnitude));
    float RedHeldHeadGap(out Vector3 contact)
    {
        Story1CapsuleContact.Segment(redWeapon,out var a,out var b,out float radius);
        contact=RedHeadSurface(RedWeaponPoint);
        RedesignSignedContactGap=Vector3.Distance(contact,RedClosestSegment(contact,a,b))-radius;
        return Mathf.Max(0,RedesignSignedContactGap);
    }
    void RedFitHeadToWeapon(float weight)
    {
        Vector3 target=RedWeaponPoint;
        Vector3 surface=RedHeadSurface(target);
        Story1CapsuleContact.Segment(redWeapon,out _,out _,out float radius);
        Vector3 approach=(target-surface).normalized;
        shark.position+=(target-approach*radius-surface)*weight;
    }
    bool RedRegisterHeadHit(string label,Vector3 impulse,float strength)
    {
        RedesignContactGap=RedHeldHeadGap(out var contact);
        if(RedesignContactGap>.18f)return false;
        Effects.Impact(contact,impulse,strength);
        RedEvent(label+"; visible head/shaft gap="+RedesignContactGap.ToString("F3"));return true;
    }
    float RedThrownHeadGap(out Vector3 contact)
    {
        var a=redProjectile.TransformPoint(redProjectileA);var b=redProjectile.TransformPoint(redProjectileB);
        contact=RedHeadSurface(RedClosestSegment(redThrowTarget,a,b));
        RedesignSignedContactGap=Vector3.Distance(contact,RedClosestSegment(contact,a,b))-redProjectileRadius;
        return Mathf.Max(0,RedesignSignedContactGap);
    }
    float RedTailBodyGap(out Vector3 contact)
    {
        Vector3 tail=sharkAnimator.TailWorldPoint;
        var hips=redAnimator.GetBoneTransform(HumanBodyBones.Hips).position;
        contact=RedClosestSegment(tail,hips,RedChest);
        RedesignSignedContactGap=Vector3.Distance(tail,contact)-.55f;
        return Mathf.Max(0,RedesignSignedContactGap);
    }
}
