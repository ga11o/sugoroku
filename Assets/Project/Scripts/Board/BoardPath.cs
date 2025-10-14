using System.Collections.Generic;
using UnityEngine;


[CreateAssetMenu(menuName = "Sugoroku/BoardPath")]
public class BoardPath : ScriptableObject
{
    [Tooltip("タイル間隔（XYの距離）")]
    public float tileSpacing = .1f;

    [Tooltip("グリッド座標（x,y）。先頭=スタート、末尾=ゴール。")]
    public List<Vector2Int> coords = new List<Vector2Int>()
    {

        new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4), new Vector2Int(0,5),
        new Vector2Int(1,5), new Vector2Int(2,5),
        new Vector2Int(2,4), new Vector2Int(2,3), new Vector2Int(2,2), new Vector2Int(2,1), new Vector2Int(2,0),
        new Vector2Int(3,0), new Vector2Int(4,0), new Vector2Int(5,0), new Vector2Int(6,0),
        new Vector2Int(6,1), new Vector2Int(6,2),
        new Vector2Int(5,2), new Vector2Int(4,2), 
        new Vector2Int(4,3), new Vector2Int(4,4), new Vector2Int(4,5),
        new Vector2Int(5,5), new Vector2Int(5,5)
    };

    /*public List<Vector2Int> coords;  //実行ごとに初期化する場合はこっち 初期化しないとコード内での変更が更新されないです

    void OnEnable()
    {
        //public List<Vector2Int> 
        coords = new List<Vector2Int>()
        {
            new Vector2Int(0,0), new Vector2Int(0,1), new Vector2Int(0,2), new Vector2Int(0,3), new Vector2Int(0,4), new Vector2Int(0,5),
            new Vector2Int(1,5), new Vector2Int(2,5),
            new Vector2Int(2,4), new Vector2Int(2,3), new Vector2Int(2,2), new Vector2Int(2,1), new Vector2Int(2,0),
            new Vector2Int(3,0), new Vector2Int(4,0), new Vector2Int(5,0), new Vector2Int(6,0),
            new Vector2Int(6,1), new Vector2Int(6,2),
            new Vector2Int(5,2), new Vector2Int(4,2), 
            new Vector2Int(4,3), new Vector2Int(4,4), new Vector2Int(4,5),
            new Vector2Int(5,5), new Vector2Int(5,5),
        };
    }*/

}
