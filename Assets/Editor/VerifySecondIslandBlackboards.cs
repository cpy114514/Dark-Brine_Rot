using System;
using System.Reflection;
using System.Threading.Tasks;
using Mavis;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>Checks actual scene signs, dynamic lettering and screen typography in play mode.</summary>
public static class VerifySecondIslandBlackboards
{
    public static string Status="idle",Result="";
    static async Task Wait(float seconds)
    {
        float end=Time.realtimeSinceStartup+seconds;
        while(Time.realtimeSinceStartup<end){if(!EditorApplication.isPlaying)throw new Exception("Play mode stopped.");await Task.Delay(25);}
    }
    public static async void Run()
    {
        Status="running";Result="";
        try
        {
            if(!EditorApplication.isPlaying)throw new Exception("Enter Play mode first.");
            typeof(GameSaveManager).GetField("editorSavePath",BindingFlags.NonPublic|BindingFlags.Static).SetValue(null,System.IO.Path.GetFullPath("Temp/blackboard-test.json"));
            var opening=UnityEngine.Object.FindFirstObjectByType<SecondIslandAwakening>();if(opening)UnityEngine.Object.DestroyImmediate(opening.gameObject);
            await Wait(1.5f);
            var scene=UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var boards=UnityEngine.Object.FindObjectsByType<SecondIslandBlackboardLabel>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            var authored=UnityEngine.Object.FindObjectsByType<SecondIslandAuthoredChalk>(FindObjectsInactive.Include,FindObjectsSortMode.None);
            if(boards.Length<35 || authored.Length<6)throw new Exception("Some world captions did not receive blackboards: "+boards.Length+" / "+authored.Length);
            int visible=0;
            foreach(var board in boards)
            {
                if(!board.gameObject.activeInHierarchy)continue;
                var source=board.GetComponentInChildren<Text>(true);var text=board.ChalkText;
                if(!source || source.enabled || !text || !text.font || text.text!=source.text)throw new Exception("Text did not synchronize: "+board.name);
                var collider=board.GetComponent<BoxCollider>();if(!collider || !collider.isTrigger)throw new Exception("Blackboard must have a non-blocking collider: "+board.name);
                foreach(var renderer in board.GetComponentsInChildren<Renderer>(true))
                    if(!renderer.GetComponent<Collider>() && !renderer.GetComponentInParent<Collider>())throw new Exception("Blackboard visual lacks collision: "+renderer.name);
                if(string.IsNullOrEmpty(text.text))continue;
                text.ForceMeshUpdate(true,true);var b=text.textBounds.size;var r=text.rectTransform.rect;
                if(!text.mesh || text.mesh.vertexCount==0 || b.x>r.width+1 || b.y>r.height+1 || b.x<r.width*.15f)throw new Exception("Text is missing or poorly fitted: "+board.name+" / "+b+" / "+r.size);
                visible++;
            }
            if(visible<8)throw new Exception("Too few readable active boards: "+visible);
            var garden=UnityEngine.Object.FindFirstObjectByType<CandyBounceGarden>();
            var sign=garden.Sign.Find("Bounce label");var chalk=sign.GetComponent<SecondIslandBlackboardLabel>();
            var originalRotation=sign.rotation;
            var camera=Camera.main;var oldRotation=camera.transform.rotation;camera.transform.rotation=oldRotation*Quaternion.Euler(0,30,0);
            await Wait(.2f);if(Quaternion.Angle(sign.rotation,originalRotation)>.1f)throw new Exception("Fixed garden sign rotates independently of its frame.");camera.transform.rotation=oldRotation;
            var sourceText=sign.GetComponentInChildren<Text>(true);string old=sourceText.text;sourceText.text="DYNAMIC BLACKBOARD TEST";await Wait(.2f);
            if(chalk.ChalkText.text!=sourceText.text)throw new Exception("Changed caption did not update the 3D lettering.");sourceText.text=old;await Wait(.2f);
            var font=SecondIslandBlackboards.InterfaceFont;if(!font || font.name!="LiberationSans")throw new Exception("Readable interface font missing.");
            var reusable=Resources.Load<GameObject>("SecondIslandBlackboard/Candy Blackboard");
            if(!reusable || !reusable.GetComponentInChildren<TextMeshPro>(true) || !reusable.GetComponentInChildren<Collider>(true))throw new Exception("Reusable blackboard prefab or its text and collision are missing.");
            int ui=0;foreach(var t in UnityEngine.Object.FindObjectsByType<Text>(FindObjectsInactive.Include,FindObjectsSortMode.None))
            {
                var canvas=t.GetComponentInParent<Canvas>(true);
                if(t.gameObject.scene!=scene || !canvas || canvas.renderMode==RenderMode.WorldSpace)continue;
                if(t.font!=font)throw new Exception("Island screen text uses another font: "+t.name);ui++;
            }
            var guide=UnityEngine.Object.FindFirstObjectByType<IslandActivityGuide>();var label=guide.GetComponentInChildren<Text>(true);
            if(label.rectTransform.anchoredPosition.y>=0)throw new Exception("Fairy tip is outside its blackboard card.");
            var weapon=UnityEngine.Object.FindFirstObjectByType<SecondIslandWeaponBackdrop>();
            if(!weapon || !weapon.source || !weapon.source.text.StartsWith("Current weapon") || !weapon.source.isActiveAndEnabled || !weapon.GetComponent<Image>().enabled)
                throw new Exception("Weapon blackboard is not attached to the visible weapon caption.");
            Status="passed";Result=boards.Length+" world labels, "+authored.Length+" authored signs, "+visible+" readable active boards and "+ui+" screen texts checked; prefab, dynamic wording, collision, fixed orientation, readable font and fairy card passed.";
        }
        catch(Exception e){Status="failed";Result=e.ToString();}
        Debug.Log("Second Island blackboard review: "+Status+" "+Result);
    }
}
