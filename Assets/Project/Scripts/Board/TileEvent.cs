using UnityEngine;
using System.Collections;

[System.Serializable]
public class TileEvent
{
    public enum EventType
    {
        None,       // イベントなし
        Forward,    // 指定マス進む
        Back,       // 指定マス戻る
        ExtraTurn,  // もう一度サイコロを振れる
        SkipNext,   // 一回休み
        GoToStart,   // スタートに戻る
        Quiz        // クイズイベント（正解で1マス進む）
    }

    public EventType eventType = EventType.None;
    public int value; // 進む/戻るマス数などの値

    // コンストラクタ
    public TileEvent(EventType type) {
        this.eventType = type;
        this.value = 0;
    }
    // コンストラクタ（値付き）
    public TileEvent(EventType type, int val) {
        this.eventType = type;
        this.value = val;
    }
}