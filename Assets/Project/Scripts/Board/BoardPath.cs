using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sugoroku/BoardGrid")]
public class BoardPath : ScriptableObject
{
    [Tooltip("タイル間隔（XYの距離）")]
    public float tileSpacing = 5f;

    [Tooltip("0=空白, 1=通常, 2=スタート, 3=ゴール, 4=イベントなど")]
    public int[,] grid = new int[,] {
        {2,10,11,4,1,1,1},   // 2=スタート
        {1,1,1,10,14,12,11},
        {1,1,1,1,1,13,1},
        {1,1,14,13,1,3,0},   // 3=ゴール
    };

    /*[Tooltip("各マスのイベント情報（必要なら拡張）")]
    public List<TileEvent> events = new List<TileEvent>()
    {
        new TileEvent(TileEvent.EventType.None),
        new TileEvent(TileEvent.EventType.Forward, 3),
        new TileEvent(TileEvent.EventType.None),
        /*new TileEvent(TileEvent.EventType.SkipNext),
        new TileEvent(TileEvent.EventType.Back, 1),
        new TileEvent(TileEvent.EventType.None),
        new TileEvent(TileEvent.EventType.None),
        new TileEvent(TileEvent.EventType.Forward, 3),
        new TileEvent(TileEvent.EventType.None),
        new TileEvent(TileEvent.EventType.ExtraTurn),
        new TileEvent(TileEvent.EventType.None),
        new TileEvent(TileEvent.EventType.GoToStart),
    };*/
}
