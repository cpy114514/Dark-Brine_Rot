using UnityEngine;
/// <summary>Final contacts and camera follow the anatomical wreck solver (1200).</summary>
[DefaultExecutionOrder(1300)]
public sealed class Story1FinalePostProcess : MonoBehaviour
{
    public Story1WreckFinale film;
    void LateUpdate(){if(film!=null)film.ResolveContactsAndCamera();}
}
