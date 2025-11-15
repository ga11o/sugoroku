using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Sugoroku.Atoms;


public class GameStateMachine : MonoBehaviour
{
    public enum GameState
    {
        Turn_AwaitInput,   // 入力待ち
        Turn_Moving,       // 駒が移動中
        Turn_Resolving,    // 着地イベント処理中
        End                // 終了
    }


    [Header("参照")]
    public List<TokenMover> players = new List<TokenMover>(); // プレイヤーの駒リスト
    private int playerCount = 4; // プレイヤー数
    public int currentTurn = 0; // 現在のターン数(正確にはゲーム開始から何人が動いたか)
    public List<int> playerOrder = new List<int> {0, 3, 1, 2}; // プレイヤーの順番リスト(拡張用)
    public int currentPlayerIndex => playerOrder[currentTurn % players.Count]; // 現在のプレイヤー番号
    public TokenMover CurrentPlayer => players[currentPlayerIndex]; // 現在のプレイヤー駒
    public DiceUIController diceUI; // DiceUI への参照
    public CameraFollow2D cameraFollow; // カメラ制御への参照
    public HandUI HandUI; // 手札UI への参照
    public MoleculeHandUI MolHandUI ; // 分子手札UI への参照


    [Header("調整")]
    public float turnDelay = 0.6f;  // ターン終了時の余韻
    public float resolveDelay   = 0.25f; // 着地後の演出/判定の待ち

    public GameState State { get; private set; } = GameState.Turn_AwaitInput;
    public event Action<GameState, GameState> OnStateChanged;

    void Start()
    {
        players.Clear();
        // 一旦人間4人作る
        if (players.Count == 0){
            players.Add(FindObjectOfType<TokenMover>());
            players[0].MoveCompleted += OnMoveCompleted;
            players[0].ordinalPlayerNumber = 0;
            players[0].playerName = "Player1";

            players[0].atomHand = FindObjectOfType<PlayerHand>();
            players[0].atomHand.transform.SetParent(players[0].transform);
            players[0].molHand = FindObjectOfType<PlayerMoleculeHand>();
            players[0].molHand.transform.SetParent(players[0].transform);
        }
        for (int i = 1; i < playerCount; i++){
            var Obj = Instantiate(players[0].gameObject, players[0].transform.parent);
            TokenMover Token = Obj.GetComponent<TokenMover>();
            Token.board = players[0].board; // 同じ盤面を使う
            Token.playerName = "Player" + (i + 1);
            Token.ordinalPlayerNumber = i;

            Token.atomHand.name = Token.playerName + "_AtomHand";
            Token.molHand.name = Token.playerName + "_MolHand";

            var renderer = Token.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.3f*i, 0.3f*i, 0.3f*i); // 色を変えるなど区別(適当)
            Token.MoveCompleted += OnMoveCompleted;
            players.Add(Token);
        }

        if(diceUI == null) diceUI = FindObjectOfType<DiceUIController>();
        if(cameraFollow == null) cameraFollow = FindObjectOfType<CameraFollow2D>();
        if(HandUI == null) HandUI = FindObjectOfType<HandUI>();
        if(MolHandUI == null) MolHandUI = FindObjectOfType<MoleculeHandUI>();

        HandUI.playerHand = CurrentPlayer.atomHand; // 最初のプレイヤーの手札をセット
        HandUI.moleculeHand = CurrentPlayer.molHand; // 最初のプレイヤーの分子手札をセット
        MolHandUI.moleculeHand = CurrentPlayer.molHand; // 最初のプレイヤーの分子手札をセット
        Sugoroku.UI.MessageManager.Important($"{CurrentPlayer.playerName} のターンです！");
        HandUI.Refresh();
        MolHandUI.Refresh();
        // 最初の状態へ
        SetState(GameState.Turn_AwaitInput);
    }

    // DiceUI から呼ぶ：最終出目が決まったらここへ
    public bool OnDiceFinal(int steps)
    {
        if (!CanRoll()) return false;
        SetState(GameState.Turn_Moving);
        return CurrentPlayer.MoveBy(steps); // TokenMover 側のコルーチンで移動
    }

    // DiceUI がボタンを押して良いかどうかの判定に使う
    public bool CanRoll()
    {
        return CurrentPlayer != null 
            && State == GameState.Turn_AwaitInput
            && CurrentPlayer.ordinalPlayerNumber == currentPlayerIndex;
    }

    void OnMoveCompleted()
    {
        // ゴール到達は TokenMover が画面を出す仕様のままでOK。ここでは状態だけ End に。
        if (CurrentPlayer.currentWaypoint != null &&
        CurrentPlayer.currentWaypoint.transform.parent.name.Contains("Goal"))
        {
            SetState(GameState.End);
            return;
        }
        StartCoroutine(ResolveAndNext());
    }

    IEnumerator ResolveAndNext()
    {
        if (State != GameState.Turn_Moving) yield break;
        SetState(GameState.Turn_Resolving);
        yield return new WaitForSeconds(resolveDelay);

        // 現在のマスのイベント処理
        var wp = CurrentPlayer.currentWaypoint;
        if (wp != null && wp.tileEvent != null)
        {
            //Debug.Log("イベント呼び出し: " + wp.tileEvent.eventType);
            yield return ExecuteTileEvent(wp.tileEvent);
        }

        if (CurrentPlayer.ExtraTurn)
        {
            CurrentPlayer.ExtraTurn = false; // フラグをリセット
            // もう一度的なメッセージを出すなど
            SetState(GameState.Turn_AwaitInput); // もう一度自分のターン
            yield break;
        }

        // ターン終了の余韻
        yield return new WaitForSeconds(turnDelay);

        // 次のプレイヤーへ
        currentTurn++;
        while (CurrentPlayer.SkipTurn){
            CurrentPlayer.SkipTurn = false; // フラグをリセット
            currentTurn++;
            // 動けなかった的なメッセージを出すなど
        }

        // だれだれのターンみたいなメッセージを出すなど
        HandUI.playerHand = CurrentPlayer.atomHand; // 次のプレイヤーの手札をセット
        HandUI.moleculeHand = CurrentPlayer.molHand; // 次のプレイヤーの分子手札をセット
        MolHandUI.moleculeHand = CurrentPlayer.molHand; // 次のプレイヤーの分子手札をセット
        Sugoroku.UI.MessageManager.Important($"{CurrentPlayer.playerName} のターンです！");
        HandUI.Refresh();
        MolHandUI.Refresh();
        SetState(GameState.Turn_AwaitInput);
    }


    void SetState(GameState next)
    {
        if (State == next) return;
        var prev = State;
        State = next;
        OnStateChanged?.Invoke(prev, next);
        // 必要ならここで UI の有効/無効を切り替える
        if (State == GameState.Turn_AwaitInput)
        {
            // カメラを現在のプレイヤーに追従させる
            if (cameraFollow != null && CurrentPlayer != null)
            {
                cameraFollow.target = CurrentPlayer.transform;
            }
        }
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
                Sugoroku.UI.MessageManager.Important($"{tile.value}進む！");
                yield return CurrentPlayer.MoveStepsEvent(tile.value);
                break;
            case TileEvent.EventType.Back:
                Sugoroku.UI.MessageManager.Important($"{tile.value}戻る…");
                yield return CurrentPlayer.MoveStepsSignedEvent(-tile.value);
                break;
            case TileEvent.EventType.ExtraTurn:
                // もう一度サイコロを振れる
                Sugoroku.UI.MessageManager.Important($"もう1回！");
                CurrentPlayer.ExtraTurn = true;
                break;
            case TileEvent.EventType.SkipNext:
                // 一回休み
                Sugoroku.UI.MessageManager.Important($"1回休み…");
                CurrentPlayer.SkipTurn = true;
                break;
            case TileEvent.EventType.GoToStart:
                // スタートに戻る 
                Sugoroku.UI.MessageManager.Important($"振り出しに戻る…");
                yield return CurrentPlayer.GoToStart();
                break;
            case TileEvent.EventType.Quiz:
                /*// ミニゲーム（クイズ）を表示。
                quizManager.ShowQuizUI();
                // プレイヤーの入力を待つ（無制限で待つ）。必要ならタイムアウトを秒数で指定できます。
                yield return StartCoroutine(quizManager.WaitForAnswerRoutine());

                // 正解処理
                if (quizManager.CorrectAnswer)
                {
                    Debug.Log("Quiz Correct Answer!");
                    // 正解なら1マス進む
                    yield return myToken.MoveStepsEvent(1);
                }
                // 不正解処理
                else if (quizManager.WrongAnswer)
                {
                    Debug.Log("Quiz Wrong Answer!");
                }*/
                break;
        }
    }
}
