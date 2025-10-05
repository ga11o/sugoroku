using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceUIController : MonoBehaviour
{
    [Header("参照")]
    public TokenMover token;      // 駒（保険として保持。無くてもOK）
    public Button rollButton;     // サイコロボタン
    public TMP_Text diceText;     // 出目表示（TMP推奨）
    public AudioSource sfxRoll;   // 任意（サイコロ音）

    [Header("アニメ設定")]
    public float rollAnimDuration = 0.6f;
    public float rollAnimInterval = 0.06f;

    bool rolling;

    [Header("ステートマシン")]
    public GameStateMachine gsm;  // ← ステート管理と連携

    void Awake()
    {
        if (token == null) token = FindObjectOfType<TokenMover>();
        if (gsm   == null) gsm   = FindObjectOfType<GameStateMachine>();

        if (rollButton != null)
            rollButton.onClick.AddListener(OnClickRoll);

        if (diceText != null) diceText.text = "—"; // 初期表示
    }

    void Update()
    {
        if (rollButton == null) return;

        // 自分のターン＆移動中でない時だけ押せる
        bool canRoll = gsm ? gsm.CanRoll() : (token != null && !token.isMoving);
        rollButton.interactable = !rolling && canRoll;
    }

    // Button の OnClick からも割り当て可能に public に
    public void OnClickRoll()
    {
        // 押してよい状態かを最終確認（ステートマシン基準）
        if (rolling) return;
        if (gsm != null && !gsm.CanRoll()) return;
        if (gsm == null && (token == null || token.isMoving)) return;

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
        int final = Random.Range(1, 7);
        if (diceText) diceText.text = final.ToString();

        // ★ ステートマシンへ通知して移動開始
        bool accepted = gsm ? gsm.OnDiceFinal(final)
                            : (token != null && token.MoveBy(final));
        if (!accepted) { rolling = false; yield break; }

        // 駒の移動終了を待つ（token は必須で保持しておく）
        while (token != null && token.isMoving) yield return null;

        rolling = false;
        if (rollButton) rollButton.interactable = true;
    }
}
