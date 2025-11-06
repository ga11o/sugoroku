using System.Collections;
using UnityEngine;

using System.Collections.Generic;

public class TokenMover : MonoBehaviour
{
    public Waypoint currentWaypoint; 
    private Waypoint previousWaypoint;
    private Stack<Waypoint> pathHistory = new Stack<Waypoint>();
    public GameStateMachine gameState; // ← インスペクタでセット

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

    /*public GameObject branchButtonPrefab;
    public Transform branchUIRoot;

    public IEnumerator ShowBranchChoice(List<Waypoint> options, System.Action<Waypoint> onChosen)
    {
        bool decided = false;
        Waypoint chosen = null;

        foreach (var opt in options)
        {
            var btnObj = Instantiate(branchButtonPrefab, branchUIRoot);
            var btn = btnObj.GetComponent<UnityEngine.UI.Button>();
            var label = btnObj.GetComponentInChildren<TMPro.TMP_Text>();
            label.text = opt.name; // まずは名前で表示

            btn.onClick.AddListener(() => { chosen = opt; decided = true; });
        }

        yield return new WaitUntil(() => decided);

        foreach (Transform child in branchUIRoot) Destroy(child.gameObject);

        onChosen?.Invoke(chosen);
    }*/

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
        if (gameState == null)
        {
            gameState = FindObjectOfType<GameStateMachine>();
            if (gameState == null)
            {
                Debug.LogError("GameStateMachine がシーンに見つかりません！");
            }
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
        //if (board != null && board.Count > 0)
        //  transform.position = board.GetPoint(currentIndex) + tokenOffset; //初期位置をtokenOffset分ずらす
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

        for (int i = 0; i < steps; i++)
        {
            //if (currentWaypoint == null || currentWaypoint.nextWaypoints.Count == 0)
            if (currentWaypoint == null || (currentWaypoint.mainNext == null && currentWaypoint.branchNexts.Count == 0))
            {
                Debug.Log("これ以上進めません");
                break;
            }

            Waypoint next = null;

            // 分岐がある場合
            if (currentWaypoint.branchNexts.Count > 0)
            {
                if (currentWaypoint.branchNexts.Count == 1)
                {
                    next = currentWaypoint.branchNexts[0]; // 自動進行
                }
                else
                {
                    // 分岐選択UIを呼ぶ処理に置き換え可能
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
                // 通常の一本道
                next = currentWaypoint.mainNext;
            }


            if (next == null) break;

            // ★ デバッグログ追加
            Debug.Log($"[MoveSteps] {currentWaypoint.name} → {next.name}");
            // 移動
            yield return MoveTo(next.transform.position + tokenOffset);

            // 更新
            previousWaypoint = currentWaypoint;
            pathHistory.Push(currentWaypoint);
            currentWaypoint = next;

        }

        // ★ イベントマスに止まったらイベント処理を呼ぶ
        if (!eventResolving && currentWaypoint.tileEvent != null)
        {
            Debug.Log("イベント呼び出し: " + currentWaypoint.tileEvent.eventType);
            yield return gameState.ExecuteTileEvent(currentWaypoint.tileEvent);
        }
        isMoving = false;

        // ★ ゴール判定
        if (currentWaypoint != null && currentWaypoint.isGoal)
        {
            Debug.Log("ゴールに到達！");
            if (endScreen != null)
            {
                endScreen.Show("ゴール！", "おめでとう 🎉");
            }
            yield break; // 移動終了
        }

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

        while (remain > 0)
        {
            Waypoint next = null;
            if (dir > 0)
            {
                // 前進
                if (currentWaypoint.branchNexts.Count > 0)
                {
                    next = currentWaypoint.branchNexts[0]; // 仮：分岐候補が1つなら自動
                }
                else
                {
                    next = currentWaypoint.mainNext;
                }
            }
            else
            {
                // 後退
                if (pathHistory.Count > 0)
                {
                    next = pathHistory.Pop();
                }
                else
                {
                    next = currentWaypoint.previous;
                }
            }

            if (next == null) break;

            // ★ デバッグログ追加
            Debug.Log($"[MoveStepsSigned] {currentWaypoint.name} → {next.name}");

            yield return MoveTo(next.transform.position + tokenOffset);
            previousWaypoint = currentWaypoint;
            currentWaypoint = next;
            remain--;
        }

        isMoving = false;
        if (!eventResolving) MoveCompleted?.Invoke();
    }


    // ★ 追加：イベント処理用のコルーチン
    public IEnumerator MoveStepsEvent(int steps)
    {
        eventResolving = true; // ★ イベント処理中フラグを立てる
        yield return MoveSteps(steps);
        eventResolving = false; // ★ フラグを下ろす

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

            // 線形補間
            Vector3 basePos = Vector3.Lerp(start, target, u);

            // ★ ジャンプオフセットを加える
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

            Debug.Log($"[GoToStart] {currentWaypoint.name} → {prev.name}");
            yield return MoveTo(prev.transform.position + tokenOffset);
            previousWaypoint = currentWaypoint;
            currentWaypoint = prev;
        }
        isMoving = false;
    }
    // ★ 追加：イベント処理用の符号付きコルーチン
    public IEnumerator MoveStepsSignedEvent(int steps)
    {
        eventResolving = true; // ★ イベント処理中フラグを立てる
        yield return MoveStepsSigned(steps);
        eventResolving = false; // ★ フラグを下ろす
    }
}
