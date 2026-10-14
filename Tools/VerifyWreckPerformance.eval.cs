if(!UnityEngine.Application.isPlaying)throw new System.Exception("Requires Story1 Play Mode");
var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
var battle=sequence.GetComponent<Story1WreckBattle>();
battle.minimumFightSeconds=battle.maximumFightSeconds=120;
sequence.sahur.GetComponent<Mavis.PlayerHealth>().GrantProtection(120);
float started=UnityEngine.Time.realtimeSinceStartup;
int stage=0;bool ended=false;
var checks=new System.Collections.Generic.List<string>();
UnityEditor.EditorApplication.CallbackFunction verify=null;
verify=()=>
{
    if(ended)return;
    try
    {
        if(!UnityEngine.Application.isPlaying || UnityEngine.Time.realtimeSinceStartup-started>45)throw new System.Exception("Verification interrupted or timed out");
        if(stage==0 && UnityEngine.Time.realtimeSinceStartup-started>3)
        {
            var staged=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(UnityEngine.FindObjectsSortMode.None);
            if(staged.Any(p=>p.IsBoardable))throw new System.Exception("Prepared timber entered gameplay before release");
            checks.Add("Prepared fragments stay invisible and unboardable");
            stage=1;sequence.BeginImpact();return;
        }
        if(stage!=1 || battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting || battle.FightElapsed<1)return;
        void Check(bool valid,string message){if(!valid)throw new System.Exception(message);checks.Add(message);}
        Check(battle.InitialFragmentCount==221 && battle.ExpectedInitialFragments==221,"All 221 initial authored fragments retained");
        var shapes=battle.Boards.Select(p=>p.Collision).Concat(battle.WreckDetails.Select(p=>p.Collision)).ToArray();
        var hitboxes=sequence.sahur.GetComponentsInChildren<UnityEngine.Collider>(true).Where(c=>!(c is UnityEngine.CharacterController) && !c.isTrigger && c.enabled && c.gameObject.activeInHierarchy).ToArray();
        int parts=0;
        foreach(var shape in shapes)
        {
            // Nonbuoyant sails/metal intentionally retire after sinking 10m.
            if(!shape.Active)continue;
            var body=shape.GetComponent<UnityEngine.Rigidbody>();
            if(body.isKinematic || !body.useGravity)throw new System.Exception("Released fragment lost physical motion: "+shape.name);
            foreach(var part in shape.Parts)
            {
                parts++;
                if(!part.convex || part.isTrigger || part.attachedRigidbody!=body)throw new System.Exception("Invalid solid geometry");
                foreach(var hitbox in hitboxes)if(!UnityEngine.Physics.GetIgnoreCollision(hitbox,part))throw new System.Exception("Prepared collision filter lost at release");
            }
        }
        checks.Add("Released mesh parts are solid dynamic bodies and retain every Sahur hurtbox filter: "+parts);
        var board=battle.Boards.First(p=>!p.IsBroken && p.FractureSource!=null);
        var collision=board.Collision;var retained=collision.Parts;
        collision.Initialize(board.GetComponent<UnityEngine.MeshFilter>().sharedMesh,board.FractureSource.splinters);
        Check(object.ReferenceEquals(retained,collision.Parts),"Repeated initialization reuses the exact same colliders");
        var cached=collision.CaptureGeometry();
        for(int i=0;i<cached.parts.Length;i++)
            if(UnityEngine.Vector3.Distance(cached.positions[i],cached.parts[i].transform.position)>.0001f || UnityEngine.Quaternion.Angle(cached.rotations[i],cached.parts[i].transform.rotation)>.01f)
                throw new System.Exception("Collision snapshot differs from rendered timber");
        checks.Add("Geometry snapshots preserve real interpolated transforms");
        var actor=battle.WreckContact;var anatomy=actor.CaptureAnatomy();
        float actualPenetration=actor.CurrentPenetration(),referencePenetration=0;
        var volumes=actor.GetComponentsInChildren<UnityEngine.CapsuleCollider>().Where(c=>c.name.StartsWith("Solid anatomy")).ToArray();
        foreach(var shape in shapes.Where(s=>s.Active))foreach(var volume in volumes)foreach(var part in shape.Parts)
            if(UnityEngine.Physics.ComputePenetration(volume,volume.transform.position,volume.transform.rotation,part,part.transform.position,part.transform.rotation,out _,out var depth))
                referencePenetration=UnityEngine.Mathf.Max(referencePenetration,depth);
        Check(UnityEngine.Mathf.Abs(actualPenetration-referencePenetration)<.00001f,"Cached live anatomy matches exhaustive native mesh penetration queries");
        var markers=Enumerable.Range(0,20).Select(i=>sequence.swimmer.position+new UnityEngine.Vector3(i%5*2-4,i%3-1,i/5*2-4))
            .Concat(battle.Boards.Take(10).SelectMany(p=>new[]{p.transform.position,p.transform.position+UnityEngine.Vector3.right*16}))
            .Concat(new[]{new UnityEngine.Vector3(-16.001f,0,-.001f),new UnityEngine.Vector3(15.999f,0,16.001f)}).ToArray();
        var independent=markers.Select(p=>actor.PoseObstruction(anatomy,p,sequence.swimmer.rotation,false)).ToArray();
        using(var planning=actor.BeginPlanning())for(int i=0;i<markers.Length;i++)
            if(UnityEngine.Mathf.Abs(independent[i]-actor.PoseObstruction(anatomy,markers[i],sequence.swimmer.rotation,false))>.000001f)
                throw new System.Exception("Shared geometry changes a planning collision score");
        checks.Add("42 planned poses produce identical independent and shared collision scores");
        var planningVolumes=(UnityEngine.CapsuleCollider[])typeof(Story1SharkWreckContact).GetField("planningVolumes",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(actor);
        var eligible=battle.Boards.Where(p=>!p.IsBroken && p.Collision.Active && UnityEngine.Time.time>=p.SharkContactAfter).Select(p=>p.Collision)
            .Concat(battle.WreckDetails.Where(p=>!p.IsBroken && p.Collision.Active).Select(p=>p.Collision)).Select(p=>p.CaptureGeometry()).ToArray();
        float BruteForce(UnityEngine.Vector3 marker,UnityEngine.Quaternion rotation)
        {
            var scale=sequence.swimmer.lossyScale;var at=marker-rotation*UnityEngine.Vector3.Scale(anatomy.nose,scale);
            UnityEngine.Vector3 World(UnityEngine.Vector3 local)=>at+rotation*UnityEngine.Vector3.Scale(local,scale);
            var animator=sequence.swimmer.GetComponent<TralaleroSwimAnimator>();
            float radius=UnityEngine.Mathf.Clamp(animator.RootMeshBounds.extents.x*UnityEngine.Mathf.Abs(scale.x)*.48f,1.2f,2),headRadius=radius*.45f,cost=0;
            var nose=World(anatomy.nose);var head=World(anatomy.head);var body=World(anatomy.body);var tailBase=World(anatomy.tailBase);var tip=World(anatomy.tailTip);
            var starts=new[]{nose+(head-nose).normalized*headRadius,head,body,tailBase};var ends=new[]{head,body,tailBase,tip};var radii=new[]{headRadius,radius,radius*.7f,radius*.32f};
            for(int n=0;n<4;n++)
            {
                var capsule=planningVolumes[n];var along=ends[n]-starts[n];var position=(starts[n]+ends[n])*.5f;
                var facing=along.sqrMagnitude>.00001f ? UnityEngine.Quaternion.LookRotation(along) : UnityEngine.Quaternion.identity;
                capsule.radius=radii[n];capsule.height=along.magnitude+2*radii[n];var half=along*.5f;
                var bounds=new UnityEngine.Bounds(position,2*(new UnityEngine.Vector3(UnityEngine.Mathf.Abs(half.x),UnityEngine.Mathf.Abs(half.y),UnityEngine.Mathf.Abs(half.z))+UnityEngine.Vector3.one*radii[n]));
                foreach(var shape in eligible)
                {
                    if(!shape.bounds.Intersects(bounds))continue;float depth=0;
                    for(int i=0;i<shape.parts.Length;i++)
                        if(shape.partBounds[i].Intersects(bounds) && UnityEngine.Physics.ComputePenetration(capsule,position,facing,shape.parts[i],shape.positions[i],shape.rotations[i],out _,out var penetration))
                            depth=UnityEngine.Mathf.Max(depth,penetration);
                    cost+=depth*depth;
                }
            }
            return cost;
        }
        for(int i=0;i<markers.Length;i++)
        {
            var rotation=UnityEngine.Quaternion.Euler(i%7*11,i*37,i%3*5);
            float indexed=actor.PoseObstruction(anatomy,markers[i],rotation,false),brute=BruteForce(markers[i],rotation);
            if(UnityEngine.Mathf.Abs(indexed-brute)>.000001f)throw new System.Exception("Spatial index changed exact collision cost: "+indexed+" vs "+brute);
        }
        checks.Add("Spatial index matches exhaustive solid-part queries for 42 positions and rotations, including cell boundaries");
        var floating=board.GetComponent<Story1WreckFloatBody>();
        var pressure=(UnityEngine.Vector3[])typeof(Story1WreckFloatBody).GetField("pressureLocal",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(floating);
        var size=board.Dimensions;float thin=UnityEngine.Mathf.Min(size.x,UnityEngine.Mathf.Min(size.y,size.z));
        var normal=thin==size.x ? UnityEngine.Vector3.right : thin==size.y ? UnityEngine.Vector3.up : UnityEngine.Vector3.forward;
        var axisA=thin==size.x ? UnityEngine.Vector3.up*(size.y*.35f) : UnityEngine.Vector3.right*(size.x*.35f);
        var axisB=thin==size.z ? UnityEngine.Vector3.up*(size.y*.35f) : UnityEngine.Vector3.forward*(size.z*.35f);
        var vertices=board.GetComponent<UnityEngine.MeshFilter>().sharedMesh.vertices;
        for(int i=0;i<4;i++)
        {
            var direction=axisA.normalized*(i%2==0 ? -1 : 1)+axisB.normalized*(i<2 ? 1 : -1);
            var extreme=vertices.OrderByDescending(p=>UnityEngine.Vector3.Dot(p,direction)).First();
            var expected=UnityEngine.Vector3.ProjectOnPlane(UnityEngine.Vector3.Scale(extreme,size)*.88f,normal);
            if(UnityEngine.Vector3.Distance(expected,pressure[i])>.00001f)throw new System.Exception("Cached buoyancy probe changed");
        }
        checks.Add("All four precomputed buoyancy probes match original geometry calculation");
        int before=battle.ShipFragmentCount;int shardCount=board.FractureSource.splinters.Length;
        Check(battle.DamageBoard(board,100,board.transform.position,UnityEngine.Vector3.up),"Real supported timber can still fracture on damage");
        Check(battle.ShipFragmentCount-before==shardCount,"Secondary fracture releases all authored splinters from the prepared pool");
        Check(battle.Boards.Where(p=>!p.IsBroken).All(p=>p.Collision!=null),"Every released board has valid support geometry");
        ended=true;UnityEditor.EditorApplication.update-=verify;
        System.IO.File.WriteAllText("ProjectRepairBackups/WreckPerformance-20261012/regression.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=true,checks,parts}));
        UnityEditor.SessionState.SetBool("WreckPerf.Verified",true);
    }
    catch(System.Exception error)
    {
        ended=true;UnityEditor.EditorApplication.update-=verify;
        System.IO.File.WriteAllText("ProjectRepairBackups/WreckPerformance-20261012/regression.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{passed=false,checks,error=error.ToString()}));
        UnityEditor.SessionState.SetBool("WreckPerf.Verified",true);
    }
};
UnityEditor.SessionState.SetBool("WreckPerf.Verified",false);UnityEditor.EditorApplication.update+=verify;
return "Scheduled physical wreck regression without blocking Unity";
