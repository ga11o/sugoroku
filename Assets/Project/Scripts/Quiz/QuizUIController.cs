using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class QuizUIController : MonoBehaviour
{
    [Header("UI 参照")]
    public GameObject rootPanel;       // クイズ全体の表示パネル
    public TMP_Text questionText;      // 問題文表示
    public Button[] choiceButtons;     // 4つの選択肢ボタン
    public TMP_Text[] choiceLabels;    // 選択肢のテキスト部分（Buttonの子にある）

    [Header("正解/不正解画像")]
    public Image correctImage;   // ○画像
    public Image wrongImage;     // ×画像

    private Action<bool> callback;     // 正解 / 不正解 の結果を返す
    private bool answered = false;

    void Awake()
    {
        if (rootPanel != null)
            rootPanel.SetActive(false);
        if (correctImage != null) correctImage.enabled = false;
        if (wrongImage != null) wrongImage.enabled = false;
    }

    /// <summary>
    /// クイズを表示し、解答が終わるまで待つコルーチン
    /// </summary>
    public IEnumerator ShowQuiz(
        string question,
        string[] choices,
        int correctIndex,
        Action<bool> onAnswered
    )
    {
        callback = onAnswered;
        answered = false;

        // パネル表示
        rootPanel.SetActive(true);

        // ■■■ シャッフル処理 ■■■
        int count = choices.Length;
        int[] order = new int[count];
        for (int i = 0; i < count; i++) order[i] = i;

        // Fisher–Yates shuffle
        for (int i = count - 1; i > 0; i--)
        {
            int j = UnityEngine.Random.Range(0, i + 1);
            (order[i], order[j]) = (order[j], order[i]);
        }

        // シャッフル後のデータ
        string[] shuffled = new string[count];
        int shuffledCorrectIndex = 0;

        for (int i = 0; i < count; i++)
        {
            shuffled[i] = choices[order[i]];

            if (order[i] == correctIndex)
                shuffledCorrectIndex = i;
        }

        // UIに反映
        questionText.text = question;

        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i; // ボタンが押されたときの識別用

            choiceLabels[i].text = shuffled[i];

            choiceButtons[i].onClick.RemoveAllListeners();
            choiceButtons[i].onClick.AddListener(() =>
            {
                OnChoiceSelected(index, shuffledCorrectIndex);
            });
        }

        // 解答されるまで待つ
        while (!answered)
            yield return null;

        yield return new WaitForSeconds(2f);

        // 非表示
        rootPanel.SetActive(false);
    }

    void OnChoiceSelected(int selectedIndex, int correctIndex)
    {
        answered = true;

        bool isCorrect = selectedIndex == correctIndex;

        if (isCorrect) StartCoroutine(ShowCorrectMark());
        else StartCoroutine(ShowWrongMark());

        // 呼び出し元へ結果を返す
        callback?.Invoke(isCorrect);
    }

    IEnumerator ShowCorrectMark()
    {
        if (correctImage != null)
        {
            correctImage.enabled = true;
            yield return new WaitForSeconds(2.0f);
            correctImage.enabled = false;
        }
    }

    IEnumerator ShowWrongMark()
    {
        if (wrongImage != null)
        {
            wrongImage.enabled = true;
            yield return new WaitForSeconds(2.0f);
            wrongImage.enabled = false;
        }
    }

}
