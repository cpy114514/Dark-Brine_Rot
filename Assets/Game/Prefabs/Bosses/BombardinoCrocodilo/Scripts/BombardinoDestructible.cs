using UnityEngine;
using UnityEngine.Events;

namespace Mavis
{
    /// <summary>Opt-in destruction for encounter structures, never terrain or scene roots.</summary>
    public sealed class BombardinoDestructible : MonoBehaviour
    {
        public float integrity=120f;
        public UnityEvent onDestroyed=new UnityEvent();
        public bool IsDestroyed { get; private set; }
        public void ApplyBlast(float amount,Vector3 origin)
        {
            if(IsDestroyed||amount<=0)return;integrity=Mathf.Max(0,integrity-amount);if(integrity>0)return;
            IsDestroyed=true;onDestroyed.Invoke();
            foreach(var collider in GetComponentsInChildren<Collider>())collider.enabled=false;
            foreach(var renderer in GetComponentsInChildren<MeshRenderer>())
            {
                var mf=renderer.GetComponent<MeshFilter>();if(mf==null||mf.sharedMesh==null)continue;
                var chunk=new GameObject("Bombed structure debris",typeof(MeshFilter),typeof(MeshRenderer),typeof(BoxCollider),typeof(Rigidbody));
                UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(chunk,gameObject.scene);
                chunk.transform.SetPositionAndRotation(renderer.transform.position,renderer.transform.rotation);chunk.transform.localScale=renderer.transform.lossyScale;
                chunk.GetComponent<MeshFilter>().sharedMesh=mf.sharedMesh;chunk.GetComponent<MeshRenderer>().sharedMaterials=renderer.sharedMaterials;
                chunk.GetComponent<Rigidbody>().AddExplosionForce(180f,origin,12f,2f,ForceMode.Impulse);renderer.enabled=false;
                if(Application.isPlaying)Destroy(chunk,5f);else DestroyImmediate(chunk);
            }
        }
    }
}
