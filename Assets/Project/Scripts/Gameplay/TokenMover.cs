using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TokenMover : MonoBehaviour
{
    public Waypoint currentWaypoint; 
    private Waypoint previousWaypoint;
    private Stack<Waypoint> pathHistory = new Stack<Waypoint>();

    public GameStateMachine gameState; // インスペクタでセット

    [Header("参照")]
    public BoardBuilder board;
    public EndScreenController endScreen;

    [Header("移動設定")]
    public float secondsPerTile = 0.22f;
    public float arriveEpsilon = 0.001f;
    public bool stopAtGoal = true;

    [Header("状態")]
    public int currentIndex = 0;
    public bool isMoving = false;
    private bool eventResolving = false; // イベント処理中フラグ

    public event System.Action MoveCompleted; // 移動完了イベント    

    [Header("移動アニメーション")]
    public bool animationJump = true;
    public float jumpHeight = 1.5f;
    public float jumpFrequency = 0f;

    [Header("Tokenの位置調整")]
    public Vector3 tokenOffset = new Vector3(0f, 1f, 0f);

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
        if (gameState == null)
        {
            gameState = FindObjectOfType<GameStateMachine>();
            if (gameState == null)
                Debug.LogError("GameStateMachine がシーンに見つかりません！");
        }

        if (board != null && board.waypoints.Count > 0)
        {
            currentWaypoint = board.waypoints[0];
            transform.position = currentWaypoint.transform.position;
        }
        else
        {
            Debug.LogError("Board または waypoints が未設定です");
        }
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

            // 移動
            yield return MoveTo(next.transform.position + tokenOffset);

            // 更新
            previousWaypoint = currentWaypoint;
            pathHistory.Push(currentWaypoint);
            currentWaypoint = next;
        }

        // イベントマス
        if (!eventResolving && currentWaypoint != null && currentWaypoint.tileEvent != null)
        {
            Debug.Log("イベント呼び出し: " + currentWaypoint.tileEvent.eventType);
            yield return gameState.ExecuteTileEvent(currentWaypoint.tileEvent);
        }

        isMoving = false;

        // ゴール
        if (currentWaypoint != null && currentWaypoint.isGoal)
        {
            Debug.Log("ゴールに到達！");
            if (endScreen != null) endScreen.Show("ゴール！", "おめでとう 🎉");
            yield break;
        }

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

            yield return MoveTo(next.transform.position + tokenOffset);

            previousWaypoint = currentWaypoint;
            currentWaypoint = next;
            remain--;
        }

        isMoving = false;
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
    }

    public IEnumerator GoToStart()
    {
        isMoving = true;
        while (pathHistory.Count > 0)
        {
            var prev = pathHistory.Pop();
            yield return MoveTo(prev.transform.position + tokenOffset);
            previousWaypoint = currentWaypoint;
            currentWaypoint = prev;
        }
        isMoving = false;
    }
}
