using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceUIController : MonoBehaviour
{
    [Header("参照")]
    public Button rollButton;     // サイコロボタン
    public TMP_Text diceText;     // 出目表示（TMP推奨）
    public AudioSource sfxRoll;   // 任意（サイコロ音）

    [Header("アニメ設定")]
    public float rollAnimDuration = 0.6f;
    public float rollAnimInterval = 0.06f;

    bool rolling;

    [Header("ステートマシン")]
    public GameStateMachine gsm;  // ← ステート管理と連携

    [Header("デバッグ")]
    public bool debugUseFixed = false;
    [Range(1,6)] public int debugFixedValue = 6;

    void Awake()
    {
        if (gsm == null) gsm = FindObjectOfType<GameStateMachine>();

        if (rollButton != null)
            rollButton.onClick.AddListener(OnClickRoll);

        if (diceText != null) diceText.text = "—"; // 初期表示
    }

    void Update()
    {
        if (rollButton == null) return;

        // 自分のターン＆入力待ちの時だけ押せる
        bool canRoll = gsm ? gsm.CanRoll() : false;
        rollButton.interactable = !rolling && canRoll;
    }

    // Button の OnClick からも割り当て可能に public に
    public void OnClickRoll()
    {
        // 押してよい状態かを最終確認（ステートマシン基準）
        if (rolling) return;
        if (gsm != null && !gsm.CanRoll()) return;
        if (gsm == null) return;

        StartCoroutine(RollRoutine());
    }

    IEnumerator RollRoutine()
    {
        rolling = true;
        if (rollButton) rollButton.interactable = false;

        if (sfxRoll) sfxRoll.Play();

        float t = 0f;
        int shown = 1;

        // クルクル表示
        while (t < rollAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            shown = Random.Range(1, 7);
            if (diceText) diceText.text = shown.ToString();
            yield return new WaitForSecondsRealtime(rollAnimInterval);
        }

        // 最終出目
        // 最終出目を決定（ここだけ置換）
        int final = debugUseFixed ? Mathf.Clamp(debugFixedValue, 1, 6)
                                : Random.Range(1, 7);
        if (diceText) diceText.text = final.ToString();

        // ★ ステートマシンへ通知して駒を動かす
        bool accepted = gsm ? gsm.OnDiceFinal(final) : false;
        if (!accepted) { rolling = false; yield break; }

        // 駒の移動終了を待つ（token は必須で保持しておく）
        while (gsm.CurrentPlayer != null && gsm.CurrentPlayer.isMoving) yield return null;

        rolling = false;
        if (rollButton) rollButton.interactable = true;
    }
}
