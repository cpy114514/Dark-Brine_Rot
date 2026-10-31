using UnityEngine;
using System.Collections.Generic;
/// <summary>Bounded two-bone correction of an authored stance onto its actual deck.</summary>
public static class SahurSupportPose
{
    sealed class FootData
    {
        public SahurSoleSkinCache.Vertex[] vertices;public Matrix4x4[] bind,matrices;public Vector3[] world;public bool compatible;
    }
    static SahurSoleSkinCache[] preparedSkins;
    static readonly Dictionary<(Mesh,int,int,int,int),FootData> footData=new Dictionary<(Mesh,int,int,int,int),FootData>();
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ClearFootData(){footData.Clear();preparedSkins=null;}
    static FootData Data(SkinnedMeshRenderer skin,Transform[] bones,Transform left,Transform right,Transform leftToe,Transform rightToe)
    {
        var mesh=skin.sharedMesh;
        int a=System.Array.IndexOf(bones,left),b=System.Array.IndexOf(bones,right),c=System.Array.IndexOf(bones,leftToe),d=System.Array.IndexOf(bones,rightToe);
        var key=(mesh,a,b,c,d);if(footData.TryGetValue(key,out var cached))return cached;
        var result=new FootData();footData.Add(key,result);
        if(!mesh.isReadable)
        {
            if(preparedSkins==null)preparedSkins=Resources.LoadAll<SahurSoleSkinCache>("SahurSupport");
            foreach(var data in preparedSkins)
            {
                if(data.source!=mesh || data.vertexCount!=mesh.vertexCount || data.leftFoot!=a || data.rightFoot!=b || data.leftToe!=c || data.rightToe!=d)continue;
                result.vertices=data.vertices;result.bind=data.bind;
                result.world=new Vector3[data.vertices.Length];result.matrices=new Matrix4x4[bones.Length];
                result.compatible=data.vertices.Length>0 && data.bind.Length==bones.Length;return result;
            }
            return result;
        }
        if(mesh.blendShapeCount!=0 || a<0 && b<0)return result;
        // The legacy bake uses the original four-weight skin. Keep unusual
        // blend-shape/extra-influence meshes on that exact legacy path.
        var influences=mesh.GetBonesPerVertex();bool supported=true;
        try{for(int i=0;i<influences.Length;i++)if(influences[i]>4){supported=false;break;}}
        finally{influences.Dispose();}
        if(!supported)return result;
        var points=mesh.vertices;var weights=mesh.boneWeights;var selected=new List<SahurSoleSkinCache.Vertex>();
        float Amount(BoneWeight w,int foot,int toe)
        {
            bool Match(int bone)=>foot>=0 && bone==foot || toe>=0 && bone==toe;
            return (Match(w.boneIndex0)?w.weight0:0)+(Match(w.boneIndex1)?w.weight1:0)+(Match(w.boneIndex2)?w.weight2:0)+(Match(w.boneIndex3)?w.weight3:0);
        }
        for(int i=0;i<points.Length;i++)
        {
            byte sides=0;if(Amount(weights[i],a,c)>=.25f)sides|=1;if(Amount(weights[i],b,d)>=.25f)sides|=2;
            if(sides!=0)selected.Add(new SahurSoleSkinCache.Vertex{point=points[i],weights=weights[i],sides=sides});
        }
        result.vertices=selected.ToArray();result.world=new Vector3[result.vertices.Length];result.bind=mesh.bindposes;
        result.matrices=new Matrix4x4[bones.Length];result.compatible=result.vertices.Length>0 && result.bind.Length==bones.Length;
        return result;
    }
    public static void Prepare(Animator animator)
    {
        if(animator==null || animator.avatar==null || !animator.isHuman)return;
        var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        var leftToe=animator.GetBoneTransform(HumanBodyBones.LeftToes);var rightToe=animator.GetBoneTransform(HumanBodyBones.RightToes);
        foreach(var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>())Data(skin,skin.bones,left,right,leftToe,rightToe);
    }
    public static void MeasureSoles(Animator animator,ref Vector3 leftSole,ref Vector3 rightSole)
    {
        using var measurement=new Unity.Profiling.ProfilerMarker("Sahur.MeasureSoles").Auto();
        if(!MeasureCachedSoles(animator,ref leftSole,ref rightSole,1))MeasureSolesLegacy(animator,ref leftSole,ref rightSole);
    }
    // Dense character meshes contain many near-identical sole vertices. The
    // traversal needs an estimate each frame, while exact measurement remains
    // available for other systems and offline validation.
    public static void MeasureSolesFast(Animator animator,ref Vector3 leftSole,ref Vector3 rightSole)
    {
        if(!MeasureCachedSoles(animator,ref leftSole,ref rightSole,16))MeasureSolesLegacy(animator,ref leftSole,ref rightSole);
    }
    static bool MeasureCachedSoles(Animator animator,ref Vector3 leftSole,ref Vector3 rightSole,int stride)
    {
        var left=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var right=animator.GetBoneTransform(HumanBodyBones.RightFoot);
        var leftToe=animator.GetBoneTransform(HumanBodyBones.LeftToes);var rightToe=animator.GetBoneTransform(HumanBodyBones.RightToes);
        var skins=animator.GetComponentsInChildren<SkinnedMeshRenderer>();
        float minLeft=float.PositiveInfinity,minRight=float.PositiveInfinity;Vector3 sumLeft=Vector3.zero,sumRight=Vector3.zero;int countLeft=0,countRight=0;
        foreach(var skin in skins)
        {
            var bones=skin.bones;var data=Data(skin,bones,left,right,leftToe,rightToe);
            // Imported unit scale can retain sub-micrometre FBX rounding.
            // Larger/non-uniform scale still uses the native bake fallback.
            if(!data.compatible || (skin.transform.localScale-Vector3.one).sqrMagnitude>1e-10f)return false;
            for(int i=0;i<bones.Length;i++){if(bones[i]==null)return false;data.matrices[i]=bones[i].localToWorldMatrix*data.bind[i];}
            for(int i=0;i<data.vertices.Length;i+=stride)
            {
                var v=data.vertices[i];var w=v.weights;Vector3 point=Vector3.zero;
                if(w.weight0>0)point+=data.matrices[w.boneIndex0].MultiplyPoint3x4(v.point)*w.weight0;
                if(w.weight1>0)point+=data.matrices[w.boneIndex1].MultiplyPoint3x4(v.point)*w.weight1;
                if(w.weight2>0)point+=data.matrices[w.boneIndex2].MultiplyPoint3x4(v.point)*w.weight2;
                if(w.weight3>0)point+=data.matrices[w.boneIndex3].MultiplyPoint3x4(v.point)*w.weight3;
                data.world[i]=point;if((v.sides&1)!=0)minLeft=Mathf.Min(minLeft,point.y);if((v.sides&2)!=0)minRight=Mathf.Min(minRight,point.y);
            }
            for(int i=0;i<data.vertices.Length;i+=stride)
            {
                var v=data.vertices[i];var point=data.world[i];
                if((v.sides&1)!=0 && point.y<=minLeft+.005f){sumLeft+=point;countLeft++;}
                if((v.sides&2)!=0 && point.y<=minRight+.005f){sumRight+=point;countRight++;}
            }
        }
        if(countLeft>0)leftSole=left.InverseTransformPoint(sumLeft/countLeft);
        if(countRight>0)rightSole=right.InverseTransformPoint(sumRight/countRight);
        return countLeft>0 || countRight>0;
    }
    // Retained verbatim for unsupported skins and native equivalence review.
    public static void MeasureSolesLegacy(Animator animator,ref Vector3 leftSole,ref Vector3 rightSole)
    {
        var feet=new[]{animator.GetBoneTransform(HumanBodyBones.LeftFoot),animator.GetBoneTransform(HumanBodyBones.RightFoot)};
        var toes=new[]{animator.GetBoneTransform(HumanBodyBones.LeftToes),animator.GetBoneTransform(HumanBodyBones.RightToes)};
        float[] minimum={float.PositiveInfinity,float.PositiveInfinity};var sum=new Vector3[2];var counts=new int[2];
        foreach(var skin in animator.GetComponentsInChildren<SkinnedMeshRenderer>())
        {
            int[] indices={System.Array.IndexOf(skin.bones,feet[0]),System.Array.IndexOf(skin.bones,feet[1])};
            int[] toeIndices={System.Array.IndexOf(skin.bones,toes[0]),System.Array.IndexOf(skin.bones,toes[1])};
            if(indices[0]<0 && indices[1]<0)continue;
            var baked=new Mesh();skin.BakeMesh(baked,true);var vertices=baked.vertices;var weights=skin.sharedMesh.boneWeights;
            bool InFoot(BoneWeight weight,int side)
            {
                bool Match(int bone)=>indices[side]>=0 && bone==indices[side] || toeIndices[side]>=0 && bone==toeIndices[side];
                float total=(Match(weight.boneIndex0) ? weight.weight0 : 0)+(Match(weight.boneIndex1) ? weight.weight1 : 0)+
                    (Match(weight.boneIndex2) ? weight.weight2 : 0)+(Match(weight.boneIndex3) ? weight.weight3 : 0);
                return total>=.25f;
            }
            for(int side=0;side<2;side++)
            {
                for(int i=0;i<vertices.Length;i++)if(InFoot(weights[i],side))minimum[side]=Mathf.Min(minimum[side],skin.transform.TransformPoint(vertices[i]).y);
                for(int i=0;i<vertices.Length;i++)if(InFoot(weights[i],side))
                {
                    Vector3 point=skin.transform.TransformPoint(vertices[i]);
                    if(point.y>minimum[side]+.005f)continue;
                    sum[side]+=point;counts[side]++;
                }
            }
            if(Application.isPlaying)Object.Destroy(baked);else Object.DestroyImmediate(baked);
        }
        // Sample a standing pose once. Swim/attack ankle height is not a
        // measurement of the original mesh's sole thickness.
        if(counts[0]>0)leftSole=feet[0].InverseTransformPoint(sum[0]/counts[0]);
        if(counts[1]>0)rightSole=feet[1].InverseTransformPoint(sum[1]/counts[1]);
    }
    public static float Anchor(Animator animator,bool left,Vector3 sole,Vector3 target,float limit=.35f)
    {
        var hip=animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperLeg : HumanBodyBones.RightUpperLeg);
        var knee=animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerLeg : HumanBodyBones.RightLowerLeg);
        var foot=animator.GetBoneTransform(left ? HumanBodyBones.LeftFoot : HumanBodyBones.RightFoot);
        Vector3 delta=Vector3.ClampMagnitude(target-foot.TransformPoint(sole),limit);Quaternion rotation=foot.rotation;
        if(delta.sqrMagnitude<.000001f)return Vector3.Distance(foot.TransformPoint(sole),target);
        PlaceSole(hip,knee,foot,sole,foot.TransformPoint(sole)+delta,animator.transform.forward,rotation);
        return Vector3.Distance(foot.TransformPoint(sole),target);
    }
    public static void Hand(Animator animator,bool left,Vector3 grip,float weight)
    {
        var hip=animator.GetBoneTransform(left ? HumanBodyBones.LeftUpperArm : HumanBodyBones.RightUpperArm);
        var knee=animator.GetBoneTransform(left ? HumanBodyBones.LeftLowerArm : HumanBodyBones.RightLowerArm);
        var end=animator.GetBoneTransform(left ? HumanBodyBones.LeftHand : HumanBodyBones.RightHand);
        if(hip==null || knee==null || end==null)return;
        Vector3 target=Vector3.Lerp(end.position,grip,weight);
        Vector3 delta=target-end.position;if(delta.magnitude>.65f)target=end.position+delta.normalized*.65f;
        Solve(hip,knee,end,target,animator.transform.forward);
    }
    public static float Plant(Animator animator,Story1WreckPlank board,Vector3 leftSole,Vector3 rightSole,float limit=.35f)
    {
        float a=Foot(animator,board,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,leftSole,limit);
        float b=Foot(animator,board,HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,rightSole,limit);
        // Walking and a stagger may deliberately unload one leg. Report the supporting sole.
        return Mathf.Min(a,b);
    }
    public static float SupportingFoot(Animator animator,Story1WreckPlank board,Vector3 sole,float limit=.35f)
        =>Foot(animator,board,HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,sole,limit);
    static float Foot(Animator animator,Story1WreckPlank board,HumanBodyBones thigh,HumanBodyBones shin,HumanBodyBones foot,Vector3 sole,float limit)
    {
        Transform hip=animator.GetBoneTransform(thigh),knee=animator.GetBoneTransform(shin),ankle=animator.GetBoneTransform(foot);
        Vector3 soleAt=ankle.TransformPoint(sole),normal=board.transform.up;
        if(normal.y<0)normal=-normal;
        float reach=Mathf.Max(2,board.Dimensions.y+1);
        if(!board.Collision.Raycast(new Ray(soleAt+normal*reach,-normal),out var hit,reach*2))
        {
            // A authored footstep may straddle a jagged timber edge. Move the
            // supporting foot onto that actual nearby surface within the same
            // bounded IK reach, rather than treating the empty ray as a deck.
            Vector3 edge=board.Collision.ClosestPoint(soleAt);
            if(Vector3.Distance(edge,soleAt)>limit ||
                !board.Collision.Raycast(new Ray(edge+normal*reach,-normal),out hit,reach*2) ||
                Vector3.Dot(hit.normal,normal)<.5f || Vector3.Distance(hit.point,soleAt)>limit)return float.PositiveInfinity;
        }
        Vector3 surface=hit.point;Quaternion footRotation=ankle.rotation;
        Vector3 delta=surface-soleAt;if(delta.magnitude>limit)delta=delta.normalized*limit;
        if(delta.sqrMagnitude<.000001f)return Vector3.Distance(soleAt,surface);
        PlaceSole(hip,knee,ankle,sole,soleAt+delta,animator.transform.forward,footRotation);
        return Vector3.Distance(ankle.TransformPoint(sole),surface);
    }
    static void PlaceSole(Transform hip,Transform knee,Transform ankle,Vector3 sole,Vector3 target,Vector3 forward,Quaternion rotation)
    {
        // Retargeted hierarchies can contain scaled parent transforms. Restoring
        // the authored foot rotation changes its sole offset after the first
        // joint solve. Iterate towards the SAME bounded world target, never
        // extend the original correction limit with each iteration.
        for(int iteration=0;iteration<4;iteration++)
        {
            Vector3 correction=target-ankle.TransformPoint(sole);
            if(correction.sqrMagnitude<.000001f)break;
            Solve(hip,knee,ankle,ankle.position+correction,forward);ankle.rotation=rotation;
        }
    }
    static void Solve(Transform hip,Transform knee,Transform ankle,Vector3 target,Vector3 forward)
    {
        Vector3 h=hip.position;
        float upper=Vector3.Distance(h,knee.position),lower=Vector3.Distance(knee.position,ankle.position);
        float distance=Mathf.Clamp(Vector3.Distance(h,target),Mathf.Abs(upper-lower)+.001f,upper+lower-.001f);
        Vector3 axis=(target-h).normalized;
        Vector3 bend=Vector3.ProjectOnPlane(knee.position-h,axis).normalized;
        if(bend.sqrMagnitude<.1f)bend=Vector3.ProjectOnPlane(forward,axis).normalized;
        float along=(upper*upper+distance*distance-lower*lower)/(2*distance);
        Vector3 desiredKnee=h+axis*along+bend*Mathf.Sqrt(Mathf.Max(0,upper*upper-along*along));
        hip.rotation=Quaternion.FromToRotation(knee.position-h,desiredKnee-h)*hip.rotation;
        knee.rotation=Quaternion.FromToRotation(ankle.position-knee.position,target-knee.position)*knee.rotation;
    }
}
