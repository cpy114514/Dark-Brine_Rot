using Mavis;
using UnityEngine;

/// <summary>The story shark takes normal weapon hits, but cannot end this encounter in victory.</summary>
public sealed class Story1SharkHealth : Health
{
    public int ReceivedHits { get; private set; }
    public float NarrativeHealthFloor { get; set; } = 1f;
    public bool IsProtected { get; set; }
    Collider head;
    public void ConfigureHead(Collider collider) => head=collider;
    public Vector3 LockPoint
    {
        get
        {
            if(head!=null)return head.bounds.center+Vector3.up*.2f;
            var hitbox=GetComponent<BoxCollider>();
            return hitbox!=null ? hitbox.bounds.center+Vector3.up*(hitbox.bounds.extents.y*.95f) : transform.position;
        }
    }
    public override void ApplyDamage(float amount, Vector3 hitPoint)
    {
        if (amount <= 0 || IsDead || IsProtected) return;
        ReceivedHits++;
        float damage = Mathf.Min(amount, Mathf.Max(0, currentHealth - NarrativeHealthFloor));
        if (damage > 0) base.ApplyDamage(damage, hitPoint);
        else OnDamaged?.Invoke(amount);
    }
}
