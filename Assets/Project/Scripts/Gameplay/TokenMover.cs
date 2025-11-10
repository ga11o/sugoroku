using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Sugoroku.Atoms;

public class TokenMover : MonoBehaviour
{
    public Waypoint currentWaypoint; 
    private Waypoint previousWaypoint;
    private Stack<Waypoint> pathHistory = new Stack<Waypoint>();

    [Header("参照")]
    public BoardBuilder board;
    public GameStateMachine gsm;
    public EndScreenController endScreen;

    [Header("プレイヤー情報")]
    public string playerName;
    public int ordinalPlayerNumber; // プレイヤー番号(何番目に行動するか、0始まり)
    public bool isCPU {get; set;} = false;
    public PlayerHand atomHand; // 原子カード手札
    public PlayerMoleculeHand molHand; // 分子カード手札

    [Header("移動設定")]
    public float secondsPerTile = 0.22f;
    public float arriveEpsilon = 0.001f;
    public bool stopAtGoal = true;

    [Header("状態")]
    public int currentIndex = 0;
    public bool isMoving = false;
    private bool eventResolving = false; // イベント処理中フラグ
    public bool SkipTurn {get; set;} = false;
    public bool ExtraTurn {get; set;} = false;

    public event System.Action MoveCompleted; // 移動完了イベント    

    [Header("移動アニメーション")]
    public bool animationJump = true;
    public float jumpHeight = 1.5f;
    public float jumpFrequency = 0f;

    [Header("Tokenの位置調整")]
    public Vector3 tokenOffset = new Vector3(0f, 1f, -3f);

    private Vector3 GetJumpOffset(float normalizedProgress)
    {
        if (!animationJump || jumpHeight <= 0f) return Vector3.zero;
        float u = Mathf.Clamp01(normalizedProgress);
        float envelope = Mathf.Sin(Mathf.PI * u);
        float wobble = (jumpFrequency > 0f)
            ? 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * jumpFrequency * u))
            : 1f;
        float lift = jumpHeight * envelope * wobble;
        return Vector3.up * lift;
    }

    void Start()
    {
        if (gsm   == null) gsm   = FindObjectOfType<GameStateMachine>();

        if (board != null && board.waypoints.Count > 0)
        {
            currentWaypoint = board.waypoints[0];
            transform.position = currentWaypoint.transform.position + tokenOffset;
        }
        else
        {
            Debug.LogError("Board または waypoints が未設定です");
        }

        if (!atomHand){
            atomHand = gameObject.AddComponent<PlayerHand>();
        }
        AdjustTokenPosition();
    }

    // ★ ダイスは DiceUIController に一本化するため、Update() のスペースキー処理は置かない

    // ===== 外部API：歩数で移動 =====
    public bool MoveBy(int steps)
    {
        if (board == null || isMoving || steps <= 0) return false;
        StartCoroutine(MoveSteps(steps));
        return true;
    }

    // 後退にも対応
    public bool MoveBySigned(int steps)
    {
        if (board == null || isMoving || steps == 0) return false;
        StartCoroutine(MoveStepsSigned(steps));
        return true;
    }

    // イベントから呼べる（イベント中はMoveCompletedを抑止）
    public IEnumerator MoveStepsEvent(int steps)
    {
        eventResolving = true;
        yield return MoveSteps(steps);
        eventResolving = false;
    }

    public IEnumerator MoveStepsSignedEvent(int steps)
    {
        eventResolving = true;
        yield return MoveStepsSigned(steps);
        eventResolving = false;
    }

    // ===== 実装：前進のみ =====
    public IEnumerator MoveSteps(int steps)
    {
        if (steps <= 0) yield break;
        isMoving = true;

        for (int i = 0; i < steps; i++)
        {
            if (currentWaypoint == null || (currentWaypoint.mainNext == null && currentWaypoint.branchNexts.Count == 0))
            {
                Debug.Log("これ以上進めません");
                break;
            }

            Waypoint next = null;

            if (currentWaypoint.branchNexts.Count > 0)
            {
                if (currentWaypoint.branchNexts.Count == 1)
                {
                    next = currentWaypoint.branchNexts[0];
                }
                else
                {
                    foreach (var candidate in currentWaypoint.branchNexts)
                    {
                        if (candidate != previousWaypoint)
                        {
                            next = candidate;
                            break;
                        }
                    }
                }
            }
            else
            {
                next = currentWaypoint.mainNext;
            }

            if (next == null) break;

            // 更新
            previousWaypoint = currentWaypoint;
            pathHistory.Push(currentWaypoint);
            currentWaypoint = next;

            // 移動
            yield return MoveTo(next.transform.position + tokenOffset);
        }
        isMoving = false;

        // ゴール
        if (currentWaypoint != null && currentWaypoint.isGoal)
        {
            Debug.Log("ゴールに到達！");
            if (endScreen != null) endScreen.Show("ゴール！", "おめでとう 🎉");
            yield break;
        }

        AdjustTokenPosition();
        if (!eventResolving) MoveCompleted?.Invoke();
    }

    // ===== 実装：符号付き（後退対応）=====
    IEnumerator MoveStepsSigned(int steps)
    {
        isMoving = true;
        int dir = (steps > 0) ? 1 : -1;
        int remain = Mathf.Abs(steps);

        while (remain > 0)
        {
            Waypoint next = null;
            if (dir > 0)
            {
                if (currentWaypoint.branchNexts.Count > 0)
                    next = currentWaypoint.branchNexts[0];
                else
                    next = currentWaypoint.mainNext;
            }
            else
            {
                if (pathHistory.Count > 0)
                    next = pathHistory.Pop();
                else
                    next = currentWaypoint.previous;
            }

            if (next == null) break;

            previousWaypoint = currentWaypoint;
            currentWaypoint = next;
            yield return MoveTo(next.transform.position + tokenOffset);
            remain--;
        }

        isMoving = false;
        AdjustTokenPosition();
        if (!eventResolving) MoveCompleted?.Invoke();
    }

    public IEnumerator MoveTo(Vector3 target)
    {
        Vector3 start = transform.position;
        float t = 0f;
        float duration = Mathf.Max(0.01f, secondsPerTile);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.SmoothStep(0f, 1f, t);
            Vector3 basePos = Vector3.Lerp(start, target, u);
            transform.position = basePos + GetJumpOffset(u);
            yield return null;
        }

        transform.position = target;
        currentIndex = board.waypoints.IndexOf(currentWaypoint);
    }

    public IEnumerator GoToStart()
    {
        isMoving = true;
        while (pathHistory.Count > 0)
        {
            var prev = pathHistory.Pop();
            previousWaypoint = currentWaypoint;
            currentWaypoint = prev;
            yield return MoveTo(prev.transform.position + tokenOffset);
        }
        isMoving = false;
    }

    // 同じマスにいる駒同士の位置をずらす
    public void AdjustTokenPosition(){
        if (currentWaypoint == null) return;

        var tokenOnSameTile = new List<TokenMover>();
        foreach (var idx in gsm.playerOrder)
        {
            if (gsm.players[idx].currentWaypoint == this.currentWaypoint)
            {
                tokenOnSameTile.Add(gsm.players[idx]);
            }
        }

        switch (tokenOnSameTile.Count)
        {
            case 1:
                tokenOnSameTile[0].transform.position = currentWaypoint.transform.position + tokenOffset;
                break;
            case 2:
                tokenOnSameTile[0].transform.position = currentWaypoint.transform.position + new Vector3(-0.6f, 0.8f, -5f);
                tokenOnSameTile[1].transform.position = currentWaypoint.transform.position + new Vector3(1f, 1.5f, -1f);
                break;
            case 3:
                tokenOnSameTile[0].transform.position = currentWaypoint.transform.position + new Vector3(-0.4f, 0.5f, -5f);
                tokenOnSameTile[1].transform.position = currentWaypoint.transform.position + new Vector3(-0.8f, 1.8f, -2f);
                tokenOnSameTile[2].transform.position = currentWaypoint.transform.position + new Vector3(1.6f, 1.2f, -1f);
                break;
            case 4:
                tokenOnSameTile[0].transform.position = currentWaypoint.transform.position + new Vector3(0f, 0.5f, -5f);
                tokenOnSameTile[1].transform.position = currentWaypoint.transform.position + new Vector3(-1.6f, 1.2f, -4f);
                tokenOnSameTile[2].transform.position = currentWaypoint.transform.position + new Vector3(0f, 2f, -1f);
                tokenOnSameTile[3].transform.position = currentWaypoint.transform.position + new Vector3(1.6f, 1.2f, -2f);
                break;
            default:
                break;
        }
    }
}
