using System.Collections;
using UnityEngine;

public class TokenMover : MonoBehaviour
{
    [Header("参照")]
    public BoardBuilder board;
    public EndScreenController endScreen;   // ← 追加：終了画面

    [Header("移動設定")]
    public float secondsPerTile = 0.22f;
    public float arriveEpsilon = 0.001f;
    public bool stopAtGoal = true;

    [Header("状態")]
    public int currentIndex = 0;
    public bool isMoving = false;
    private bool eventResolving = false; // ★ 追加：イベント処理中フラグ

    public event System.Action MoveCompleted; // 移動完了イベント

    void Start()
    {
        if (board != null && board.Count > 0)
            transform.position = board.GetPoint(currentIndex);
    }

    void Update()
    {
        #if ENABLE_INPUT_SYSTEM
                bool space = UnityEngine.InputSystem.Keyboard.current != null &&
                            UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
        #else
                bool space = Input.GetKeyDown(KeyCode.Space);
        #endif
        if (space) RollAndMove();
    }

    public void RollAndMove()
    {
        if (board == null || isMoving) return;
        int dice = Random.Range(1, 7); // 1..6
        StartCoroutine(MoveSteps(dice));
    }

    public IEnumerator MoveSteps(int steps)
    {
        if (steps <= 0) yield break;
        isMoving = true;

        int lastIndex = board.Count - 1;
        int targetIndex = currentIndex + steps;
        if (stopAtGoal && targetIndex > lastIndex) targetIndex = lastIndex;

        while (currentIndex < targetIndex)
        {
            int next = Mathf.Min(currentIndex + 1, lastIndex);
            yield return MoveToIndex(next);
            currentIndex = next;

            if (currentIndex >= lastIndex) break; // ゴール
        }

        isMoving = false;

        // ★ ゴール到達で終了画面
        if (currentIndex >= board.Count - 1 && endScreen != null)
            endScreen.Show("ゴール！", "おめでとう 🎉");
        
        // その直後にイベント通知
        if (!eventResolving) MoveCompleted?.Invoke();
    }

    IEnumerator MoveToIndex(int targetIndex)
    {
        Vector3 start = transform.position;
        Vector3 end = board.GetPoint(targetIndex);
        float t = 0f;
        float duration = Mathf.Max(0.01f, secondsPerTile);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.SmoothStep(0f, 1f, t);
            transform.position = Vector3.Lerp(start, end, u);
            yield return null;
        }
        transform.position = end;
    }

        // UI から「nマス進める」を呼べるようにする
    public bool MoveBy(int steps)
    {
        if (board == null || isMoving) return false;
        StartCoroutine(MoveSteps(steps));
        return true;
    }

    // ★ 追加：符号付きで進む（-1 で1マス戻る）
    public bool MoveBySigned(int steps)
    {
        if (board == null || isMoving || steps == 0) return false;
        StartCoroutine(MoveStepsSigned(steps));
        return true;
    }

    IEnumerator MoveStepsSigned(int steps)
    {
        isMoving = true;

        int dir = (steps > 0) ? 1 : -1;
        int remain = Mathf.Abs(steps);
        int lastIndex = board.Count - 1;

        while (remain > 0)
        {
            int next = Mathf.Clamp(currentIndex + dir, 0, lastIndex);
            if (next == currentIndex) break;      // 端に到達

            yield return MoveToIndex(next);
            currentIndex = next;
            remain--;
        }

        isMoving = false;
        if(!eventResolving) MoveCompleted?.Invoke(); // ステートマシンへ通知（必要なら）
    }

    // ★ 追加：イベント処理用のコルーチン
    public IEnumerator MoveStepsEvent(int steps)
    {
        eventResolving = true; // ★ イベント処理中フラグを立てる
        yield return MoveSteps(steps);
        eventResolving = false; // ★ フラグを下ろす
    }

    // ★ 追加：イベント処理用の符号付きコルーチン
    public IEnumerator MoveStepsSignedEvent(int steps)
    {
        eventResolving = true; // ★ イベント処理中フラグを立てる
        yield return MoveStepsSigned(steps);
        eventResolving = false; // ★ フラグを下ろす
    }
}
