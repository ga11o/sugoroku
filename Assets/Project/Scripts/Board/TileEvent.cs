using UnityEngine;
using System.Collections;
using Sugoroku.Atoms;
using Sugoroku.Quiz;

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
        GoToStart,  // スタートに戻る
        Quiz        // クイズ（正解で原子カード獲得 / 不正解でランダムに1枚失う）
    }

    [Header("共通")]
    public EventType eventType = EventType.None;

    [Tooltip("進む/戻るマス数などの値（Quiz では未使用）")]
    public int value; // 進む/戻るマス数などの値

    [Header("Quiz 設定（アセット優先）")]
    [Tooltip("このマス専用のクイズアセット（設定されていればこちらが優先される）")]
    public QuizQuestionDef quizAsset;

    [Header("Quiz 設定（個別カスタム用・アセット未設定時に使用）")]
    [TextArea]
    public string quizQuestion;              // 問題文
    [Tooltip("4択の選択肢（要素数4を推奨）")]
    public string[] quizChoices = new string[4]; // 選択肢
    [Tooltip("正解となる選択肢のインデックス（0〜3）")]
    [Range(0, 3)]
    public int quizCorrectIndex = 0;         // 正解インデックス
    [Tooltip("正解したときに獲得できる原子カード")]
    public AtomCardDef quizRewardCard;       // 報酬の原子カード

    // コンストラクタ
    public TileEvent(EventType type)
    {
        this.eventType = type;
        this.value = 0;
    }
    // コンストラクタ（値付き）
    public TileEvent(EventType type, int val)
    {
        this.eventType = type;
        this.value = val;
    }
}
