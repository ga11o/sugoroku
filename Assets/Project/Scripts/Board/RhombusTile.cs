using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class RhombusTile : MonoBehaviour
{
    [Header("対角線の長さ（フル長）")]
    public float diagonalX = 1.0f; // 横の対角線（2:1なら 1.0）
    public float diagonalY = 0.5f; // 縦の対角線（2:1なら 0.5）

    [Header("見た目")]
    public Color color = new Color(0.7415f, 1.0f, 0.6549f); // 薄緑
    public string sortingLayerName = "Default";
    public int sortingOrder = 0;

    Mesh mesh;
    Material mat;

    void OnEnable()  => Build();
    void OnValidate() => Build();

    void Build()
    {
        var mf = GetComponent<MeshFilter>();
        var mr = GetComponent<MeshRenderer>();

        if (mat == null)
        {
            var shader = Shader.Find("Universal Render Pipeline/Unlit");
            mat = new Material(shader);
        }
        mat.color = color;
        mr.sharedMaterial = mat;
        mr.sortingLayerName = sortingLayerName;
        mr.sortingOrder = sortingOrder;

        float hx = diagonalX * 0.5f;
        float hy = diagonalY * 0.5f;

        if (mesh == null) mesh = new Mesh { name = "Rhombus" };
        mf.sharedMesh = mesh;

        Vector3[] v =
        {
            new Vector3(-hx, 0f, 0f),  // 左
            new Vector3( 0f, hy, 0f),  // 上
            new Vector3( hx, 0f, 0f),  // 右
            new Vector3( 0f,-hy, 0f),  // 下
        };
        int[] tris = { 0,1,2, 0,2,3 };
        Vector2[] uv  = { new Vector2(0,0.5f), new Vector2(0.5f,1), new Vector2(1,0.5f), new Vector2(0.5f,0) };

        mesh.Clear();
        mesh.vertices = v;
        mesh.triangles = tris;
        mesh.uv = uv;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public void ApplyColor(TileEvent tile)
    {
        Color col = this.color;
        if (tile != null)
        {
            switch (tile.eventType)
            {
                case TileEvent.EventType.None:      col = this.color;   break;
                case TileEvent.EventType.Forward:   col = new Color(0.4235f, 0.8078f, 1.0f);   break;
                case TileEvent.EventType.Back:      col = new Color(1.0f, 0.5686f, 0.5686f);   break;
                case TileEvent.EventType.ExtraTurn: col = new Color(1.0f, 0.8824f, 0.4196f);   break;
                case TileEvent.EventType.SkipNext:  col = new Color(0.784f, 0.784f, 0.784f);   break;
                case TileEvent.EventType.GoToStart: col = new Color(0.941f, 0.498f, 0.941f);   break;
                case TileEvent.EventType.Quiz:      col = new Color(1.0f, 0.647f, 0.0f);       break;
                default:                            col = this.color;   break;
            }
        }
        this.color = col;
        GetComponent<MeshRenderer>().material.color = col;
    }
}
