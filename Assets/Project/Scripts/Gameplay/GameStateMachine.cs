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
        CpuTurn_Rolling,    // CPUのターン（サイコロ振り中）
        CpuTurn_Moving,     // CPUの駒が移動中
        CpuTurn_Resolving,  // CPUの着地イベント処理中
        End                  // 終了
    }

    [Header("参照")]
    public TokenMover myToken;           // 自分の駒
    public TokenMover cpuToken;        // CPUの駒
    public DiceUIController diceUI;   // サイコロUI
    public CameraFollow2D cameraFollow; // カメラ追従


    [Header("調整")]
    public float otherTurnDelay = 0.6f;  // 他ターンのダミー待ち
    public float resolveDelay   = 0.25f; // 着地後の演出/判定の待ち

    public GameState State { get; private set; } = GameState.MyTurn_AwaitInput;
    public event Action<GameState, GameState> OnStateChanged;

    void Start()
    {
        if (myToken == null) myToken = FindObjectOfType<TokenMover>();
        if (cpuToken == null){
            var cpuObj = Instantiate(myToken.gameObject, myToken.transform.parent);
            cpuToken = cpuObj.GetComponent<TokenMover>();
            cpuToken.isCPU = true;
            cpuToken.board = myToken.board; // 同じ盤面を使う
            cpuToken.name = "Cpu_Token";

            var renderer = cpuToken.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.3f, 0.3f, 0.3f); // 色を変えるなど区別
            cpuToken.tokenOffset = new Vector3(-0.5f, 1.5f, -3f); // 少しずらして配置
        }
        if (diceUI == null) diceUI = FindObjectOfType<DiceUIController>();
        if (cameraFollow == null) cameraFollow = FindObjectOfType<CameraFollow2D>();

        if (myToken != null) myToken.MoveCompleted += OnMyTokenMoveCompleted;
        if (cpuToken != null) cpuToken.MoveCompleted += OnCpuTokenMoveCompleted;
        // 最初の状態へ
        SetState(GameState.MyTurn_AwaitInput);
    }

    // DiceUI から呼ぶ：最終出目が決まったらここへ
    public bool OnDiceFinal(int steps)
    {
        switch (State)
        {
            case GameState.MyTurn_AwaitInput:
                if (!CanRoll()) return false;
                SetState(GameState.MyTurn_Moving);
                return myToken.MoveBy(steps); // TokenMover 側のコルーチンで移動
            case GameState.CpuTurn_Rolling:
                SetState(GameState.CpuTurn_Moving);
                return cpuToken.MoveBy(steps); // TokenMover 側のコルーチンで移動
            default:
                return false;
        }
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
        StartCoroutine(ResolveAndNext(myToken));
    }

    void OnCpuTokenMoveCompleted()
    {
        // ゴール到達は TokenMover が画面を出す仕様のままでOK。ここでは状態だけ End に。
        if (cpuToken.board != null && cpuToken.currentIndex >= cpuToken.board.Count - 1)
        {
            SetState(GameState.End);
            return;
        }
        StartCoroutine(ResolveAndNext(cpuToken));
    }

    IEnumerator ResolveAndNext(TokenMover token)
    {
        SetState(token.isCPU ? GameState.CpuTurn_Resolving : GameState.MyTurn_Resolving);
        yield return new WaitForSeconds(resolveDelay);

        // 現在のマスのイベント処理
        if (token.board != null)
        {
            yield return ExecuteTileEvent(token, token.board.path.events[(token.currentIndex% token.board.path.events.Count)]);
        }

        if (myToken.SkipTurn && cpuToken.SkipTurn){
            // 両者一回休みなら両方消費して次のターンへ
            myToken.SkipTurn = false;
            cpuToken.SkipTurn = false;
        }

        // ターン交代(元の構成的に一回休みの処理が簡潔にしづらく、人数を増やすと大変になるが今回はこれで)
        switch (State)
        {
            case GameState.MyTurn_Resolving:
                // プレイヤーが連続で動く場合の処理
                if (myToken.ExtraTurn || cpuToken.SkipTurn)
                {
                    // エクストラターンから消費
                    if(myToken.ExtraTurn) myToken.ExtraTurn = false;
                    // CPUの一回休みを消費
                    else if(cpuToken.SkipTurn) cpuToken.SkipTurn = false;
                    // 次のプレイヤーターンへ
                    SetState(GameState.MyTurn_AwaitInput);
                    yield break;
                }
                // 次のCPUターンへ
                StartCoroutine(CpuTurn());
                break;
            case GameState.CpuTurn_Resolving:
                // CPUが連続で動く場合の処理
                if (cpuToken.ExtraTurn || myToken.SkipTurn)
                {
                    // エクストラターンから消費
                    if(cpuToken.ExtraTurn) cpuToken.ExtraTurn = false;
                    // プレイヤーの一回休みを消費
                    else if(myToken.SkipTurn) myToken.SkipTurn = false;
                    // 次のCPUターンへ
                    StartCoroutine(CpuTurn());
                    yield break;
                }
                // 次のプレイヤーターンへ
                SetState(GameState.MyTurn_AwaitInput);
                break;
        }
    }

    IEnumerator CpuTurn()
    {
        yield return new WaitForSeconds(otherTurnDelay);
        SetState(GameState.CpuTurn_Rolling);
        yield return diceUI.RollRoutine();
    }

    void SetState(GameState next)
    {
        if (State == next) return;
        var prev = State;
        State = next;
        OnStateChanged?.Invoke(prev, next);
        // 必要ならここで UI の有効/無効を切り替える

        if (cameraFollow != null){
            switch (State)
            {
                case GameState.MyTurn_AwaitInput:
                case GameState.MyTurn_Moving:
                case GameState.MyTurn_Resolving:
                    cameraFollow.target = myToken.transform;
                    break;
                case GameState.CpuTurn_Rolling:
                case GameState.CpuTurn_Moving:
                case GameState.CpuTurn_Resolving:
                    cameraFollow.target = cpuToken.transform;
                    break;
            }
        }
    }
    

    IEnumerator ExecuteTileEvent(TokenMover token, TileEvent tile)
    {
        if (tile == null) yield break;
        
        switch (tile.eventType)
        {
            case TileEvent.EventType.None:
                // 何もしない
                break;
            case TileEvent.EventType.Forward:
                // 指定マス進む
                yield return token.MoveStepsEvent(tile.value);
                break;
            case TileEvent.EventType.Back:
                // 指定マス戻る
                yield return token.MoveStepsSignedEvent(-tile.value);
                break;
            case TileEvent.EventType.ExtraTurn:
                // もう一度サイコロを振れる
                token.ExtraTurn = true;
                break;
            case TileEvent.EventType.SkipNext:
                // 一回休み
                token.SkipTurn = true;
                break;
            case TileEvent.EventType.GoToStart:
                // スタートに戻る
                yield return token.MoveStepsSignedEvent(-token.currentIndex);
                break;
        }
    }
}
