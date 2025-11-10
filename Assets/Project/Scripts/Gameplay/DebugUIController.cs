using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugUIController : MonoBehaviour
{
    [Header("参照")]
    public DiceUIController dice;
    public GameStateMachine gsm;

    [Header("UI部品")]
    public Toggle useFixedToggle;
    public TMP_InputField fixedValueInput; // 1〜6
    public Button stepPlusBtn;             // +1
    public Button stepMinusBtn;            // -1

    [Header("表示テキスト")]
    public TMP_Text stateText;             // 現在ステート
    public TMP_Text turnText;              // 現在ターン数
    public TMP_Text nameText;              // 現在プレイヤー名
    public TMP_Text indexText;             // 現在マス
    public TMP_Text diceMirrorText;        // ダイスの表示をミラー

    void Awake()
    {
        if (dice  == null) dice  = FindObjectOfType<DiceUIController>();
        if (gsm   == null) gsm   = FindObjectOfType<GameStateMachine>();

        if (useFixedToggle != null)
        {
            useFixedToggle.isOn = dice ? dice.debugUseFixed : false;
            useFixedToggle.onValueChanged.AddListener(v => { if (dice) dice.debugUseFixed = v; });
        }

        if (fixedValueInput != null)
        {
            int init = (dice != null) ? dice.debugFixedValue : 6;
            fixedValueInput.text = init.ToString();
            fixedValueInput.onEndEdit.AddListener(s =>
            {
                if (dice == null) return;
                if (int.TryParse(s, out int v))
                    dice.debugFixedValue = Mathf.Clamp(v, 1, 6);
                fixedValueInput.text = dice.debugFixedValue.ToString();
            });
        }

        if (stepPlusBtn != null)  stepPlusBtn.onClick.AddListener(OnStepPlus);
        if (stepMinusBtn != null) stepMinusBtn.onClick.AddListener(OnStepMinus);
    }

    void Update()
    {
        // ラベル更新（存在チェックを十分に）
        if (gsm && stateText)   stateText.text   = $"State: {gsm.State}";
        if (dice && diceMirrorText)
        {
            var t = dice.diceText ? dice.diceText.text : "-";
            diceMirrorText.text = $"Dice: {t}";
        }

        // 駒移動中は±ボタンを無効に
        if (stepPlusBtn)  stepPlusBtn.interactable  = !(gsm.CurrentPlayer && gsm.CurrentPlayer.isMoving);
        if (stepMinusBtn) stepMinusBtn.interactable = !(gsm.CurrentPlayer && gsm.CurrentPlayer.isMoving);
        if (gsm.CurrentPlayer && turnText) turnText.text   = $"Turn: {gsm.currentTurn / gsm.players.Count + 1}";
        if (gsm.CurrentPlayer && nameText) nameText.text   = $"Name: {gsm.CurrentPlayer.playerName}";
        if (gsm.CurrentPlayer && indexText) indexText.text   = $"Index: {gsm.CurrentPlayer.currentIndex}";
    }

    void OnStepPlus()
    {
        // 可能ならステートマシン経由（自分のターンだけ+1）
        if (gsm != null && gsm.CanRoll())
        {
            gsm.CurrentPlayer.MoveBy(1);
        }
    }

    void OnStepMinus()
    {
        if (gsm.CurrentPlayer != null && gsm.CanRoll())
        {
            gsm.CurrentPlayer.MoveBySigned(-1);
        }
    }

    // ===== Debug Warp additions (Canvas-based) =====
    [Header("ワープUI")]
    public TMP_InputField warpIndexInput;   // マス番号を直接入力
    public Button        warpButton;        // 実行ボタン

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void OnEnable()
    {
        if (warpButton)   warpButton.onClick.AddListener(OnWarpClicked);
        if (warpIndexInput) RefreshWarpOptions();
    }

    void OnDisable()
    {
        if (warpButton)   warpButton.onClick.RemoveListener(OnWarpClicked);
    }

    void RefreshWarpOptions()
    {
        if (gsm.players.Count == 0 || gsm.players[0].board == null) return;
        int n = gsm.players[0].board.Count;
        var opts = new System.Collections.Generic.List<string>(n);
        for (int i = 0; i < n; i++) opts.Add(i.ToString("000"));

        if (0 <= gsm.CurrentPlayer.currentIndex && gsm.CurrentPlayer.currentIndex < n)
        {
            warpIndexInput.text = gsm.CurrentPlayer.currentIndex.ToString();
        }
    }

    void OnWarpClicked()
    {
        if (gsm.players.Count == 0 || gsm.players[0].board == null) { Debug.LogWarning("[DebugUI] token/board 未設定"); return; }
        if (gsm.CurrentPlayer.isMoving) { Debug.LogWarning("[DebugUI] 移動中はワープ不可"); return; }

        int n = gsm.players[0].board.Count;
        int idx = gsm.CurrentPlayer.currentIndex;

        if (warpIndexInput && int.TryParse(warpIndexInput.text, out var parsed))
        {
            idx = Mathf.Clamp(parsed, 0, n - 1);
        }

        // ★ 追加：いまの見え方（オフセット）を保つ
        Vector3 currentAnchor = gsm.CurrentPlayer.board.GetPoint(gsm.CurrentPlayer.currentIndex);
        Vector3 currentDelta  = gsm.CurrentPlayer.transform.position - currentAnchor;

        Vector3 targetAnchor  = gsm.CurrentPlayer.board.GetPoint(idx);
        Vector3 targetPos     = targetAnchor + currentDelta;

        gsm.CurrentPlayer.transform.position = targetPos;
        gsm.CurrentPlayer.currentIndex = idx;
        gsm.CurrentPlayer.currentWaypoint = gsm.CurrentPlayer.board.waypoints[idx];
        gsm.CurrentPlayer.isMoving = false;

        Debug.Log($"[DebugUI] Warp to index {idx}");
    }
#endif
    // ===== end of Debug Warp additions =====
}
