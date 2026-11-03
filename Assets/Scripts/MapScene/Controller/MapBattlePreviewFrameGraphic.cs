using UnityEngine;
using UnityEngine.UI;

/// <summary>Scalable heraldic panel and oxblood button; all decoration stays inside the UI bounds.</summary>
[RequireComponent(typeof(CanvasRenderer))]
public sealed class MapBattlePreviewFrameGraphic : MaskableGraphic
{
    public enum Style { Panel, Button, Return }
    public Style style;

    protected override void OnPopulateMesh(VertexHelper vh)
    {
        vh.Clear();
        Rect rect = GetPixelAdjustedRect();
        if (rect.width <= 0 || rect.height <= 0) return;
        if (style == Style.Return)
        {
            Color ivory = new Color(0.93f, 0.87f, 0.68f);
            Vector2 c = new Vector2(rect.xMin + 25, rect.center.y);
            Stroke(vh, c + new Vector2(-4, 7), c + new Vector2(8, 7), 2.5f, ivory);
            Stroke(vh, c + new Vector2(8, 7), c + new Vector2(12, 2), 2.5f, ivory);
            Stroke(vh, c + new Vector2(12, 2), c + new Vector2(10, -4), 2.5f, ivory);
            Stroke(vh, c + new Vector2(10, -4), c + new Vector2(4, -6), 2.5f, ivory);
            Stroke(vh, c + new Vector2(-4, 7), c + new Vector2(2, 13), 2.5f, ivory);
            Stroke(vh, c + new Vector2(-4, 7), c + new Vector2(2, 1), 2.5f, ivory);
            Stroke(vh, new Vector2(rect.xMin + 49, rect.yMin + 3), new Vector2(rect.xMax - 26, rect.yMin + 3), 0.8f, new Color(0.57f, 0.46f, 0.26f));
            return;
        }
        Vector2[] path = style == Style.Panel ? PanelPath(rect) : ButtonPath(rect);
        Fill(vh, path, style == Style.Panel ? new Color(0.025f, 0.065f, 0.043f) : new Color(0.30f, 0.055f, 0.032f));
        Loop(vh, path, 9f, new Color(0.10f, 0.07f, 0.025f));
        Loop(vh, path, 5f, new Color(0.73f, 0.51f, 0.22f));
        Loop(vh, Inset(path, rect.center, 0.991f), 1.6f, new Color(1f, 0.84f, 0.48f));
        Loop(vh, Inset(path, rect.center, 0.970f), 1.1f, new Color(0.23f, 0.19f, 0.085f));
        Loop(vh, Inset(path, rect.center, 0.955f), 0.75f, new Color(0.68f, 0.54f, 0.29f, 0.85f));
        if (style == Style.Panel)
        {
            for (int i = 0; i < 100; i++)
            {
                float x = rect.xMin + rect.width * (0.10f + Mathf.Repeat(Mathf.Sin((i+1)*127.1f)*43758.5453f, 1f) * 0.80f);
                float y = rect.yMin + rect.height * (0.13f + Mathf.Repeat(Mathf.Sin((i+1)*311.7f)*19631.2f, 1f) * 0.70f);
                Stroke(vh, new Vector2(x, y), new Vector2(x + 5 + i % 9, y + 2 + i % 4), 0.7f, new Color(0.39f, 0.44f, 0.23f, 0.018f));
            }
            Vector2 tip = new Vector2(rect.center.x, rect.yMin + 20);
            Vector2[] gem = { tip + Vector2.up * 13, tip + Vector2.right * 8, tip + Vector2.down * 10, tip + Vector2.left * 8 };
            Fill(vh, gem, new Color(0.78f, 0.54f, 0.20f));
            Loop(vh, gem, 1.3f, new Color(1f, 0.85f, 0.50f));
        }
    }

    private static Vector2[] PanelPath(Rect r)
    {
        Vector2[] p = {
            new Vector2(.018f,.80f), new Vector2(.018f,.18f), new Vector2(.07f,.14f), new Vector2(.10f,.075f),
            new Vector2(.20f,.065f), new Vector2(.29f,.045f), new Vector2(.39f,.027f), new Vector2(.50f,.020f),
            new Vector2(.61f,.027f), new Vector2(.71f,.045f), new Vector2(.80f,.075f), new Vector2(.90f,.075f),
            new Vector2(.93f,.13f), new Vector2(.982f,.18f), new Vector2(.982f,.80f), new Vector2(.94f,.835f),
            new Vector2(.90f,.90f), new Vector2(.78f,.917f), new Vector2(.67f,.94f), new Vector2(.57f,.967f),
            new Vector2(.50f,.994f), new Vector2(.43f,.967f), new Vector2(.33f,.94f), new Vector2(.22f,.917f), new Vector2(.10f,.90f), new Vector2(.06f,.835f)
        };
        for (int i = 0; i < p.Length; i++) p[i] = new Vector2(r.xMin + p[i].x * r.width, r.yMax - p[i].y * r.height);
        return p;
    }

    private static Vector2[] ButtonPath(Rect r)
    {
        float c = Mathf.Min(13f, r.height * .18f);
        return new[] { new Vector2(r.xMin+c,r.yMax-2),new Vector2(r.xMax-c,r.yMax-2),new Vector2(r.xMax-2,r.yMax-c),
            new Vector2(r.xMax-2,r.yMin+c),new Vector2(r.xMax-c,r.yMin+2),new Vector2(r.xMin+c,r.yMin+2),new Vector2(r.xMin+2,r.yMin+c),new Vector2(r.xMin+2,r.yMax-c) };
    }

    private static Vector2[] Inset(Vector2[] path, Vector2 centre, float scale)
    {
        Vector2[] result = new Vector2[path.Length];
        for (int i=0;i<path.Length;i++) result[i]=Vector2.Lerp(centre,path[i],scale);
        return result;
    }

    private static void Fill(VertexHelper vh, Vector2[] path, Color colour)
    {
        Vector2 centre = Vector2.zero;
        foreach (Vector2 p in path) centre += p;
        centre /= path.Length;
        int first=vh.currentVertCount;
        vh.AddVert(centre,colour,Vector2.zero);
        foreach(Vector2 p in path) vh.AddVert(p,colour,Vector2.zero);
        for(int i=0;i<path.Length;i++) vh.AddTriangle(first,first+1+i,first+1+(i+1)%path.Length);
    }

    private static void Loop(VertexHelper vh, Vector2[] path, float width, Color colour)
    { for(int i=0;i<path.Length;i++) Stroke(vh,path[i],path[(i+1)%path.Length],width,colour); }

    private static void Stroke(VertexHelper vh, Vector2 from, Vector2 to, float width, Color colour)
    {
        Vector2 delta=to-from;
        if(delta.sqrMagnitude<.0001f)return;
        Vector2 n=new Vector2(-delta.y,delta.x).normalized*width*.5f;
        int i=vh.currentVertCount;
        vh.AddVert(from-n,colour,Vector2.zero);vh.AddVert(from+n,colour,Vector2.zero);
        vh.AddVert(to+n,colour,Vector2.zero);vh.AddVert(to-n,colour,Vector2.zero);
        vh.AddTriangle(i,i+1,i+2);vh.AddTriangle(i,i+2,i+3);
    }
}
