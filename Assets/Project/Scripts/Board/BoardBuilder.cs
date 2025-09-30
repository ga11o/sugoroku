using System.Collections.Generic;
using UnityEngine;

public class BoardBuilder : MonoBehaviour
{
    [Header("Path / Prefabs")]
    public BoardPath path;
    public GameObject tileNormalPrefab;
    public GameObject tileStartPrefab;
    public GameObject tileGoalPrefab;

    [Header("見た目・配置")]
    [Tooltip("Prefab側でZ=45°など回しているなら false を推奨")]
    public bool rotateDiamond = false;   // 2Dスプライト利用時の回転（RhombusTileなら通常OFF）
    [Tooltip("横方向の中心間距離")]
    public float spacingX = 1.0f;        // ★ 2:1菱形(対角X=1.0,Y=0.5)なら 1.0 がジャスト
    [Tooltip("縦方向の中心間距離")]
    public float spacingY = 0.5f;        // ★ 同上 0.5 がジャスト
    [Tooltip("RhombusTile の diagonal 値から自動で間隔を合わせる")]
    public bool autoSpacing = false;

    [Header("生成先(安全)")]
    [SerializeField] Transform tilesRoot; // タイルだけ入れる親。Boardの他の子(カメラ等)は触らない

    [HideInInspector] public List<Transform> waypoints = new List<Transform>();

    void Awake()
    {
        if (Application.isPlaying) Build();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Editor上で値を変えたら即反映したいときに使う
        if (!Application.isPlaying)
        {
            if (autoSpacing && tileNormalPrefab != null)
            {
                var rh = tileNormalPrefab.GetComponent<RhombusTile>();
                if (rh != null)
                {
                    spacingX = rh.diagonalX;
                    spacingY = rh.diagonalY;
                }
            }
        }
    }

    [ContextMenu("Rebuild (Editor)")]
    void RebuildEditor()
    {
        if (Application.isPlaying) return;
        BuildInternal(destroyImmediate:true);
    }
#endif

    public void Build()
    {
        if (!Application.isPlaying) return;
        BuildInternal(destroyImmediate:false);
    }

    Transform EnsureTilesRoot()
    {
        if (tilesRoot == null)
        {
            var t = transform.Find("_Tiles");
            if (t == null)
            {
                var go = new GameObject("_Tiles");
                go.transform.SetParent(transform, false);
                tilesRoot = go.transform;
            }
            else tilesRoot = t;
        }
        return tilesRoot;
    }

    void BuildInternal(bool destroyImmediate)
    {
        if (path == null || path.coords == null || path.coords.Count == 0) return;
        if (tileNormalPrefab == null || tileStartPrefab == null || tileGoalPrefab == null) return;

        var root = EnsureTilesRoot();

        // 既存タイルだけをクリア（Board の他の子は触らない）
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            var child = root.GetChild(i).gameObject;
#if UNITY_EDITOR
            if (destroyImmediate) UnityEditor.Undo.DestroyObjectImmediate(child);
            else Destroy(child);
#else
            Destroy(child);
#endif
        }
        waypoints.Clear();

        // 生成
        for (int i = 0; i < path.coords.Count; i++)
        {
            var g = path.coords[i];
            // 置き換え：常に 0.5 を使う
            Vector3 pos = new Vector3(g.x * 5f + g.y * 5f, g.y * 2.5f - g.x * 2.5f, 0f);


            GameObject prefab =
                (i == 0) ? tileStartPrefab :
                (i == path.coords.Count - 1) ? tileGoalPrefab :
                tileNormalPrefab;

            var tile = Instantiate(prefab, pos, Quaternion.identity, root);

            if (rotateDiamond)
            {
                var e = tile.transform.eulerAngles;
                e.z = 45f;
                tile.transform.eulerAngles = e;
            }

            var wp = new GameObject($"WP_{i}").transform;
            wp.SetParent(tile.transform, false);
            waypoints.Add(wp);
        }
    }

    public Vector3 GetPoint(int index)
    {
        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index].position;
    }

    public int Count => waypoints.Count;
}
