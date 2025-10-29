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

    [Header("移動アニメーション")]
    public bool animationJump = true;      // 移動中に上下の浮きを付与
    public float jumpHeight = 1.5f;     // 浮きの最大高さ（ワールド単位）
    public float jumpFrequency = 0f;     // 浮き中の細かな揺れ回数（0でなし）

    [Header("Tokenの位置調整")]
    public Vector3 tokenOffset = new Vector3(0f, 1f, 0f); // Tokenの中心がタイル中央に来るようにするオフセット

    // 正規化進捗(0..1)に応じた上下オフセット（開始/終了は0＝着地）
    private Vector3 GetJumpOffset(float normalizedProgress)
    {
        if (!animationJump || jumpHeight <= 0f) return Vector3.zero;

        float u = Mathf.Clamp01(normalizedProgress);
        // 山型エンベロープ（0→1→0）で必ず着地させる
        float envelope = Mathf.Sin(Mathf.PI * u);
        // お好みで細かな揺れ（常に正値、端で0）
        float wobble = (jumpFrequency > 0f)
            ? 0.5f * (1f - Mathf.Cos(2f * Mathf.PI * jumpFrequency * u))
            : 1f;
        float lift = jumpHeight * envelope * wobble;
        return Vector3.up * lift;
    }

    void Start()
    {
        if (board != null && board.Count > 0)
            transform.position = board.GetPoint(currentIndex) + tokenOffset; //初期位置をtokenOffset分ずらす
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
        Vector3 end = board.GetPoint(targetIndex) + tokenOffset; //移動先もtokenOffset分ずらす
        float t = 0f;
        float duration = Mathf.Max(0.01f, secondsPerTile);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            float u = Mathf.SmoothStep(0f, 1f, t);
            Vector3 basePos = Vector3.Lerp(start, end, u);  
            transform.position = basePos + GetJumpOffset(u);    //移動中ジャンプするアニメーションを追加
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
