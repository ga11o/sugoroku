using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;



public class GameStateMachine : MonoBehaviour
{
    public enum GameState
    {
        MyTurn_AwaitInput,   // 自分のターン（入力待ち）
        MyTurn_Moving,       // 自分の駒が移動中
        MyTurn_Resolving,    // 着地イベント処理中
        OtherTurn,           // 他プレイヤーのターン（将来拡張）
        End                  // 終了
    }

    [Header("参照")]
    public TokenMover myToken;           // 自分の駒（今は1人想定）
    [Tooltip("将来AIや他プレイヤーの駒を使うならここに追加する")]
    public TokenMover otherToken;        // 任意/今は未使用


    [Header("調整")]
    public float otherTurnDelay = 0.6f;  // 他ターンのダミー待ち
    public float resolveDelay   = 0.25f; // 着地後の演出/判定の待ち
    private bool extraTurnPending = false; // ★ 追加：もう一度サイコロを振るフラグ
    private bool skipNextPending = false; // ★ 追加：一回休みフラグ

    public GameState State { get; private set; } = GameState.MyTurn_AwaitInput;
    public event Action<GameState, GameState> OnStateChanged;

    [Header("Quiz trigger (optional)")]
    [Tooltip("If enabled, QuizManager will be asked to show a quiz when entering the selected GameState")]
    public bool showQuizOnState = false;
    public GameState quizTriggerState = GameState.MyTurn_Resolving;
    [Tooltip("If true, timeScale will be set to 0 while the quiz is active (and restored after)")]
    public bool pauseDuringQuiz = true;

    void Start()
    {
        if (myToken == null) myToken = FindObjectOfType<TokenMover>();
        if (myToken != null) myToken.MoveCompleted += OnMyTokenMoveCompleted;
        // 最初の状態へ
        SetState(GameState.MyTurn_AwaitInput);
    }

    // DiceUI から呼ぶ：最終出目が決まったらここへ
    public bool OnDiceFinal(int steps)
    {
        if (!CanRoll()) return false;
        SetState(GameState.MyTurn_Moving);
        return myToken.MoveBy(steps); // TokenMover 側のコルーチンで移動
    }

    // DiceUI がボタンを押して良いかどうかの判定に使う
    public bool CanRoll()
    {
        return myToken != null
            && State == GameState.MyTurn_AwaitInput
            && !myToken.isMoving;
    }

    void OnMyTokenMoveCompleted()
    {
        // ゴール到達は TokenMover が画面を出す仕様のままでOK。ここでは状態だけ End に。
        if (myToken.currentWaypoint != null &&
        myToken.currentWaypoint.transform.parent.name.Contains("Goal"))
        {
            SetState(GameState.End);
            return;
        }
        StartCoroutine(ResolveAndNext());
    }

    IEnumerator ResolveAndNext()
    {
        SetState(GameState.MyTurn_Resolving);
        yield return new WaitForSeconds(resolveDelay);

        // 現在のマスのイベント処理
        if (myToken != null && myToken.board != null)
        {
            // 現在位置の Waypoint を取得
            var wp = myToken.currentWaypoint;
            if (wp != null && wp.tileEvent != null)
            {
                //Debug.Log("イベント呼び出し: " + wp.tileEvent.eventType);
                yield return ExecuteTileEvent(wp.tileEvent);
            }

            if (extraTurnPending)
            {
                extraTurnPending = false; // フラグをリセット
                SetState(GameState.MyTurn_AwaitInput); // もう一度自分のターンへ
                yield break;
            }
        }

      


        // 今はダミーとして「相手のターン」を少しだけ挟む
        SetState(GameState.OtherTurn);
        yield return new WaitForSeconds(otherTurnDelay);

        if(skipNextPending)
        {
            skipNextPending = false; // フラグをリセット
            SetState(GameState.OtherTurn);
            yield return new WaitForSeconds(otherTurnDelay);
        }

        // 自分のターンに戻す
        SetState(GameState.MyTurn_AwaitInput);
    }

    void SetState(GameState next)
    {
        if (State == next) return;
        var prev = State;
        State = next;
        OnStateChanged?.Invoke(prev, next);
        // Quiz trigger hook: if configured, show quiz when entering the selected state
        if (showQuizOnState && next == quizTriggerState)
        {
            StartCoroutine(ShowQuizOnState());
        }
        // 必要ならここで UI の有効/無効を切り替える
    }

    IEnumerator ShowQuizOnState()
    {
        if (QuizManager.Instance == null)
        {
            var go = new GameObject("QuizManager");
            go.AddComponent<QuizManager>();
            // wait a frame to ensure Awake ran
            yield return null;
        }

        bool answered = false;
        if (pauseDuringQuiz) Time.timeScale = 0f;

        yield return QuizManager.Instance.AskRandomQuestionRoutine((ok) => { answered = ok; });

        if (pauseDuringQuiz) Time.timeScale = 1f;

        // Optionally, you can react to the result here (answered==true means correct)
        // e.g. grant a bonus move, set flags, etc. For now we just return.
        yield break;
    }
    

    public IEnumerator ExecuteTileEvent(TileEvent tile)
    {
        if (tile == null) yield break;
        
        switch (tile.eventType)
        {
            case TileEvent.EventType.None:
                // 何もしない
                break;
            case TileEvent.EventType.Forward:
                // 指定マス進む
                yield return myToken.MoveStepsEvent(tile.value);
                break;
            case TileEvent.EventType.Back:
                yield return myToken.MoveStepsSignedEvent(-tile.value);
                break;
            case TileEvent.EventType.ExtraTurn:
                // もう一度サイコロを振れる
                extraTurnPending = true;
                break;
            case TileEvent.EventType.SkipNext:
                // 一回休み
                skipNextPending = true;
                break;
            case TileEvent.EventType.GoToStart:
                // スタートに戻る
                yield return myToken.GoToStart();
                break;
        }
    }
}
