using System;
using System.Collections;
using UnityEngine;

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

    public GameState State { get; private set; } = GameState.MyTurn_AwaitInput;
    public event Action<GameState, GameState> OnStateChanged;

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
        if (myToken.board != null && myToken.currentIndex >= myToken.board.Count - 1)
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

        // ここでイベントマス処理などを行う（将来）
        // …

        // 今はダミーとして「相手のターン」を少しだけ挟む
        SetState(GameState.OtherTurn);
        yield return new WaitForSeconds(otherTurnDelay);

        // 自分のターンに戻す
        SetState(GameState.MyTurn_AwaitInput);
    }

    void SetState(GameState next)
    {
        if (State == next) return;
        var prev = State;
        State = next;
        OnStateChanged?.Invoke(prev, next);
        // 必要ならここで UI の有効/無効を切り替える
    }
}
