using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mavis;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class VerifyWreckScatter
{
    static void Check(bool value,string message){if(!value)throw new Exception(message);}
    static Vector3 Plane(Vector3 p)=>Vector3.ProjectOnPlane(p,Vector3.up);
    static float Gap(Story1WreckPlank a,Story1WreckPlank b)
    {
        float gap=float.PositiveInfinity;
        for(int x=-1;x<=1;x+=2)for(int z=-1;z<=1;z+=2)
        {
            Vector3 at=a.transform.TransformPoint(new Vector3(x*.5f,0,z*.5f));
            gap=Mathf.Min(gap,Plane(at-b.Deck.ClosestPoint(at)).magnitude);
            at=b.transform.TransformPoint(new Vector3(x*.5f,0,z*.5f));
            gap=Mathf.Min(gap,Plane(at-a.Deck.ClosestPoint(at)).magnitude);
        }
        return gap;
    }
    public static async Task<object> Verify()
    {
        Check(Application.isPlaying,"Requires Play Mode.");
        string save=Path.Combine(Application.persistentDataPath,"dark-brine-rot-save.json");
        byte[] original=File.Exists(save)?File.ReadAllBytes(save):null;
        var signatures=new HashSet<string>();var results=new List<object>();
        try
        {
            for(int run=0;run<3;run++)
            {
                GameSaveManager.CancelPendingContinue();
                var load=SceneManager.LoadSceneAsync("Assets/Scenes/First story/Story1.unity");
                while(!load.isDone)await Task.Delay(40);await Task.Delay(300);
                var seq=UnityEngine.Object.FindFirstObjectByType<Story1SharkCollisionSequence>();
                var battle=seq.GetComponent<Story1WreckBattle>();seq.BeginImpact();
                for(int i=0;i<120 && battle.CurrentPhase!=Story1WreckBattle.Phase.Fighting;i++)await Task.Delay(50);
                Check(battle.CurrentPhase==Story1WreckBattle.Phase.Fighting,"Scatter did not finish.");
                var actor=seq.sahur;actor.GetComponent<PlayerHealth>().GrantProtection(60);
                await Task.Delay(250);
                var boards=UnityEngine.Object.FindObjectsByType<Story1WreckPlank>(FindObjectsSortMode.None);
                var decks=boards.Where(p=>p.Dimensions.x>=3).ToArray();
                Check(boards.Length==44 && decks.Length==20 && battle.ShipFragmentCount==202,"Random layout omitted fragments.");
                var reachable=new HashSet<Story1WreckPlank>{decks[0]};
                for(int pass=0;pass<20;pass++)foreach(var a in decks)foreach(var b in decks)
                    if(reachable.Contains(a) && Gap(a,b)<3.8f)reachable.Add(b);
                Check(reachable.Count==20,"Scattering left a deck outside jumping range.");
                int yawBins=decks.Select(p=>Mathf.FloorToInt(p.transform.eulerAngles.y/30)).Distinct().Count();
                Check(yawBins>=6,"Boards still face repeated grid directions.");
                string signature=string.Join(";",decks.OrderBy(p=>p.name).Select(p=>p.transform.position.ToString()+p.transform.eulerAngles.y.ToString("F1")));
                Check(signatures.Add(signature),"Encounter reused the same scatter layout.");
                var support=actor.GetComponent<Story1WreckRider>().Support();
                Check(support!=null && actor.enabled && !actor.ExternalControlLock,"Actor did not land on a usable board.");
                foreach(var a in boards)foreach(var b in boards)
                {
                    if(a==b)continue;
                    bool overlaps=Physics.ComputePenetration(a.Deck,a.transform.position,a.transform.rotation,
                        b.Deck,b.transform.position,b.transform.rotation,out _,out float depth);
                    Check(!overlaps || depth<.02f,"Random boards overlap physically.");
                }
                Shot(actor.transform.position,run);
                results.Add(new{run,realPieces=battle.ShipFragmentCount,solidBoards=boards.Length,reachableDecks=reachable.Count,yawBins,
                    playerLandsOnBoard=true,noBoardOverlap=true});
            }
            string path=Path.GetFullPath(".codex/wreck-scatter-preview/verification.json");
            File.WriteAllText(path,Newtonsoft.Json.JsonConvert.SerializeObject(results,Newtonsoft.Json.Formatting.Indented));
            return new{independentLayouts=signatures.Count,runs=results};
        }
        finally
        {
            var actor=UnityEngine.Object.FindFirstObjectByType<ThirdPersonPlayerController>();
            if(actor!=null && actor.GetComponent<GameSaveExcluded>()==null)actor.gameObject.AddComponent<GameSaveExcluded>();
            GameSaveManager.CancelPendingContinue();
            if(original!=null)File.WriteAllBytes(save,original);else if(File.Exists(save))File.Delete(save);
        }
    }
    static void Shot(Vector3 center,int run)
    {
        string dir=Path.GetFullPath(".codex/wreck-scatter-preview");Directory.CreateDirectory(dir);
        var go=new GameObject("Scatter inspection camera");var camera=go.AddComponent<Camera>();camera.enabled=false;
        camera.CopyFrom(Camera.main);camera.enabled=false;camera.orthographic=true;camera.orthographicSize=68;
        go.transform.SetPositionAndRotation(center+Vector3.up*135-Vector3.forward*24,Quaternion.LookRotation(-Vector3.up*135+Vector3.forward*24));
        var texture=new RenderTexture(1024,768,24);var frame=new Texture2D(1024,768,TextureFormat.RGB24,false);
        var active=RenderTexture.active;
        try{camera.targetTexture=texture;camera.Render();RenderTexture.active=texture;frame.ReadPixels(new Rect(0,0,1024,768),0,0);frame.Apply();File.WriteAllBytes(Path.Combine(dir,"layout-"+run+".png"),frame.EncodeToPNG());}
        finally{RenderTexture.active=active;texture.Release();UnityEngine.Object.DestroyImmediate(texture);UnityEngine.Object.DestroyImmediate(frame);UnityEngine.Object.DestroyImmediate(go);}
    }
}
