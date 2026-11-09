using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace Mavis
{
    // Runs after attack retargeting and procedural poses. Death owns presentation
    // only; Health, AI, progression, loot and reclamation keep their existing roles.
    [DefaultExecutionOrder(19000),DisallowMultipleComponent]
    public sealed class EnemyDeathPresentation : MonoBehaviour
    {
        sealed class Skin
        {
            public Renderer renderer;
            public EnemyDeathSupportLibrary.Entry shape;
            public Transform[] bones;
            public Matrix4x4[] matrices;
        }
        struct BonePose {public Transform bone;public Quaternion start,target;public Vector3 position;}
        struct CapturedPose {public Transform bone;public Vector3 position,scale;public Quaternion rotation;}
        enum FallStyle { Shared, Dragon, Ronin }
        static EnemyDeathSupportLibrary library;
        static readonly Dictionary<string,EnemyDeathSupportLibrary.Entry> shapes=new Dictionary<string,EnemyDeathSupportLibrary.Entry>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetCache(){library=null;shapes.Clear();}
        readonly List<Skin> skins=new List<Skin>();
        readonly List<BonePose> relaxed=new List<BonePose>();
        readonly List<CapturedPose> capturedPose=new List<CapturedPose>();
        readonly List<(Collider collider,bool enabled)> collisionState=new List<(Collider,bool)>();
        readonly Dictionary<Vector2Int,float> heights=new Dictionary<Vector2Int,float>();
        readonly RaycastHit[] hits=new RaycastHit[48];
        Transform visual;
        Animator animator;
        Collider floor;
        Vector3 floorPoint,floorNormal,localPosition,localScale,startPosition,pivot,fallDirection,previousCorrection;
        Quaternion localRotation,startRotation,previousPlane=Quaternion.identity;
        float height,age,clipLength,animatorSpeed,impactWeight,flightHeight,flightSpeed;
        AnimatorCullingMode culling;
        bool fallback,prepared,held,animatorEnabled,settled;
        FallStyle fallStyle;
        CappuccinoMotionRig cupRig;
        float heldAt;
        public bool IsPlaying {get;private set;}
        public bool IsSettled => IsPlaying && settled;
        public bool UsesFallback=>fallback&&IsPlaying;
        public float GroundCorrection {get;private set;}
        public static bool OwnsFallback(GameObject owner)
        {var death=owner.GetComponent<EnemyDeathPresentation>();return death&&death.UsesFallback;}

        public static void WarmUp()
        {
            if(library)return;
            library=Resources.Load<EnemyDeathSupportLibrary>("EnemyDeath/SupportLibrary");
            if(library&&library.entries!=null)foreach(var entry in library.entries)shapes[entry.key]=entry;
        }
        public static EnemyDeathSupportLibrary.Entry Shape(Mesh mesh,int boneCount=0)
        {
            if(!mesh)return null;WarmUp();shapes.TryGetValue(EnemyDeathSupportLibrary.Key(mesh,boneCount),out var shape);return shape;
        }
        public static EnemyDeathPresentation Prepare(Component owner,Vector3 hitPoint)
        {
            if(!Application.isPlaying||owner is Story1SharkHealth||owner.GetComponent<BombardinoBoss>())return null;
            var death=owner.GetComponent<EnemyDeathPresentation>();if(!death)death=owner.gameObject.AddComponent<EnemyDeathPresentation>();
            if(death.IsPlaying)return death;
            death.Capture(hitPoint);return death;
        }
        void Capture(Vector3 hitPoint)
        {
            WarmUp();skins.Clear();relaxed.Clear();capturedPose.Clear();heights.Clear();prepared=false;held=false;settled=false;age=0;impactWeight=0;floor=null;previousCorrection=Vector3.zero;previousPlane=Quaternion.identity;
            var cup=GetComponent<CappuccinoEnemy>();var marshmallow=GetComponent<MarshmallowEnemyVisual>();
            var pig=GetComponent<PigRoninBossVisual>();
            var dragon=GetComponent<NailongAI>();
            fallStyle = pig ? FallStyle.Ronin : dragon ? FallStyle.Dragon : FallStyle.Shared;
            cupRig = cup ? cup.motionRig : null;
            animator=cup?cup.animator:GetComponentInChildren<Animator>();
            visual=cup&&cup.motionRig?cup.motionRig.rigRoot:marshmallow?marshmallow.visual:animator?animator.transform:null;
            if(!visual){var renderer=GetComponentInChildren<Renderer>();visual=renderer?renderer.transform:transform;}
            if(!visual)return;
            clipLength=0;
            if(animator&&animator.runtimeAnimatorController)
                foreach(var clip in animator.runtimeAnimatorController.animationClips)
                    if(clip&&!clip.isLooping&&clip.name.ToLowerInvariant().Contains("death"))clipLength=Mathf.Max(clipLength,clip.length);
            // The bulky bird/dragon rigs fold their coats and heads into the torso
            // under the shared human death clip. Collapse their actual last pose
            // with limited limb relaxation instead of retargeting that clip.
            fallback=marshmallow||pig||GetComponent<ChickenBroAI>()||dragon||clipLength<=.05f;
            if(animator){animatorSpeed=animator.speed;culling=animator.cullingMode;animatorEnabled=animator.enabled;}
            localPosition=visual.localPosition;localRotation=visual.localRotation;localScale=visual.localScale;
            collisionState.Clear();foreach(var collider in GetComponentsInChildren<Collider>(true))collisionState.Add((collider,collider.enabled));
            startPosition=visual.position;startRotation=visual.rotation;
            // The cup skin is bound to the retargeted rig but lives beside it.
            var renderers = cup && cup.bodyRenderers != null && cup.bodyRenderers.Length > 0
                ? cup.bodyRenderers : visual.GetComponentsInChildren<Renderer>(false);
            Bounds bounds=new Bounds(transform.position,Vector3.one);bool found=false;
            foreach(var renderer in renderers)
            {
                if(!renderer||!renderer.enabled||!renderer.gameObject.activeInHierarchy||Weapon(renderer.transform))continue;
                Mesh mesh=null;Transform[] bones=null;
                if(renderer is SkinnedMeshRenderer skinned){mesh=skinned.sharedMesh;bones=skinned.bones;}
                else {var filter=renderer.GetComponent<MeshFilter>();if(filter)mesh=filter.sharedMesh;}
                if(!mesh)continue;
                if(!found){bounds=renderer.bounds;found=true;}else bounds.Encapsulate(renderer.bounds);
                if(shapes.TryGetValue(EnemyDeathSupportLibrary.Key(mesh,bones==null?0:bones.Length),out var shape))
                    skins.Add(new Skin{renderer=renderer,shape=shape,bones=bones,matrices=bones==null?null:new Matrix4x4[bones.Length]});
            }
            if(!found)return;
            height=Mathf.Max(.4f,bounds.size.y);pivot=bounds.center;
            if (fallStyle != FallStyle.Shared) pivot.y = bounds.min.y + height * .23f;
            fallDirection=Vector3.ProjectOnPlane(bounds.center-hitPoint,Vector3.up);
            if(fallDirection.sqrMagnitude<.01f)fallDirection=-transform.forward;
            fallDirection=(fallDirection.normalized+transform.right*.24f).normalized;
            if(marshmallow)fallDirection=(transform.right-transform.forward*.22f).normalized;
            int count=Physics.RaycastNonAlloc(new Ray(new Vector3(bounds.center.x,bounds.max.y+2,bounds.center.z),Vector3.down),hits,Mathf.Max(30,height*4),~0,QueryTriggerInteraction.Ignore);
            float nearest=float.PositiveInfinity;
            for(int i=0;i<count;i++)
            {
                var hit=hits[i];if(!hit.collider||hit.transform.IsChildOf(transform)||hit.collider.GetComponentInParent<IDamageable>()!=null||hit.normal.y<.35f||hit.distance>=nearest)continue;
                nearest=hit.distance;floor=hit.collider;floorPoint=hit.point;floorNormal=hit.normal;
            }
            if(!floor){floorPoint=new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);floorNormal=Vector3.up;}
            var motor=GetComponent<CharacterController>();var bird=GetComponent<ChickenBroAI>();
            flightHeight=motor?Mathf.Max(0,motor.bounds.min.y-floorPoint.y-.05f):0;
            flightSpeed=flightHeight>.12f&&bird?bird.DeathVerticalSpeed:0;
            if(fallback)
            {
                foreach(var bone in visual.GetComponentsInChildren<Transform>())if(bone!=visual)
                    capturedPose.Add(new CapturedPose{bone=bone,position=bone.localPosition,rotation=bone.localRotation,scale=bone.localScale});
                foreach(var bone in visual.GetComponentsInChildren<Transform>())
                {
                    string name=bone.name.ToLowerInvariant();float angle=0;
                    if(name.Contains("forearm")||name.Contains("lowerarm"))angle=22;
                    else if(name.Contains("upperarm")||name=="leftarm"||name=="rightarm")angle=-24;
                    else if(name.Contains("lowerleg")||name=="leftleg"||name=="rightleg")angle=26;
                    else if(name=="chest"||name=="spine1")angle=9;
                    else if(fallStyle!=FallStyle.Shared && (name=="head"||name=="neck"))
                        angle=fallStyle==FallStyle.Dragon?14:8;
                    else if(fallStyle!=FallStyle.Shared && (name.Contains("upperleg")||name.Contains("upleg")))
                        angle=fallStyle==FallStyle.Dragon?-12:-20;
                    if(angle==0||!bone.parent)continue;
                    var axis=bone.parent.InverseTransformDirection(transform.right).normalized;
                    relaxed.Add(new BonePose{bone=bone,start=bone.localRotation,position=bone.localPosition,target=Quaternion.AngleAxis(angle,axis)*bone.localRotation});
                }
                // Let raised weapon/guard arms lose their support. Keeping the
                // last guard pose made a fallen bird look as if it was still fighting.
                RelaxArm(true);RelaxArm(false);
                RelaxFeetAndWings();
                if(marshmallow)
                {
                    RelaxPart(marshmallow.leftArm,Quaternion.Euler(15,0,70));
                    RelaxPart(marshmallow.rightArm,Quaternion.Euler(-5,0,-70));
                    RelaxPart(marshmallow.leftLeg,Quaternion.Euler(15,0,-10));
                    RelaxPart(marshmallow.rightLeg,Quaternion.Euler(-5,0,12));
                }
            }
            prepared=true;
        }
        void RelaxPart(Transform bone,Quaternion target)
        {
            if(!bone)return;relaxed.RemoveAll(p=>p.bone==bone);
            relaxed.Add(new BonePose{bone=bone,start=bone.localRotation,target=target,position=bone.localPosition});
        }
        void RelaxArm(bool left)
        {
            if(!animator)return;
            Transform upper=null,lower=null,hand=null;
            if(animator.isHuman)
            {
                upper=animator.GetBoneTransform(left?HumanBodyBones.LeftUpperArm:HumanBodyBones.RightUpperArm);
                lower=animator.GetBoneTransform(left?HumanBodyBones.LeftLowerArm:HumanBodyBones.RightLowerArm);
                hand=animator.GetBoneTransform(left?HumanBodyBones.LeftHand:HumanBodyBones.RightHand);
            }
            else
            {
                string prefix=left?"Left":"Right";
                foreach(var bone in visual.GetComponentsInChildren<Transform>())
                {if(bone.name==prefix+"UpperArm")upper=bone;else if(bone.name==prefix+"LowerArm")lower=bone;else if(bone.name==prefix+"Hand")hand=bone;}
            }
            if(!upper||!lower||!hand)return;
            var a=upper.localRotation;var b=lower.localRotation;float side=left?-1:1;
            var down=(-Vector3.up+transform.right*(side*.15f)+transform.forward*.10f).normalized;
            upper.rotation=Quaternion.FromToRotation(lower.position-upper.position,down)*upper.rotation;
            var upperTarget=upper.localRotation;
            down=(-Vector3.up*.90f+transform.forward*.30f+transform.right*(side*.12f)).normalized;
            lower.rotation=Quaternion.FromToRotation(hand.position-lower.position,down)*lower.rotation;
            var lowerTarget=lower.localRotation;upper.localRotation=a;lower.localRotation=b;
            RelaxPart(upper,upperTarget);RelaxPart(lower,lowerTarget);
        }
        void RelaxFeetAndWings()
        {
            Transform body=null;var bones=visual.GetComponentsInChildren<Transform>();
            foreach(var bone in bones)if(bone.name=="Body")body=bone;
            foreach(var bone in bones)
            {
                bool foot=bone.name=="FootL"||bone.name=="FootR"||bone.name=="LeftFoot"||bone.name=="RightFoot";
                bool wing=bone.name=="WingL"||bone.name=="WingR";
                if((foot||wing)&&bone.parent)
                {
                    Vector3 direction=Vector3.zero;
                    if(foot&&bone.childCount>0)direction=bone.GetChild(0).position-bone.position;
                    else if(wing&&body)direction=bone.position-body.position;
                    if(direction.sqrMagnitude>.00001f)
                    {
                        var start=bone.localRotation;
                        var desired=(-Vector3.up+transform.forward*.15f).normalized;
                        var delta=Quaternion.FromToRotation(direction,desired);
                        bone.rotation=Quaternion.Slerp(Quaternion.identity,delta,wing?.72f:.70f)*bone.rotation;
                        var target=bone.localRotation;bone.localRotation=start;RelaxPart(bone,target);
                    }
                }
                if(bone.name=="ShinL"||bone.name=="ShinR"||bone.name=="Neck2"||bone.name=="Neck3")
                {
                    float degrees=bone.name.StartsWith("Shin")?24:7;
                    var axis=bone.parent.InverseTransformDirection(transform.right);
                    RelaxPart(bone,Quaternion.AngleAxis(degrees,axis)*bone.localRotation);
                }
            }
        }
        public void ApplyImpulse(Vector3 direction,bool heavy)
        {
            var flat=Vector3.ProjectOnPlane(direction,Vector3.up);
            if(flat.sqrMagnitude>.01f)fallDirection=flat.normalized;
            impactWeight=heavy?1:0;
            if(heavy)flightSpeed=Mathf.Max(flightSpeed,3f);
        }
        bool Weapon(Transform part)
        {
            for(var t=part;t&&t!=visual;t=t.parent)
            {
                string name=t.name.ToLowerInvariant();
                if(name.Contains("katana")||name.Contains("sword")||name.Contains("blade")||name.Contains("warning")||name.Contains("trail"))return true;
            }
            return false;
        }
        public void Begin()
        {
            if(IsPlaying||!prepared)return;IsPlaying=true;enabled=true;
            foreach(var collider in GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var agent in GetComponentsInChildren<NavMeshAgent>())if(agent.enabled)agent.enabled=false;
            if(fallback)
            {
                if(animator)animator.enabled=false;
                visual.SetPositionAndRotation(startPosition,startRotation);visual.localScale=localScale;
                foreach(var pose in capturedPose)if(pose.bone){pose.bone.localPosition=pose.position;pose.bone.localRotation=pose.rotation;pose.bone.localScale=pose.scale;}
                var pig=GetComponent<PigRoninBossVisual>();if(pig)pig.BeginDeathPose();
            }
            else if(animator)
            {
                animator.enabled=true;animator.speed=1;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
                foreach(var parameter in animator.parameters)if(parameter.type==AnimatorControllerParameterType.Trigger)animator.ResetTrigger(parameter.nameHash);
                int death = Animator.StringToHash(animator.GetLayerName(0) + ".Death");
                int die = Animator.StringToHash(animator.GetLayerName(0) + ".Die");
                if(animator.HasState(0,death))animator.CrossFadeInFixedTime(death,.16f,0,0);
                else if(animator.HasState(0,die))animator.CrossFadeInFixedTime(die,.16f,0,0);
            }
        }
        float VertexFloor(Vector3 p)
        {
            int x=Mathf.FloorToInt(p.x*2),z=Mathf.FloorToInt(p.z*2);
            float u=p.x*2-x,v=p.z*2-z;
            return Mathf.Lerp(Mathf.Lerp(GridHeight(x,z),GridHeight(x+1,z),u),Mathf.Lerp(GridHeight(x,z+1),GridHeight(x+1,z+1),u),v);
        }
        float GridHeight(int x,int z)
        {
            var key=new Vector2Int(x,z);if(heights.TryGetValue(key,out float y))return y;
            float px=x*.5f,pz=z*.5f;
            if(floor&&floor.Raycast(new Ray(new Vector3(px,floor.bounds.max.y+2,pz),Vector3.down),out var hit,floor.bounds.size.y+20))y=hit.point.y;
            else y=floorPoint.y-(floorNormal.x*(px-floorPoint.x)+floorNormal.z*(pz-floorPoint.z))/Mathf.Max(.35f,floorNormal.y);
            heights[key]=y;return y;
        }
        void PoseBossFall()
        {
            bool dragon = fallStyle == FallStyle.Dragon;
            float buckleEnd = dragon ? .43f : .34f;
            float contact = dragon ? 1.23f : .96f;
            float settleEnd = contact + (dragon ? .40f : .32f);
            float buckle = Mathf.SmoothStep(0, 1, age / buckleEnd);
            float down = Mathf.InverseLerp(buckleEnd, contact, age);
            float settle = Mathf.InverseLerp(contact, settleEnd, age);
            float recoil = Mathf.Sin(Mathf.PI * Mathf.InverseLerp(0, .22f, age));
            // Weight first drops through the knees. The body accelerates into
            // contact and rolls a few degrees before resting on the slope.
            float pitch = 10 * buckle + 76 * down * down
                + (dragon ? 4 : 7) * Mathf.Sin(Mathf.PI * settle) + 3 * settle - 6 * recoil;
            var plane = Quaternion.Slerp(Quaternion.identity,
                Quaternion.FromToRotation(Vector3.up, floorNormal), down * down);
            var rotation = plane * Quaternion.AngleAxis(pitch, Vector3.Cross(Vector3.up, fallDirection).normalized);
            float compression = buckle * (1 - down) * (dragon ? .09f : .13f);
            float follow = down * down * (3 - 2 * down);
            visual.SetPositionAndRotation(
                pivot + rotation * (startPosition - pivot)
                + fallDirection * (height * (.05f + .04f * impactWeight) * follow)
                - Vector3.up * (height * compression), rotation * startRotation);
            visual.localScale = localScale;
            foreach (var pose in relaxed)
            {
                if (!pose.bone) continue;
                float release = Mathf.SmoothStep(0, 1, Mathf.InverseLerp(
                    dragon ? .14f : .08f, contact * .78f, age));
                pose.bone.localRotation = Quaternion.Slerp(pose.start, pose.target, release);
                pose.bone.localPosition = pose.position;
            }
        }
        void LateUpdate()
        {
            if(!IsPlaying||settled||!visual||Time.timeScale<=0||Time.deltaTime<=0||PauseSettingsMenu.IsOpen)return;
            age+=Time.deltaTime;
            if(flightHeight>0||flightSpeed>0){flightSpeed-=18f*Time.deltaTime;flightHeight=Mathf.Max(0,flightHeight+flightSpeed*Time.deltaTime);}
            if (fallback && fallStyle != FallStyle.Shared) PoseBossFall();
            else if(fallback)
            {
                float relax=Mathf.SmoothStep(0,1,Mathf.Clamp01(age/.7f));
                foreach(var pose in relaxed)if(pose.bone){pose.bone.localRotation=Quaternion.Slerp(pose.start,pose.target,relax);pose.bone.localPosition=pose.position;}
                float t=Mathf.Clamp01(age/1.1f);
                float fall=t<.20f?Mathf.Lerp(0,8,t/.20f):t<.80f?Mathf.Lerp(8,88,Mathf.Pow((t-.20f)/.60f,1.6f)):88+Mathf.Sin((t-.80f)/.20f*Mathf.PI)*1.5f;
                var q=Quaternion.AngleAxis(fall,Vector3.Cross(Vector3.up,fallDirection).normalized);
                q=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,floorNormal),t*t)*q;
                visual.rotation=q*startRotation;
                visual.position=pivot+q*(startPosition-pivot)+fallDirection*(height*(.08f+impactWeight*.08f)*t)-Vector3.up*(height*.32f*t*t);
                visual.localScale=localScale;
            }
            else
            {
                // Capri's retargeter rewrites its rig position every frame. Other
                // imported Animator roots keep the correction from the previous frame.
                if(!cupRig){visual.position-=previousCorrection;visual.rotation=Quaternion.Inverse(previousPlane)*visual.rotation;}
                previousPlane=Quaternion.Slerp(Quaternion.identity,Quaternion.FromToRotation(Vector3.up,floorNormal),Mathf.SmoothStep(0,1,Mathf.Clamp01(age/Mathf.Max(.5f,clipLength*.8f))));
                visual.rotation=previousPlane*visual.rotation;
                if(animator&&!held&&age>clipLength*.65f&&!animator.IsInTransition(0)&&animator.GetCurrentAnimatorStateInfo(0).normalizedTime>=.985f)
                {animator.speed=0;held=true;heldAt=age;}
            }
            float lowest=float.PositiveInfinity;
            foreach(var skin in skins)
            {
                if(!skin.renderer||!skin.renderer.enabled)continue;
                if(skin.bones!=null)
                    for(int b=0;b<skin.matrices.Length;b++)skin.matrices[b]=skin.bones[b]?skin.bones[b].localToWorldMatrix*skin.shape.bindposes[b]:Matrix4x4.zero;
                foreach(var point in skin.shape.points)
                {
                    Vector3 p;
                    if(skin.bones==null)p=skin.renderer.transform.TransformPoint(point.vertex);
                    else
                    {
                        var w=point.weight;var m=skin.matrices;
                        p=m[w.boneIndex0].MultiplyPoint3x4(point.vertex)*w.weight0;
                        if(w.weight1>0)p+=m[w.boneIndex1].MultiplyPoint3x4(point.vertex)*w.weight1;
                        if(w.weight2>0)p+=m[w.boneIndex2].MultiplyPoint3x4(point.vertex)*w.weight2;
                        if(w.weight3>0)p+=m[w.boneIndex3].MultiplyPoint3x4(point.vertex)*w.weight3;
                    }
                    lowest=Mathf.Min(lowest,p.y-VertexFloor(p));
                }
            }
            if(float.IsPositiveInfinity(lowest))
                foreach(var renderer in visual.GetComponentsInChildren<Renderer>())if(renderer.enabled&&!Weapon(renderer.transform))lowest=Mathf.Min(lowest,renderer.bounds.min.y-VertexFloor(renderer.bounds.center));
            if(!float.IsInfinity(lowest))
            {
                // Reset and solve against the current posed body, never accumulate
                // corrections and never use standing foot targets for a prone body.
                previousCorrection=Vector3.up*(.025f+flightHeight-lowest);visual.position+=previousCorrection;GroundCorrection=previousCorrection.y;
            }
            float settleTime = fallStyle == FallStyle.Dragon ? 1.68f : 1.6f;
            if(fallback&&age>settleTime&&flightHeight<=0)settled=true;
            if(cupRig&&held&&age-heldAt>.28f)settled=true;
        }
        void OnDisable()
        {
            if(!IsPlaying||!visual)return;
            visual.localPosition=localPosition;visual.localRotation=localRotation;visual.localScale=localScale;
            if(animator){animator.speed=animatorSpeed;animator.cullingMode=culling;animator.enabled=animatorEnabled;}
            foreach(var pose in relaxed)if(pose.bone){pose.bone.localRotation=pose.start;pose.bone.localPosition=pose.position;}
            foreach(var state in collisionState)if(state.collider)state.collider.enabled=state.enabled;
            IsPlaying=false;prepared=false;previousCorrection=Vector3.zero;skins.Clear();relaxed.Clear();capturedPose.Clear();heights.Clear();
        }
    }
}
