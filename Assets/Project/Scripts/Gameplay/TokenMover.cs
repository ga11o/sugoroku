using System.Collections;
using UnityEngine;

// 入力システムの有無に依存しない書き方
public class TokenMover : MonoBehaviour
{
    [Header("参照")]
    public BoardBuilder board;

    [Header("移動設定")]
    [Tooltip("1マスにかける時間（秒）。盤のスケールが変わっても速度が一定）")]
    public float secondsPerTile = 0.22f;
    [Tooltip("到達とみなす距離（誤差吸収）")]
    public float arriveEpsilon = 0.001f;
    [Tooltip("出目がゴールを超える場合はゴールで止める")]
    public bool stopAtGoal = true;

    [Header("状態")]
    public int currentIndex = 0;   // いま立っているマス（0=スタート）
    public bool isMoving = false;

    void Start()
    {
        // 盤の準備が済んでいればスタート位置へスナップ
        if (board != null && board.Count > 0)
            transform.position = board.GetPoint(currentIndex);
    }

    void Update()
    {
        // スペースでダイス（どちらの入力設定でも動く）
        bool space = false;

#if ENABLE_INPUT_SYSTEM
        // 新Input System
        space = UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        // 旧Input Manager
        space = Input.GetKeyDown(KeyCode.Space);
#endif
        if (space) RollAndMove();
    }

    // ------- 外部UIからも呼べるAPI -------
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

        // 1マスずつ
        while (currentIndex < targetIndex)
        {
            int next = Mathf.Min(currentIndex + 1, lastIndex);
            yield return MoveToIndex(next);
            currentIndex = next;

            if (currentIndex >= lastIndex) break; // ゴール
        }

        isMoving = false;
    }

    IEnumerator MoveToIndex(int targetIndex)
    {
        Vector3 start = transform.position;
        Vector3 end   = board.GetPoint(targetIndex);

        float t = 0f;
        float duration = Mathf.Max(0.01f, secondsPerTile);

        while (t < 1f)
        {
            t += Time.deltaTime / duration;

            // イージング（加速→減速）
            float u = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(start, end, u);
            yield return null;
        }
        transform.position = end; // 誤差吸収
    }
}
