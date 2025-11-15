using System;
using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Sugoroku.Atoms;
using Sugoroku.UI;
using Sugoroku.Quiz;

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
    public List<int> playerOrder = new List<int> { 0, 3, 1, 2 }; // プレイヤーの順番リスト(拡張用)
    public int currentPlayerIndex => playerOrder[currentTurn % players.Count]; // 現在のプレイヤー番号
    public TokenMover CurrentPlayer => players[currentPlayerIndex]; // 現在のプレイヤー駒
    public DiceUIController diceUI; // DiceUI への参照
    public CameraFollow2D cameraFollow; // カメラ制御への参照
    public HandUI HandUI; // 手札UI への参照
    public MoleculeHandUI MolHandUI; // 分子手札UI への参照
    public QuizUIController quizUI; // クイズUI への参照

    [Header("クイズ設定")]
    [Tooltip("ここに登録したクイズの中からランダムに出題されます")]
    public QuizQuestionDef[] quizPool;

    [Header("調整")]
    public float turnDelay = 0.6f;  // ターン終了時の余韻
    public float resolveDelay = 0.25f; // 着地後の演出/判定の待ち

    public GameState State { get; private set; } = GameState.Turn_AwaitInput;
    public event Action<GameState, GameState> OnStateChanged;

    void Start()
    {
        players.Clear();
        // 一旦人間4人作る
        if (players.Count == 0)
        {
            players.Add(FindObjectOfType<TokenMover>());
            players[0].MoveCompleted += OnMoveCompleted;
            players[0].ordinalPlayerNumber = 0;
            players[0].playerName = "Player1";

            players[0].atomHand = FindObjectOfType<PlayerHand>();
            players[0].atomHand.transform.SetParent(players[0].transform);
            players[0].molHand = FindObjectOfType<PlayerMoleculeHand>();
            players[0].molHand.transform.SetParent(players[0].transform);
        }
        for (int i = 1; i < playerCount; i++)
        {
            var Obj = Instantiate(players[0].gameObject, players[0].transform.parent);
            TokenMover Token = Obj.GetComponent<TokenMover>();
            Token.board = players[0].board; // 同じ盤面を使う
            Token.playerName = "Player" + (i + 1);
            Token.ordinalPlayerNumber = i;

            Token.atomHand.name = Token.playerName + "_AtomHand";
            Token.molHand.name = Token.playerName + "_MolHand";

            var renderer = Token.GetComponentInChildren<Renderer>();
            if (renderer != null) renderer.material.color = new Color(0.3f * i, 0.3f * i, 0.3f * i); // 色を変えるなど区別(適当)
            Token.MoveCompleted += OnMoveCompleted;
            players.Add(Token);
        }

        if (diceUI == null) diceUI = FindObjectOfType<DiceUIController>();
        if (cameraFollow == null) cameraFollow = FindObjectOfType<CameraFollow2D>();
        if (HandUI == null) HandUI = FindObjectOfType<HandUI>();
        if (MolHandUI == null) MolHandUI = FindObjectOfType<MoleculeHandUI>();
        if (quizUI == null) quizUI = FindObjectOfType<QuizUIController>();

        HandUI.playerHand = CurrentPlayer.atomHand; // 最初のプレイヤーの手札をセット
        HandUI.moleculeHand = CurrentPlayer.molHand; // 最初のプレイヤーの分子手札をセット
        MolHandUI.moleculeHand = CurrentPlayer.molHand; // 最初のプレイヤーの分子手札をセット
        MessageManager.Important($"{CurrentPlayer.playerName} のターンです！");
        HandUI.Refresh();
        MolHandUI.Refresh();
        // 最初の状態へ
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
            yield return ExecuteTileEvent(wp.tileEvent);
        }

        if (CurrentPlayer.ExtraTurn)
        {
            CurrentPlayer.ExtraTurn = false; // フラグをリセット
            SetState(GameState.Turn_AwaitInput); // もう一度自分のターン
            yield break;
        }

        // ターン終了の余韻
        yield return new WaitForSeconds(turnDelay);

        // 次のプレイヤーへ
        currentTurn++;
        while (CurrentPlayer.SkipTurn)
        {
            CurrentPlayer.SkipTurn = false; // フラグをリセット
            currentTurn++;
        }

        HandUI.playerHand = CurrentPlayer.atomHand; // 次のプレイヤーの手札をセット
        HandUI.moleculeHand = CurrentPlayer.molHand; // 次のプレイヤーの分子手札をセット
        MolHandUI.moleculeHand = CurrentPlayer.molHand; // 次のプレイヤーの分子手札をセット
        MessageManager.Important($"{CurrentPlayer.playerName} のターンです！");
        HandUI.Refresh();
        MolHandUI.Refresh();
        SetState(GameState.Turn_AwaitInput);
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
                MessageManager.Important($"{tile.value}進む！");
                yield return CurrentPlayer.MoveStepsEvent(tile.value);
                break;

            case TileEvent.EventType.Back:
                MessageManager.Important($"{tile.value}戻る…");
                yield return CurrentPlayer.MoveStepsSignedEvent(-tile.value);
                break;

            case TileEvent.EventType.ExtraTurn:
                MessageManager.Important($"もう1回！");
                CurrentPlayer.ExtraTurn = true;
                break;

            case TileEvent.EventType.SkipNext:
                MessageManager.Important($"1回休み…");
                CurrentPlayer.SkipTurn = true;
                break;

            case TileEvent.EventType.GoToStart:
                MessageManager.Important($"振り出しに戻る…");
                yield return CurrentPlayer.GoToStart();
                break;

            case TileEvent.EventType.Quiz:
                // クイズイベント
                MessageManager.Important($"クイズマス！ {CurrentPlayer.playerName} はクイズに挑戦！");

                if (quizUI == null) quizUI = FindObjectOfType<QuizUIController>();

                string question   = tile.quizQuestion;
                string[] choices  = tile.quizChoices;
                int correctIndex  = tile.quizCorrectIndex;
                AtomCardDef reward = tile.quizRewardCard;

                // quizPool に何か入っていれば、その中からランダムに1問選ぶ
                if (quizPool != null && quizPool.Length > 0)
                {
                    int idx = UnityEngine.Random.Range(0, quizPool.Length);
                    var q = quizPool[idx];
                    if (q != null)
                    {
                        if (!string.IsNullOrWhiteSpace(q.question))
                            question = q.question;
                        if (q.choices != null && q.choices.Length >= 4)
                            choices = q.choices;
                        correctIndex = q.correctIndex;
                        if (q.rewardCard != null)
                            reward = q.rewardCard;
                    }
                }

                if (quizUI != null)
                {
                    bool isCorrect = false;
                    yield return quizUI.ShowQuiz(
                        question,
                        choices,
                        correctIndex,
                        result => isCorrect = result
                    );

                    if (isCorrect)
                    {
                        if (reward != null && CurrentPlayer.atomHand != null)
                        {
                            CurrentPlayer.atomHand.Add(reward);
                            MessageManager.Important(
                                $"{CurrentPlayer.playerName} は正解！『{reward.nameJP}』の原子カードを獲得した！");
                        }
                        else
                        {
                            MessageManager.Important($"{CurrentPlayer.playerName} は正解！");
                        }
                    }
                    else
                    {
                        if (CurrentPlayer.atomHand != null && CurrentPlayer.atomHand.Cards.Count > 0)
                        {
                            var cards = CurrentPlayer.atomHand.Cards;
                            int lostIndex = UnityEngine.Random.Range(0, cards.Count);
                            var lost = cards[lostIndex];
                            CurrentPlayer.atomHand.RemoveOne(lost);
                            MessageManager.Important(
                                $"{CurrentPlayer.playerName} は不正解… 原子カード『{lost.nameJP}』を失ってしまった！");
                        }
                        else
                        {
                            MessageManager.Important(
                                $"{CurrentPlayer.playerName} は不正解… しかし原子カードを持っていなかった！");
                        }
                    }
                }
                else
                {
                    Debug.LogWarning("[GameStateMachine] QuizUIController がシーン内に見つかりません。");
                }
                break;
        }
    }
}
