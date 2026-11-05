using UnityEngine;
using UnityEngine.UI;

/// <summary>Fine stable focus corners, connector and small reading ornaments over the painted tutorial skin.</summary>
public sealed class TutorialForestGraphic : MapMerchantFrameGraphic
{
    public enum TutorialStyle { Focus, Leader, Compass, Divider, Footprint, Die, Growth }
    public TutorialStyle tutorialStyle;
    public float glow;
    public Vector2[] points;
    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear(); Rect r=GetPixelAdjustedRect(); Color gold=new Color(1,.83f,.36f,.85f+glow*.15f);
        if (tutorialStyle==TutorialStyle.Focus)
        {
            Band(vh,Inset(r,.5f),3,1.4f,gold); Corners(vh,Inset(r,1),14,gold);
            Band(vh,Inset(r,-2),4,2,new Color(1,.77f,.21f,.08f+glow*.08f)); return;
        }
        if (tutorialStyle==TutorialStyle.Leader)
        {
            if (points==null || points.Length<2) return;
            for(int i=1;i<points.Length;i++) Stroke(vh,points[i-1],points[i],1.5f,gold);
            Disc(vh,points[points.Length-1],3.5f,gold); return;
        }
        if (tutorialStyle==TutorialStyle.Divider)
        {
            Stroke(vh,new Vector2(r.xMin,r.center.y),new Vector2(r.xMax,r.center.y),1,new Color(.77f,.62f,.29f,.8f));
            Diamond(vh,r.center,4.5f,gold); Diamond(vh,r.center,2,new Color(.02f,.12f,.07f)); return;
        }
        Vector2 c=r.center;float radius=Mathf.Min(r.width,r.height)*.37f;
        if(tutorialStyle==TutorialStyle.Compass)
        {
            for(int i=0;i<48;i++){float a=i*Mathf.PI/24,b=(i+1)*Mathf.PI/24;Stroke(vh,c+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius,c+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius,1.7f,gold);}
            Stroke(vh,c+Vector2.up*radius*1.24f,c-Vector2.up*radius*1.1f,1.5f,gold);
            Stroke(vh,c+Vector2.right*radius*1.1f,c-Vector2.right*radius*1.1f,1.5f,gold);
            Diamond(vh,c,radius*.55f,gold); Diamond(vh,c,3,new Color(.03f,.16f,.09f));return;
        }
        if(tutorialStyle==TutorialStyle.Footprint)
        {
            Disc(vh,c+new Vector2(-radius*.3f,radius*.25f),radius*.32f,gold);
            Disc(vh,c+new Vector2(radius*.4f,-radius*.18f),radius*.32f,gold);
            Stroke(vh,c+new Vector2(-radius*.35f,-radius*.2f),c+new Vector2(-radius*.5f,-radius*.7f),radius*.4f,gold);
            Stroke(vh,c+new Vector2(radius*.4f,-radius*.5f),c+new Vector2(radius*.3f,-radius*.9f),radius*.4f,gold);return;
        }
        if(tutorialStyle==TutorialStyle.Die)
        {
            Band(vh,new Rect(c-Vector2.one*radius,Vector2.one*radius*2),4,2,gold);
            foreach(Vector2 p in new[]{new Vector2(-.5f,-.5f),new Vector2(.5f,.5f),Vector2.zero,new Vector2(-.5f,.5f),new Vector2(.5f,-.5f)})Disc(vh,c+p*radius,2.5f,gold);return;
        }
        Stroke(vh,c-Vector2.up*radius,c+Vector2.up*radius,2,gold);
        Stroke(vh,c,c+new Vector2(-radius*.8f,radius*.7f),radius*.35f,gold);
        Stroke(vh,c+Vector2.up*radius*.3f,c+new Vector2(radius*.7f,radius*.9f),radius*.35f,gold);
        Disc(vh,c+new Vector2(radius*.6f,-radius*.65f),radius*.3f,gold);
    }
}
