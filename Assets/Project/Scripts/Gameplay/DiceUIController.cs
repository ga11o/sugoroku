using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 分子手札＆効果サービス
using Sugoroku.Atoms;

public class DiceUIController : MonoBehaviour
{
    [Header("参照")]
    public TokenMover token;      // 駒
    public Button rollButton;     // サイコロボタン
    public TMP_Text diceText;     // 出目表示
    public AudioSource sfxRoll;   // 任意（サイコロ音）

    [Header("アニメ設定")]
    public float rollAnimDuration = 0.6f;
    public float rollAnimInterval = 0.06f;

    bool rolling;

    [Header("ステートマシン")]
    public GameStateMachine gsm;

    [Header("デバッグ")]
    public bool debugUseFixed = false;
    [Range(1,6)] public int debugFixedValue = 6;

    [Header("Molecule Effects")]
    public PlayerMoleculeHand playerMoleculeHand;

    void Awake()
    {
        if (token == null) token = FindObjectOfType<TokenMover>();
        if (gsm   == null) gsm   = FindObjectOfType<GameStateMachine>();
        if (playerMoleculeHand == null) playerMoleculeHand = FindObjectOfType<PlayerMoleculeHand>(true);

        if (rollButton != null)
            rollButton.onClick.AddListener(OnClickRoll);

        if (diceText != null) diceText.text = "—";
    }

    void Update()
    {
        if (rollButton == null) return;
        bool canRoll = gsm ? gsm.CanRoll() : (token != null && !token.isMoving);
        rollButton.interactable = !rolling && canRoll;
    }

    public void OnClickRoll()
    {
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

        // 演出
        while (t < rollAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            shown = Random.Range(1, 7);
            if (diceText) diceText.text = shown.ToString();
            yield return new WaitForSecondsRealtime(rollAnimInterval);
        }

        // --- 最終出目確定（効果フック） ---
        int final = debugUseFixed ? Mathf.Clamp(debugFixedValue, 1, 6)
                          : Random.Range(1, 7);

        // ★ ここで「このロール中は制限解除か？」を先に保持
        bool liftThisRoll = MoleculeEffectService.Instance &&
                            MoleculeEffectService.Instance.ShouldLiftDiceLimit();

        // BeforeRoll（範囲/偶奇）
        if (MoleculeEffectService.Instance != null)
            MoleculeEffectService.Instance.ApplyBeforeRoll(playerMoleculeHand, ref final);

        // AfterRoll（±N/偶奇/最終範囲）
        if (MoleculeEffectService.Instance != null)
            MoleculeEffectService.Instance.ApplyAfterRoll(playerMoleculeHand, ref final);

        // ★ ここは「最初に決めたフラグ」で判定（途中でDisarmされても維持）
        if (!liftThisRoll)
            final = Mathf.Clamp(final, 1, 6);

        if (diceText) diceText.text = final.ToString();
        Sugoroku.UI.MessageManager.Important($"サイコロの出目は {final} です！");
        
        // ステートマシン経由 or 直接移動
        bool accepted = gsm ? gsm.OnDiceFinal(final)
                            : (token != null && token.MoveBy(final));
        if (!accepted) { rolling = false; yield break; }

        while (token != null && token.isMoving) yield return null;

        rolling = false;
        if (rollButton) rollButton.interactable = true;
    }
}
