using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sugoroku/BoardPath")]
public class BoardPath : ScriptableObject
{
    [Tooltip("タイル間隔（XYの距離）")]
    public float tileSpacing = 1.1f;

    [Tooltip("グリッド座標（x,y）。先頭=スタート、末尾=ゴール。")]
    public List<Vector2Int> coords = new List<Vector2Int>()
    {
        // まずは20マス直線（x正方向）。あとで自由に曲げてOK
        new Vector2Int(0,0), new Vector2Int(1,0), new Vector2Int(2,0), new Vector2Int(3,0), new Vector2Int(4,0),
        new Vector2Int(5,0), new Vector2Int(6,0), new Vector2Int(7,0), new Vector2Int(8,0), new Vector2Int(9,0),
        new Vector2Int(10,0), new Vector2Int(11,0), new Vector2Int(12,0), new Vector2Int(13,0), new Vector2Int(14,0),
        new Vector2Int(15,0), new Vector2Int(16,0), new Vector2Int(17,0), new Vector2Int(18,0), new Vector2Int(19,0)
    };
}
