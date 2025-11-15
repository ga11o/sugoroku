using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 分子手札＆効果サービス
using Sugoroku.Atoms;

public class DiceUIController : MonoBehaviour
{
    [Header("参照")]
    public Button rollButton;     // サイコロボタン
    public AudioSource sfxRoll;   // 任意（サイコロ音）

    [Header("アニメ設定")]
    public float rollAnimDuration = 0.6f;
    public float rollAnimInterval = 0.06f;

    [Header("サイコロ見た目")]
    public Image diceImage;      // サイコロの絵を出す Image
    private Image diceImage2;    // 出目の余剰分
    public Sprite[] diceFaces;   // 1〜6 のサイコロ画像（要素数 6）

    private Vector3 originalDicePos;

    bool rolling;

    [Header("ステートマシン")]
    public GameStateMachine gsm;

    [Header("デバッグ")]
    public bool debugUseFixed = false;
    [Range(1,6)] public int debugFixedValue = 6;

    void Awake()
    {
        if (gsm   == null) gsm   = FindObjectOfType<GameStateMachine>();

        if (rollButton != null)
            rollButton.onClick.AddListener(OnClickRoll);

        if (diceImage != null)
            originalDicePos = diceImage.rectTransform.localPosition;
    }

    void Update()
    {
        if (rollButton == null) return;
        bool canRoll = gsm ? gsm.CanRoll() : false;
        rollButton.interactable = !rolling && canRoll;
    }

    public void OnClickRoll()
    {
        if (rolling) return;
        if (gsm != null && !gsm.CanRoll()) return;
        if (gsm == null) return;

        StartCoroutine(RollRoutine());
    }

// ★ 数字と画像をまとめて更新する共通メソッド
    void UpdateDiceVisual(int value)
    {
        // 画像（1〜6の範囲だけ）
        if (diceImage != null && diceFaces != null &&
            value >= 1 && value <= diceFaces.Length)
        {
            diceImage.sprite = diceFaces[value - 1];
        }
        else if (diceImage != null && diceFaces != null && value > 6 && value < 13)
        {
            diceImage.sprite = diceFaces[5];
            diceImage.rectTransform.localPosition += new Vector3(-35f, 0f, 0f);

            GameObject go = Instantiate(diceImage.gameObject, diceImage.transform.parent);
            diceImage2 = go.GetComponent<Image>();
            diceImage2.sprite = diceFaces[value - 7];
            diceImage2.rectTransform.localPosition += new Vector3(35f, 0f, 0f);
        }
    }

    public void ClearExtraDice()
    {
        if (diceImage2 != null)
        {
            Destroy(diceImage2.gameObject);
            diceImage2 = null;
            diceImage.rectTransform.localPosition = originalDicePos;
        }
    }
    
    IEnumerator RollRoutine()
    {
        rolling = true;
        ClearExtraDice();
        if (rollButton) rollButton.interactable = false;
        if (sfxRoll) sfxRoll.Play();

        float t = 0f;
        int shown = 1;

        // 演出
        while (t < rollAnimDuration)
        {
            t += Time.unscaledDeltaTime;
            shown = Random.Range(1, 7);
            UpdateDiceVisual(shown);
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
            MoleculeEffectService.Instance.ApplyBeforeRoll(gsm.CurrentPlayer.molHand, ref final);

        // AfterRoll（±N/偶奇/最終範囲）
        if (MoleculeEffectService.Instance != null)
            MoleculeEffectService.Instance.ApplyAfterRoll(gsm.CurrentPlayer.molHand, ref final);

        // ★ ここは「最初に決めたフラグ」で判定（途中でDisarmされても維持）
        if (!liftThisRoll)
            final = Mathf.Clamp(final, 1, 6);

        UpdateDiceVisual(final);
        Sugoroku.UI.MessageManager.Important($"サイコロの出目は {final} です！");

        
        // ステートマシン経由 or 直接移動
        bool accepted = gsm ? gsm.OnDiceFinal(final) : false;
        if (!accepted) { rolling = false; yield break; }

        while (gsm.CurrentPlayer != null && gsm.CurrentPlayer.isMoving) yield return null;

        rolling = false;
        if (rollButton) rollButton.interactable = true;
    }
}
