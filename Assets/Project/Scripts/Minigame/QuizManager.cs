using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using UnityEditor.Search;


public class QuizManager : MonoBehaviour
{
    [SerializeField] GameObject QuizUI;  //表示するクイズUI
    [SerializeField] public QuizData QuizData;   //表示するクイズのデータ
    [SerializeField] TextMeshProUGUI QuestionText;   //質問文を表示するテキストUI
    [SerializeField] Button[] ChoiceButtons = new Button[4]; // 選択肢ボタン（Inspectorで割当）
    [SerializeField] GameObject CorrectIcon; // 正解時に表示する赤い円（QuizUI 内の GameObject）
    [SerializeField] GameObject WrongIcon;   // 不正解時に表示する青い×（QuizUI 内の GameObject）
    public bool CorrectAnswer;  // プレイヤーが正答したときTrue
    public bool WrongAnswer;    // プレイヤーが不正解だったときTrue

    // 現在出題中の問題（Inspector の QuizData から取得）
    private QuizData.QuestionData currentQuestion;


    void Awake()
    {
        CorrectAnswer = false;
        WrongAnswer = false;
        QuizUI.SetActive(false);
        CorrectIcon.SetActive(false);
        WrongIcon.SetActive(false);
    }
    
    public void ShowQuizUI()
    {
        // QuizData からランダムな問題を取得して currentQuestion に保持
        CorrectAnswer = false;
        WrongAnswer = false;
        currentQuestion = GetRandomQuestion();
        if (currentQuestion != null && QuestionText != null)
        {
            QuestionText.text = currentQuestion.question;
        }
        // 選択肢をボタンに反映
        if (ChoiceButtons != null)
        {
            for (int i = 0; i < ChoiceButtons.Length; i++)
            {
                var btn = ChoiceButtons[i];
                if (btn == null) continue;
                // テキストは TextMeshPro
                var ChoiceTxet = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (ChoiceTxet  != null)
                {
                    ChoiceTxet .text = (currentQuestion != null && currentQuestion.choices != null && i < currentQuestion.choices.Length) ? currentQuestion.choices[i] : "";
                }

                // ボタンの挙動をセット（多重登録回避）
                btn.onClick.RemoveAllListeners();
                int idx = i; // capture
                btn.onClick.AddListener(() => OnButton(idx));
                btn.interactable = true;
            }
        }

        QuizUI.SetActive(true);
    }

    // ボタンが押されたときの処理
    private void OnButton(int index)
    {
        if (currentQuestion == null) return;

        // 回答後はボタンを全て無効化して多重クリックを防止
        if (ChoiceButtons != null)
        {
            foreach (var b in ChoiceButtons) if (b != null) b.interactable = false;
        }

        // フィードバックを表示してから正誤フラグをセットし UI を閉じる
        StartCoroutine(PlayFeedbackAndSetResult(index));
    }

    // フィードバックを表示してから結果を確定させるコルーチン
    private IEnumerator PlayFeedbackAndSetResult(int index)
    {
        bool isCorrect = (currentQuestion != null && index == currentQuestion.correctIndex);

        // アイコン表示
        if (isCorrect)
        {
            if (CorrectIcon != null) CorrectIcon.SetActive(true);
        }
        else
        {
            if (WrongIcon != null) WrongIcon.SetActive(true);
        }

        // 少し待って視認させる（0.8秒）
        yield return new WaitForSeconds(0.8f);

        // フラグを確定
        CorrectAnswer = isCorrect;
        WrongAnswer = !isCorrect;

        // アイコンを非表示に戻す
        if (CorrectIcon != null) CorrectIcon.SetActive(false);
        if (WrongIcon != null) WrongIcon.SetActive(false);

        // UI を閉じる
        if (QuizUI != null) QuizUI.SetActive(false);

        yield break;
    }

    // プレイヤーの回答が出るまで待つコルーチン（GameStateMachine から使用）
    // タイムアウト秒数を渡すと、その時間で強制的に終了します（0 以下で無制限）。
    public IEnumerator WaitForAnswerRoutine(float timeoutSeconds = 0f)
    {
        float start = Time.time;
        // 待ち：CorrectAnswer または WrongAnswer がセットされるまで
        while (!CorrectAnswer && !WrongAnswer)
        {
            if (timeoutSeconds > 0f && Time.time - start >= timeoutSeconds)
            {
                // タイムアウト: 不正解扱いにするか、どちらでも良いがここでは不正解にする
                WrongAnswer = true;
                break;
            }
            yield return null;
        }
        yield break;
    }

    // QuizData からランダムな問題を取得する（存在しなければ null を返す）
    public QuizData.QuestionData GetRandomQuestion()
    {
        if (QuizData == null || QuizData.questions == null || QuizData.questions.Count == 0) return null;
        return QuizData.questions[UnityEngine.Random.Range(0, QuizData.questions.Count)];
    }

    // インデックス指定で問題を取得（範囲外なら null）
    public QuizData.QuestionData GetQuestion(int index)
    {
        if (QuizData == null || QuizData.questions == null) return null;
        if (index < 0 || index >= QuizData.questions.Count) return null;
        return QuizData.questions[index];
    }

    // 現在の問題のデータを返す（出題前は null）
    //public QuizData.QuestionData GetCurrentQuestion() => currentQuestion;

}