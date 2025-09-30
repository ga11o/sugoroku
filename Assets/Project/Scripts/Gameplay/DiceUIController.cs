using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DiceUIController : MonoBehaviour
{
    [Header("参照")]
    public TokenMover token;      // 駒
    public Button rollButton;     // サイコロボタン
    public TMP_Text diceText;     // 出目表示（TMP推奨）
    public AudioSource sfxRoll;   // 任意（サイコロ音）

    [Header("アニメ設定")]
    public float rollAnimDuration = 0.6f;
    public float rollAnimInterval = 0.06f;

    bool rolling;

    void Awake()
    {
        if (token == null) token = FindObjectOfType<TokenMover>();
        rollButton.onClick.AddListener(OnClickRoll);
        if (diceText != null) diceText.text = "—"; // 初期表示
    }

    // DiceUIController.cs の Update を安全版に
    void Update()
    {
        if (rollButton == null) return;
        bool tokenMoving = (token != null && token.isMoving);
        rollButton.interactable = !rolling && !tokenMoving; // これで常に意図通りの状態に
    }


    public void OnClickRoll()
    {
        if (rolling || token == null || token.isMoving) return;
        StartCoroutine(RollRoutine());
    }


    IEnumerator RollRoutine()
    {
        rolling = true;
        rollButton.interactable = false;

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

        // 最終出目を決定
        int final = Random.Range(1, 7);
        if (diceText) diceText.text = final.ToString();

        // 進める（戻り値で弾かれたらUIだけ元に戻す）
        bool accepted = token.MoveBy(final);
        if (!accepted) { rolling = false; yield break; }

        // 駒の移動が終わるまで待つ
        while (token.isMoving) yield return null;

        rolling = false;
        rollButton.interactable = true;
    }
}
