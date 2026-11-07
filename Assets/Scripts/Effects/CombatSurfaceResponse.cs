using UnityEngine;

namespace Mavis
{
    // Optional explicit authoring wins over imported material names. No collision or damage changes.
    [DisallowMultipleComponent]
    public sealed class CombatSurfaceResponse : MonoBehaviour
    {
        public CombatMaterial material=CombatMaterial.Soil;
        public static CombatMaterial Resolve(Collider collider,Vector3 point)
        {
            if(!collider)return CombatMaterial.Soil;
            var authored=collider.GetComponentInParent<CombatSurfaceResponse>();
            if(authored)return authored.material;
            var actor=collider.GetComponentInParent<CombatImpactResponse>();
            if(actor)return actor.material;
            if(collider.GetComponentInParent<IDamageable>()!=null)return CombatMaterial.Flesh;
            var island=collider.GetComponentInParent<ProceduralIsland>();
            if(island)
            {
                var ocean=Object.FindFirstObjectByType<OceanWorld>();
                float sea=ocean?ocean.oceanHeight:0;
                return point.y<sea+island.beachHeight?CombatMaterial.Sand:CombatMaterial.Soil;
            }
            if(collider.GetComponentInParent<OceanWorld>())return CombatMaterial.Water;
            var renderer=collider.GetComponent<Renderer>();
            if(!renderer)renderer=collider.GetComponentInParent<Renderer>();
            string name=(collider.name+" "+(renderer&&renderer.sharedMaterial?renderer.sharedMaterial.name:"")).ToLowerInvariant();
            if(name.Contains("metal")||name.Contains("steel")||name.Contains("iron"))return CombatMaterial.Metal;
            if(name.Contains("vegetation")||name.Contains("leaf")||name.Contains("leaves")||name.Contains("grass")||name.Contains("fern")||name.Contains("bush"))return CombatMaterial.Vegetation;
            if(name.Contains("timber")||name.Contains("deck")||name.Contains("wood")||name.Contains("bark")||name.Contains("trunk")||name.Contains("tree")||name.Contains("plank"))return CombatMaterial.Wood;
            if(name.Contains("rock")||name.Contains("stone")||name.Contains("cliff"))return CombatMaterial.Rock;
            if(name.Contains("sand")||name.Contains("beach"))return CombatMaterial.Sand;
            if(name.Contains("water")||name.Contains("ocean"))return CombatMaterial.Water;
            return CombatMaterial.Soil;
        }
        public static Color Body(CombatMaterial surface)
        {
            switch(surface)
            {
                case CombatMaterial.Wood:return new Color(.23f,.15f,.075f);
                case CombatMaterial.Metal:return new Color(.22f,.24f,.25f);
                case CombatMaterial.Rock:return new Color(.30f,.30f,.28f);
                case CombatMaterial.Sand:return new Color(.48f,.39f,.25f);
                case CombatMaterial.Soil:return new Color(.24f,.19f,.13f);
                case CombatMaterial.Water:return new Color(.19f,.31f,.34f);
                case CombatMaterial.Vegetation:return new Color(.20f,.26f,.12f);
                default:return new Color(.18f,.165f,.14f);
            }
        }
        public static bool Dust(CombatMaterial surface)=>surface==CombatMaterial.Rock||surface==CombatMaterial.Sand||surface==CombatMaterial.Soil||surface==CombatMaterial.Wood;
    }
}
