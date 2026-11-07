using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Optional stars, course progress and clear feedback for rescue assistance.</summary>
[DisallowMultipleComponent]
public sealed class MarshmallowHopPresentation : MonoBehaviour
{
    struct Marker { public Transform visual; public BoxCollider platform; public int bit; public float phase; }
    readonly List<Marker> markers = new List<Marker>();
    MarshmallowHopRecovery recovery;
    GameObject screen, card;
    Text progress, message;
    Mesh starMesh;
    Material starMaterial;
    string notice;
    float noticeUntil, nextHudRefresh;

    void Start()
    {
        recovery = GetComponent<MarshmallowHopRecovery>();
        BuildMarkers();
        BuildHud();
    }

    void BuildMarkers()
    {
        starMesh = new Mesh { name = "Challenge star" };
        var vertices = new Vector3[11];
        for (int i=0;i<10;i++)
        {
            float angle = Mathf.PI * .5f + i * Mathf.PI / 5f;
            float radius = i % 2 == 0 ? .85f : .38f;
            vertices[i+1] = new Vector3(Mathf.Cos(angle)*radius, Mathf.Sin(angle)*radius, 0);
        }
        var triangles = new int[30];
        for (int i=0;i<10;i++) { triangles[i*3]=0; triangles[i*3+1]=i+1; triangles[i*3+2]=(i+1)%10+1; }
        starMesh.vertices=vertices; starMesh.triangles=triangles; starMesh.RecalculateNormals();
        var shader=Shader.Find("Universal Render Pipeline/Unlit");
        if (!shader) shader=Shader.Find("Sprites/Default");
        if (!shader) return;
        starMaterial=new Material(shader) { color = new Color(1f,.84f,.25f) };
        if (starMaterial.HasProperty("_Cull")) starMaterial.SetFloat("_Cull",0);
        var holder=new GameObject("Challenge star markers").transform;
        holder.SetParent(transform,false);
        int row=0;
        foreach (Transform child in transform)
        {
            if (!child.name.StartsWith("Row ")) continue;
            row++;
            int bit=System.Array.IndexOf(MarshmallowHopRecovery.StarRows,row);
            if (bit<0) continue;
            int side=0;
            foreach (Transform platform in child)
            {
                if (side++ != MarshmallowHopRecovery.StarSides[bit]) continue;
                var box=platform.GetComponentInChildren<BoxCollider>();
                if (!box) continue;
                var obj=new GameObject("Floating course star",typeof(MeshFilter),typeof(MeshRenderer));
                var starCollider=obj.AddComponent<SphereCollider>();starCollider.isTrigger=true;starCollider.radius=.85f;
                obj.transform.SetParent(holder,false);
                obj.GetComponent<MeshFilter>().sharedMesh=starMesh;
                obj.GetComponent<MeshRenderer>().sharedMaterial=starMaterial;
                markers.Add(new Marker { visual=obj.transform, platform=box, bit=bit, phase=row*.37f });
            }
        }
    }

    void BuildHud()
    {
        screen=new GameObject("Marshmallow challenge progress",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler));
        screen.transform.SetParent(transform,false);
        var canvas=screen.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=75;
        GameUITheme.Scale(canvas);
        var rect=GameUITheme.Rect(screen.transform,"Course card",new Vector2(.5f,1),new Vector2(500,105));
        rect.pivot=new Vector2(.5f,1);rect.anchoredPosition=new Vector2(0,-25);
        GameUITheme.Image(rect,new Color(.045f,.035f,.065f,.9f));
        card=rect.gameObject;
        progress=GameUITheme.Label(rect,"Course progress",23,new Vector2(0,-27),new Vector2(470,35),TextAnchor.MiddleCenter);
        message=GameUITheme.Label(rect,"Course feedback",19,new Vector2(0,-67),new Vector2(470,45),TextAnchor.MiddleCenter);
        message.color=new Color(1f,.86f,.53f);
        card.SetActive(false);
    }

    public void ShowFall(int row, bool assisted)
    {
        notice = assisted ? "ASSIST ON  ·  Slower platforms  ·  Rescue row " + row + "\nStars and rewards remain available." : "Back to row " + row + "  ·  Try again!";
        noticeUntil = Time.time + 5f;
        nextHudRefresh=0;
    }
    public void ShowReturn()
    { notice="Returned to start  ·  Your stars are saved.";noticeUntil=Time.time+5;nextHudRefresh=0; }
    public void ShowStar(int count)
    {
        notice = count == 5 ? "All 5 stars found!" : "Star collected!  " + count + " / 5";
        noticeUntil = Time.time + 4f;
        nextHudRefresh=0;
    }

    void Update()
    {
        if (!recovery) return;
        var view=Camera.main;
        foreach (var marker in markers)
        {
            if (!marker.visual || !marker.platform) continue;
            bool visible=(recovery.StarMask & (1 << marker.bit)) == 0;
            if (marker.visual.gameObject.activeSelf != visible) marker.visual.gameObject.SetActive(visible);
            if (!visible) continue;
            var bounds=marker.platform.bounds;
            marker.visual.position=new Vector3(bounds.center.x,bounds.max.y+2.7f+.18f*Mathf.Sin(Time.time*2+marker.phase),bounds.center.z);
            if (view) marker.visual.rotation=view.transform.rotation*Quaternion.Euler(0,0,Mathf.Sin(Time.time*2+marker.phase)*12f);
        }
        if (!card) return;
        bool show=recovery.CurrentRow>0 && !GameUITheme.ModalOpen && !MarshmallowChallengeGuide.AnyConversation;
        if(card.activeSelf!=show)card.SetActive(show);
        if (!show) return;
        if(Time.unscaledTime<nextHudRefresh)return;
        nextHudRefresh=Time.unscaledTime+.2f;
        progress.text="MARSHMALLOW HOP   " + recovery.CurrentRow + " / 22    |    BONUS STARS " + recovery.StarCount + " / 5";
        message.text=Time.time<noticeUntil ? notice : recovery.AssistanceActive ?
            "ASSIST ON  ·  Slower platforms  ·  Rescue row " + recovery.RescueRow :
            "Stars are optional. Hold Backspace to return to start.";
    }

    void OnDestroy()
    {
        if(screen)Destroy(screen);
        foreach(var marker in markers) if(marker.visual) Destroy(marker.visual.gameObject);
        if(starMesh)Destroy(starMesh);
        if(starMaterial)Destroy(starMaterial);
    }
}
