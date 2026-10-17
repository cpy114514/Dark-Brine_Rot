using UnityEngine;
using UnityEngine.UI;

namespace Mavis
{
    /// <summary>Small pen drawings. Geometry is rebuilt only when the drawing or UI size changes.</summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class EquipmentJournalInk : MaskableGraphic
    {
        public enum Drawing { Border, Rule, Stick, Blades, Glasses, Head, Coat, Trousers, Boots, Skill, Rifle }
        Drawing drawing;
        public void SetDrawing(Drawing value) { drawing = value; SetVerticesDirty(); }
        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            var r = rectTransform.rect;
            if (drawing == Drawing.Border)
            {
                Path(vh, r, 1.2f, 0,0.04f, .24f,.015f, .62f,.025f, .99f,0, 1,.47f, .985f,1, .66f,.98f, .24f,1, .01f,.985f, 0,.04f);
                Path(vh, r, .55f, .012f,.065f, .02f,.96f, .38f,.99f, .84f,.97f);
                return;
            }
            if (drawing == Drawing.Rule)
            {
                Path(vh,r,1.15f,0,.5f,.17f,.62f,.43f,.4f,.7f,.55f,1,.42f);
                Path(vh,r,.5f,.03f,.24f,.31f,.3f,.65f,.2f,.94f,.28f);
                return;
            }
            // Drawings use a square, normalized paper space and deliberately uneven contours.
            float side = Mathf.Min(r.width,r.height);
            r = new Rect(r.center-new Vector2(side,side)*.5f,new Vector2(side,side));
            switch(drawing)
            {
                case Drawing.Rifle:
                    Path(vh,r,1.7f,.04f,.58f,.22f,.58f,.24f,.64f,.63f,.64f,.66f,.6f,.94f,.58f,.94f,.44f,.84f,.44f,.69f,.5f,.62f,.49f,.68f,.3f,.59f,.29f,.53f,.48f,.47f,.48f,.43f,.27f,.32f,.29f,.36f,.5f,.24f,.51f,.22f,.55f,.04f,.55f,.04f,.58f);
                    Path(vh,r,1.2f,.47f,.64f,.49f,.74f,.66f,.74f,.68f,.65f);
                    Path(vh,r,.7f,.28f,.54f,.28f,.61f);Path(vh,r,.7f,.32f,.54f,.32f,.61f);Path(vh,r,.7f,.36f,.54f,.36f,.61f);
                    break;
                case Drawing.Stick:
                    Path(vh,r,2.1f,.22f,.12f,.16f,.17f,.3f,.4f,.48f,.65f,.72f,.91f,.8f,.87f,.61f,.59f,.43f,.34f,.22f,.12f);
                    Path(vh,r,1,.22f,.22f,.42f,.46f,.52f,.65f,.72f,.85f);
                    Path(vh,r,1.2f,.3f,.34f,.36f,.32f,.39f,.37f, .36f,.42f);
                    Path(vh,r,1,.6f,.68f,.64f,.7f,.63f,.76f);
                    Path(vh,r,.7f,.2f,.17f,.26f,.18f,.29f,.23f);
                    break;
                case Drawing.Blades:
                    Path(vh,r,1.9f,.18f,.14f,.73f,.87f,.72f,.57f,.32f,.22f);
                    Path(vh,r,1.9f,.82f,.14f,.27f,.87f,.28f,.57f,.68f,.22f);
                    Path(vh,r,2,.14f,.29f,.34f,.14f);Path(vh,r,2,.67f,.14f,.87f,.29f);
                    Path(vh,r,1,.3f,.35f,.65f,.79f);Path(vh,r,1,.7f,.35f,.35f,.79f);
                    Path(vh,r,2.6f,.16f,.1f,.26f,.23f);Path(vh,r,2.6f,.84f,.1f,.74f,.23f);
                    break;
                case Drawing.Glasses:
                    Path(vh,r,2,.08f,.57f,.17f,.64f,.4f,.62f,.42f,.43f,.34f,.34f,.17f,.36f,.08f,.57f);
                    Path(vh,r,2,.58f,.44f,.6f,.62f,.84f,.64f,.93f,.57f,.83f,.36f,.66f,.34f,.58f,.44f);
                    Path(vh,r,1.8f,.42f,.54f,.5f,.57f,.59f,.54f);Path(vh,r,1.8f,.09f,.58f,.03f,.77f,.12f,.8f);
                    Path(vh,r,1.8f,.92f,.58f,.98f,.77f,.88f,.8f);Path(vh,r,.8f,.18f,.43f,.32f,.56f);
                    Path(vh,r,.8f,.68f,.43f,.82f,.56f);break;
                case Drawing.Head:
                    Path(vh,r,1.6f,.15f,.34f,.19f,.67f,.33f,.83f,.69f,.81f,.82f,.65f,.85f,.34f,.15f,.34f);
                    Path(vh,r,1.2f,.08f,.31f,.92f,.32f);Path(vh,r,1,.32f,.42f,.35f,.65f);break;
                case Drawing.Coat:
                    Path(vh,r,1.6f,.37f,.86f,.19f,.79f,.05f,.5f,.23f,.4f,.3f,.56f,.29f,.15f,.73f,.16f,.7f,.56f,.78f,.41f,.95f,.51f,.82f,.79f,.63f,.86f,.57f,.74f,.45f,.74f,.37f,.86f);
                    Path(vh,r,1,.5f,.73f,.51f,.23f);break;
                case Drawing.Trousers:
                    Path(vh,r,1.6f,.25f,.85f,.77f,.85f,.82f,.15f,.57f,.14f,.5f,.54f,.43f,.15f,.18f,.15f,.25f,.85f);
                    Path(vh,r,1,.25f,.72f,.77f,.73f);break;
                case Drawing.Boots:
                    Path(vh,r,1.6f,.14f,.81f,.43f,.8f,.43f,.39f,.53f,.27f,.52f,.15f,.08f,.15f,.09f,.34f,.16f,.42f,.14f,.81f);
                    Path(vh,r,1.6f,.59f,.81f,.85f,.8f,.84f,.42f,.95f,.29f,.94f,.15f,.59f,.15f);
                    Path(vh,r,1,.18f,.62f,.36f,.61f);Path(vh,r,1,.2f,.5f,.37f,.51f);break;
                case Drawing.Skill:
                    Path(vh,r,1.6f,.57f,.9f,.26f,.51f,.49f,.48f,.39f,.12f,.78f,.6f,.53f,.61f,.57f,.9f);break;
            }
            // Ground pencil marks, kept outside the object's silhouette.
            Path(vh,r,.65f,.12f,.07f,.4f,.055f,.7f,.07f,.88f,.055f);
        }

        void Path(VertexHelper vh, Rect r, float width, params float[] xy)
        {
            for(int i=2;i<xy.Length;i+=2)
            {
                Vector2 a=new Vector2(r.x+xy[i-2]*r.width,r.y+xy[i-1]*r.height);
                Vector2 b=new Vector2(r.x+xy[i]*r.width,r.y+xy[i+1]*r.height);
                Vector2 n=new Vector2(-(b-a).y,(b-a).x).normalized*width*.5f;
                int start=vh.currentVertCount;
                vh.AddVert(a-n,color,Vector2.zero);vh.AddVert(a+n,color,Vector2.zero);
                vh.AddVert(b+n,color,Vector2.zero);vh.AddVert(b-n,color,Vector2.zero);
                vh.AddTriangle(start,start+1,start+2);vh.AddTriangle(start,start+2,start+3);
            }
        }
    }
}
