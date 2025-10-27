using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DebugUIController : MonoBehaviour
{
    [Header("参照")]
    public TokenMover token;
    public DiceUIController dice;
    public GameStateMachine gsm;

    [Header("UI部品")]
    public Toggle useFixedToggle;
    public TMP_InputField fixedValueInput; // 1〜6
    public Button stepPlusBtn;             // +1
    public Button stepMinusBtn;            // -1

    [Header("表示テキスト")]
    public TMP_Text stateText;             // 現在ステート
    public TMP_Text indexText;             // 現在マス
    public TMP_Text movingText;            // 移動中か
    public TMP_Text diceMirrorText;        // ダイスの表示をミラー

    void Start()
    {
        // 既存ボタンの結線
        if (stepPlusBtn)  stepPlusBtn.onClick.AddListener(OnStepPlus);
        if (stepMinusBtn) stepMinusBtn.onClick.AddListener(OnStepMinus);

        // 入力欄の有効/無効（必要に応じて）
        if (useFixedToggle && fixedValueInput)
        {
            fixedValueInput.interactable = useFixedToggle.isOn;
            useFixedToggle.onValueChanged.AddListener(on => fixedValueInput.interactable = on);
        }
    }

    void Update()
    {
        // ラベル更新（存在チェックを十分に）
        if (gsm && stateText)   stateText.text   = $"State: {gsm.State}";
        if (token && indexText) indexText.text   = $"Index: {token.currentIndex}";
        if (token && movingText)movingText.text  = $"Moving: {token.isMoving}";
        if (dice && diceMirrorText)
        {
            var t = dice.diceText ? dice.diceText.text : "-";
            diceMirrorText.text = $"Dice: {t}";
        }

        // 駒移動中は±ボタンを無効に
        if (stepPlusBtn)  stepPlusBtn.interactable  = !(token && token.isMoving);
        if (stepMinusBtn) stepMinusBtn.interactable = !(token && token.isMoving);
    }

    void OnStepPlus()
    {
        // 可能ならステートマシン経由（自分のターンだけ+1）
        if (gsm != null && gsm.CanRoll())
        {
            gsm.OnDiceFinal(1);
        }
        else if (token != null && !token.isMoving)
        {
            // デバッグ強制：状態に関係なく1マス進めたいとき
            token.MoveBySigned(+1);
        }
    }

    void OnStepMinus()
    {
        if (token != null && !token.isMoving)
            token.MoveBySigned(-1); // 1マス戻る（デバッグ専用）
    }

    // ===== Debug Warp additions (Canvas-based) =====
    [Header("ワープUI")]
    public TMP_InputField warpIndexInput;   // マス番号を直接入力
    public Button        warpButton;        // 実行ボタン

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void OnEnable()
    {
        if (warpButton)   warpButton.onClick.AddListener(OnWarpClicked);
        RefreshWarpOptions();
    }

    void OnDisable()
    {
        if (warpButton)   warpButton.onClick.RemoveListener(OnWarpClicked);
    }

    void RefreshWarpOptions()
    {
        if (token == null || token.board == null) return;
        int n = token.board.Count;
        var opts = new System.Collections.Generic.List<string>(n);
        for (int i = 0; i < n; i++) opts.Add(i.ToString("000"));

        if (0 <= token.currentIndex && token.currentIndex < n)
        {
            if (warpIndexInput) warpIndexInput.text = token.currentIndex.ToString();
        }
    }

    void OnWarpClicked()
    {
        if (token == null || token.board == null) { Debug.LogWarning("[DebugUI] token/board 未設定"); return; }
        if (token.isMoving) { Debug.LogWarning("[DebugUI] 移動中はワープ不可"); return; }

        int n = token.board.Count;
        int idx = token.currentIndex;

    if (warpIndexInput && int.TryParse(warpIndexInput.text, out var parsed))
        {
            idx = Mathf.Clamp(parsed, 0, n - 1);
        }

        // ★ イベントを発火しない安全なワープ
        Vector3 pos = token.board.GetPoint(idx);
        token.transform.position = pos;
        token.currentIndex = idx;
        token.isMoving = false;

        Debug.Log($"[DebugUI] Warp to index {idx}");
    }
#endif
    // ===== end of Debug Warp additions =====
}
