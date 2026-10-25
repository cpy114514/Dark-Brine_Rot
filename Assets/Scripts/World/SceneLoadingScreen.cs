using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>A single white progress line, retained through scene activation and world restoration.</summary>
    public sealed class SceneLoadingScreen : MonoBehaviour
    {
        static SceneLoadingScreen instance;
        RectTransform fill;
        float progress;
        bool loading;
        public static bool IsLoading => instance!=null&&instance.loading;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetState()=>instance=null;

        static SceneLoadingScreen Create()
        {
            var root=new GameObject("Loading screen",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            var canvas=root.GetComponent<Canvas>();canvas.renderMode=RenderMode.ScreenSpaceOverlay;canvas.sortingOrder=32760;
            var scaler=root.GetComponent<CanvasScaler>();scaler.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution=new Vector2(1920,1080);scaler.matchWidthOrHeight=.5f;
            instance=root.AddComponent<SceneLoadingScreen>();instance.loading=true;
            DontDestroyOnLoad(root);return instance;
        }
        void Awake()
        {
            var background=new GameObject("Black background",typeof(RectTransform),typeof(Image));background.transform.SetParent(transform,false);
            var rect=background.GetComponent<RectTransform>();rect.anchorMin=Vector2.zero;rect.anchorMax=Vector2.one;rect.offsetMin=rect.offsetMax=Vector2.zero;
            background.GetComponent<Image>().color=Color.black;
            var line=new GameObject("Progress line",typeof(RectTransform));line.transform.SetParent(transform,false);
            var track=line.GetComponent<RectTransform>();track.anchorMin=new Vector2(.32f,.13f);track.anchorMax=new Vector2(.68f,.13f);track.sizeDelta=new Vector2(0,4);
            var white=new GameObject("White fill",typeof(RectTransform),typeof(Image));white.transform.SetParent(line.transform,false);
            fill=white.GetComponent<RectTransform>();fill.anchorMin=Vector2.zero;fill.anchorMax=new Vector2(0,1);fill.offsetMin=fill.offsetMax=Vector2.zero;
            var image=white.GetComponent<Image>();image.color=Color.white;image.raycastTarget=false;
        }
        public static bool Load(string scenePath)
        {
            if(IsLoading||!Application.CanStreamedLevelBeLoaded(scenePath))return false;
            var screen=Create();screen.StartCoroutine(screen.LoadScene(scenePath));return true;
        }
        public static void ShowWorldLoading()
        {
            if(IsLoading)return;
            var screen=Create();screen.StartCoroutine(screen.WaitForWorld());
        }
        void Report(float value)
        {
            value=Mathf.Clamp01(value);if(value<=progress)return;
            progress=value;fill.anchorMax=new Vector2(value,1);
        }
        static AsyncOperation BeginScene(string scenePath)
        {
            try{return SceneManager.LoadSceneAsync(scenePath,LoadSceneMode.Single);}
            catch(System.Exception e){Debug.LogException(e);return null;}
        }
        IEnumerator LoadScene(string scenePath)
        {
            // Let the black screen draw before Unity starts integrating assets.
            yield return null;
            var operation=BeginScene(scenePath);
            if(operation==null){GameSaveManager.CancelPendingContinue();Close();yield break;}
            while(!operation.isDone)
            {
                Report(Mathf.Clamp01(operation.progress/.9f)*.2f);
                yield return null;
            }
            Report(.2f);
            // OnEnable/Start of the new world register its additive loads first.
            yield return null;
            yield return WaitForWorld();
        }
        IEnumerator WaitForWorld()
        {
            float deadline=Time.realtimeSinceStartup+90;
            while(AdditiveSceneBootstrap.IsLoading||GameSaveManager.IsContinuePending)
            {
                Report(.2f+.75f*AdditiveSceneBootstrap.Progress);
                if(Time.realtimeSinceStartup>=deadline)
                {
                    Debug.LogError("[Loading] World initialization timed out; see scene/save diagnostics.");
                    Close();yield break;
                }
                yield return null;
            }
            Report(1);
            yield return null;
            Close();
        }
        void Close(){loading=false;if(instance==this)instance=null;Destroy(gameObject);}
        void OnDestroy(){if(instance==this)instance=null;}
    }
}
