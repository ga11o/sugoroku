using System.Collections.Generic;
using UnityEngine;
using Sugoroku.Quiz;

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

    //[HideInInspector] public List<Transform> waypoints = new List<Transform>();
    [HideInInspector] public List<Waypoint> waypoints = new List<Waypoint>();


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
        if (!ErrorHandling()) return;

        var root = EnsureTilesRoot();

        // 既存タイル削除
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

        // グリッド走査
        int rows = path.grid.GetLength(0);
        int cols = path.grid.GetLength(1);

        // BoardBuilder.cs の盤面生成メソッド内
        Dictionary<(int,int), Waypoint> waypointGrid = new();

        for (int y = 0; y < rows; y++)
        {
            for (int x = 0; x < cols; x++)
            {
                int cell = path.grid[y, x];
                int eventCode = cell % 100;   // 下2桁
                if (cell == 0) continue; // 空白はスキップ

                Vector3 pos = new Vector3((x + y) * path.tileSpacing,(y - x) * path.tileSpacing * 0.5f,0f);

                GameObject prefab =
                    (eventCode == 1) ? tileStartPrefab :
                    (eventCode == 2) ? tileGoalPrefab :
                    tileNormalPrefab;

                var tile = Instantiate(prefab, pos, Quaternion.identity, root);
                TileEvent ev = null;

                switch (eventCode)
                {
                    case 10: ev = new TileEvent(TileEvent.EventType.Forward, 3); break;
                    case 11: ev = new TileEvent(TileEvent.EventType.Back, 1); break;
                    case 12: ev = new TileEvent(TileEvent.EventType.SkipNext); break;
                    case 13: ev = new TileEvent(TileEvent.EventType.ExtraTurn); break;
                    case 14: ev = new TileEvent(TileEvent.EventType.GoToStart); break;
                    case 15: ev = new TileEvent(TileEvent.EventType.Quiz); break; // クイズマス
                }

                // Waypoint を必ず作成
                var wp = new GameObject($"WP_{x}_{y}").AddComponent<Waypoint>();
                wp.tileEvent = ev; // ★ イベントを生成した後にセットする
                wp.transform.SetParent(tile.transform, false);

                // ゴールセルならフラグを立てる
                if (eventCode == 2)
                {
                    wp.isGoal = true;
                }

                // イベントマスなら色を変える
                if (ev != null)
                {
                    var rhombus = tile.GetComponent<RhombusTile>();
                    if (rhombus != null) rhombus.ApplyColor(ev);
                }

                // Waypointリストに追加（型を Waypoint に統一）
                waypoints.Add(wp);

                waypointGrid[(x,y)] = wp;
            }
        }


        // 接続
        foreach (var kv in waypointGrid)
        {
            int x = kv.Key.Item1;
            int y = kv.Key.Item2;
            var wp = kv.Value;

            int cell = path.grid[y, x];
            int eventCode = cell % 100;   // 下2桁
            int dirCode   = cell / 100;   // 上の桁

            switch (dirCode)
            {
                case 3: // 本道＝右
                    if (waypointGrid.TryGetValue((x+1,y), out var right))
                    {
                        wp.mainNext = right;
                        right.previous = wp;
                    }
                    break;

                case 4: // 本道＝左
                    if (waypointGrid.TryGetValue((x-1,y), out var left))
                    {
                        wp.mainNext = left;
                        left.previous = wp;
                    }
                    break;

                case 5: // 本道＝上
                    if (waypointGrid.TryGetValue((x,y-1), out var up))
                    {
                        wp.mainNext = up;
                        up.previous = wp;
                    }
                    break;

                case 6: // 本道＝下
                    if (waypointGrid.TryGetValue((x,y+1), out var down))
                    {
                        wp.mainNext = down;
                        down.previous = wp;
                    }
                    break;

            }

            // 分岐開始セルなら分岐候補を追加
            if (eventCode == 15)
            {
                if (waypointGrid.TryGetValue((x,y+1), out var branchDown)) wp.branchNexts.Add(branchDown);
                if (waypointGrid.TryGetValue((x-1,y), out var branchLeft)) wp.branchNexts.Add(branchLeft);
                if (waypointGrid.TryGetValue((x,y-1), out var branchUp))   wp.branchNexts.Add(branchUp);
                if (waypointGrid.TryGetValue((x+1,y), out var branchRight)) wp.branchNexts.Add(branchRight);
            }        
        }
    }

    public Vector3 GetPoint(int index)
    {
        index = Mathf.Clamp(index, 0, waypoints.Count - 1);
        return waypoints[index].transform.position;
    }

    // 例外処理 エラー検知
    private bool ErrorHandling()
    {
        try
        {
            bool ok = true;

            if (path == null || path.grid == null || path.grid.GetLength(0) == 0 || path.grid.GetLength(1) == 0)

            {
                Debug.LogWarning($"{nameof(BoardBuilder)}: Path が未設定または空です。生成を中止します。", this);
                ok = false;
            }
            if (tileNormalPrefab == null || tileStartPrefab == null || tileGoalPrefab == null)
            {
                Debug.LogWarning($"{nameof(BoardBuilder)}: タイルPrefabのいずれかが未設定です。生成を中止します。", this);
                ok = false;
            }
            var root = EnsureTilesRoot();
            if (root == null)
            {
                Debug.LogError($"{nameof(BoardBuilder)}: tilesRoot の用意に失敗しました。", this);
                ok = false;
            }
            
            return ok;
        }
        catch (System.Exception ex)
        {
            Debug.LogException(ex, this);
            return false;
        }
    }

    public int Count => waypoints.Count;
}
