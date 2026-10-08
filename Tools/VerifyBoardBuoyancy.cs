using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Mavis;
public static class VerifyBoardBuoyancy
{
    public static async Task<object> Verify(string label="after",bool enforce=false)
    {
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        GameObject[] fixtures=null;
        try
        {
            GameSaveManager.CancelPendingContinue();var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
            while(!load.isDone)await Task.Delay(30);await Task.Delay(300);
            var sequence=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();sequence.enabled=false;
            sequence.sahur.gameObject.AddComponent<GameSaveExcluded>();sequence.sahur.enabled=false;
            var water=UnityEngine.Object.FindFirstObjectByType<OceanWorld>();
            fixtures=new GameObject[6];var bodies=new Rigidbody[4];var minY=new float[5];var maxY=new float[5];var speed2=new double[5];var tilt2=new double[5];var maxTilt=new float[5];var previousY=new float[5];
            // Keep fixture forces independent of the large ship's moving collision hull.
            var worldColliders=UnityEngine.Object.FindObjectsByType<Collider>(FindObjectsSortMode.None);
            Vector3 origin=sequence.sahur.transform.position+Vector3.right*90;
            for(int i=0;i<6;i++)
            {
                var go=GameObject.CreatePrimitive(PrimitiveType.Cube);fixtures[i]=go;go.name="Buoyancy verification "+i;
                UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());go.AddComponent<BoxCollider>();
                Vector3 size=i!=3 ? new Vector3(9.39f,.7f,19.4f) : new Vector3(2,2,8);go.transform.localScale=size;
                Vector3 at=origin+Vector3.right*(i*30);at.y=water.SampleSurfaceHeight(at,Time.time)+(i==2 ? 12 : i==3 ? 8 : .2f);
                go.transform.SetPositionAndRotation(at,i==1 || i==5 ? Quaternion.Euler(78,0,0) : Quaternion.identity);
                if(i<4)
                {
                    foreach(var collider in worldColliders)if(collider!=null)Physics.IgnoreCollision(go.GetComponent<BoxCollider>(),collider);
                    bodies[i]=go.AddComponent<Rigidbody>();go.AddComponent<Story1WreckFloatBody>().Initialize(water,size,i<3,Vector3.zero,Vector3.zero);
                }
                else{UnityEngine.Object.DestroyImmediate(go.GetComponent<BoxCollider>());go.AddComponent<Story1WreckDebris>().Initialize(water,size,true,Vector3.zero,Vector3.zero);}
            }
            float dropStart=bodies[2].position.y;
            await Task.Delay(600);float gravityDrop=dropStart-bodies[2].position.y;
            await Task.Delay(6400);
            for(int i=0;i<5;i++){minY[i]=float.PositiveInfinity;maxY[i]=float.NegativeInfinity;previousY[i]=fixtures[i<3 ? i : i+1].transform.position.y;}
            int count=0;float sampleStart=Time.time,lastSample=Time.time;
            while(Time.time-sampleStart<9)
            {
                float dt=Mathf.Max(.001f,Time.time-lastSample);
                for(int i=0;i<5;i++)
                {
                    var fixture=fixtures[i<3 ? i : i+1];
                    float y=fixture.transform.position.y,tilt=Vector3.Angle(fixture.transform.rotation*Vector3.up,Vector3.up);tilt=Mathf.Min(tilt,180-tilt);
                    float speed=i<3 ? bodies[i].linearVelocity.y : (y-previousY[i])/dt;
                    minY[i]=Mathf.Min(minY[i],y);maxY[i]=Mathf.Max(maxY[i],y);speed2[i]+=speed*speed;tilt2[i]+=tilt*tilt;maxTilt[i]=Mathf.Max(maxTilt[i],tilt);previousY[i]=y;
                }
                count++;lastSample=Time.time;await Task.Delay(20);
            }
            float sinkingClearance=bodies[3].position.y-water.SampleSurfaceHeight(bodies[3].position,Time.time);
            var result=new{label,count,gravityDrop,sinkingClearance,gravityPreserved=bodies.All(b=>b.useGravity && !b.isKinematic),
                boards=Enumerable.Range(0,5).Select(i=>new{index=i,kind=i<3 ? "playable" : "decorative",heightRange=maxY[i]-minY[i],rmsVerticalSpeed=Math.Sqrt(speed2[i]/count),rmsTilt=Math.Sqrt(tilt2[i]/count),maxTilt=maxTilt[i]}).ToArray()};
            Directory.CreateDirectory(Path.GetFullPath(".codex/board-buoyancy-20261007"));
            File.WriteAllText(Path.GetFullPath(".codex/board-buoyancy-20261007/"+label+".json"),Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            if(enforce && (!result.gravityPreserved || gravityDrop<.8f || sinkingClearance>-4 || result.boards.Any(b=>b.maxTilt>35 || b.rmsVerticalSpeed>4)))
                throw new Exception("Buoyancy regression failed: "+Newtonsoft.Json.JsonConvert.SerializeObject(result));
            return result;
        }
        finally
        {
            if(fixtures!=null)foreach(var go in fixtures)if(go!=null)UnityEngine.Object.Destroy(go);
            GameSaveManager.CancelPendingContinue();if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
}
