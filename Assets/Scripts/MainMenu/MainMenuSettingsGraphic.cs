using UnityEngine;
using UnityEngine.UI;

/// <summary>Native brass and jade controls, using the forest UI's existing grain and bevels.</summary>
public sealed class MainMenuSettingsGraphic : MapMerchantFrameGraphic
{
    public enum Part { Title, Section, Track, Fill, Grip, Switch, Knob, Field, Chevron, Speaker, Display, Check }
    public Part part;
    private static readonly Color Gold = new Color32(219, 182, 102, 255);
    private static readonly Color Brass = new Color32(136, 106, 53, 255);
    private static readonly Color Light = new Color32(255, 224, 153, 255);

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect r = GetPixelAdjustedRect();
        if (r.width <= 0 || r.height <= 0) return;
        switch (part)
        {
            case Part.Title:
                Surface(vh,r,22,new Color32(66,45,24,255),new Color32(30,23,14,255));
                Band(vh,Inset(r,1),21,4,Light,true);
                Band(vh,Inset(r,8),16,1.5f,Brass,true);
                Gem(vh,new Vector2(r.xMin+29,r.center.y),8);
                Gem(vh,new Vector2(r.xMax-29,r.center.y),8);
                break;
            case Part.Section:
                Surface(vh,r,10,new Color32(23,43,30,245),new Color32(8,24,17,245));
                Band(vh,Inset(r,1),9,1.1f,new Color32(124,115,62,155),true);
                CornerCaps(vh,Inset(r,7),12,new Color32(169,144,75,155));
                break;
            case Part.Track:
                Surface(vh,r,5,new Color32(7,17,12,255),new Color32(28,43,30,255));
                Band(vh,Inset(r,1),4,1.4f,Brass,true);
                break;
            case Part.Fill:
                Surface(vh,r,2,new Color32(174,202,120,255),new Color32(62,128,82,255));
                Stroke(vh,new Vector2(r.xMin,r.yMax-1),new Vector2(r.xMax,r.yMax-1),1,new Color32(225,224,150,220));
                break;
            case Part.Grip:
                Diamond(vh,r.center,Mathf.Min(r.width,r.height)*.46f,new Color32(62,43,19,255));
                Diamond(vh,r.center,Mathf.Min(r.width,r.height)*.40f,Light);
                Diamond(vh,r.center,Mathf.Min(r.width,r.height)*.30f,Brass);
                Gem(vh,r.center,Mathf.Min(r.width,r.height)*.17f);
                break;
            case Part.Switch:
                Surface(vh,r,12,selected?new Color32(46,100,72,255):new Color32(30,42,34,255),new Color32(10,31,22,255));
                Band(vh,Inset(r,1),11,1.7f,selected?Gold:Brass,true);
                Band(vh,Inset(r,5),7,.8f,new Color32(91,113,70,255));
                break;
            case Part.Knob:
                Surface(vh,r,8,new Color32(244,218,157,255),new Color32(159,120,58,255));
                Band(vh,Inset(r,1),7,1.6f,Light,true);
                for(int i=-1;i<=1;i++) Stroke(vh,r.center+new Vector2(i*5,-6),r.center+new Vector2(i*5,6),1,new Color32(106,78,34,180));
                break;
            case Part.Field:
                Surface(vh,r,8,new Color32(26,43,31,255),new Color32(9,25,17,255));
                Band(vh,Inset(r,1),7,1.6f,Brass,true);
                Band(vh,Inset(r,5),4,.8f,new Color32(77,92,53,255));
                break;
            case Part.Chevron:
                Stroke(vh,r.center+new Vector2(-7,3),r.center+new Vector2(0,-4),2.6f,Gold);
                Stroke(vh,r.center+new Vector2(0,-4),r.center+new Vector2(7,3),2.6f,Gold);
                break;
            case Part.Speaker:
                Surface(vh,new Rect(r.center.x-12,r.center.y-5,7,10),1,Gold,Brass);
                Triangle(vh,new Vector2(r.center.x-6,r.center.y-5),r.center+new Vector2(3,-12),r.center+new Vector2(3,12),Gold);
                for(int i=0;i<8;i++)
                {
                    float a=(-.8f+i*.2f),b=a+.2f;
                    Stroke(vh,r.center+new Vector2(2+Mathf.Cos(a)*15,Mathf.Sin(a)*15),r.center+new Vector2(2+Mathf.Cos(b)*15,Mathf.Sin(b)*15),1.8f,Gold);
                }
                break;
            case Part.Display:
                Band(vh,new Rect(r.center.x-15,r.center.y-7,30,22),3,2,Gold,true);
                Stroke(vh,r.center+new Vector2(0,-7),r.center+new Vector2(0,-14),2,Gold);
                Stroke(vh,r.center+new Vector2(-9,-14),r.center+new Vector2(9,-14),2,Gold);
                break;
            case Part.Check: Gem(vh,r.center,7); break;
        }
    }

    private void Gem(VertexHelper vh,Vector2 p,float radius)
    {
        Diamond(vh,p,radius+2,Gold); Diamond(vh,p,radius,new Color32(29,63,46,255));
        Diamond(vh,p,radius-2,new Color32(87,180,137,255));
    }

    private void Triangle(VertexHelper vh,Vector2 a,Vector2 b,Vector2 c,Color tint)
    {
        int start=vh.currentVertCount; Vertex(vh,a,tint); Vertex(vh,b,tint); Vertex(vh,c,tint); vh.AddTriangle(start,start+1,start+2);
    }
}
