


using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 簡易クイズ管理クラス。
/// ランタイムで簡単な UI を生成して問題を出題する。正解/不正解はコールバックで返す。
/// </summary>
public class QuizManager : MonoBehaviour
{
    public static QuizManager Instance { get; private set; }

    [Serializable]
    public class QuizQuestion
    {
        public string question;
        public string[] options;
        public int correctIndex;
    }

    [Header("問題一覧 (デフォルトは化学に関する簡単な問題)")]
    public List<QuizQuestion> questions = new List<QuizQuestion>()
    {
        new QuizQuestion { question = "水の化学式はどれ？", options = new[]{ "H2O", "CO2", "O2", "NaCl" }, correctIndex = 0 },
        new QuizQuestion { question = "酸は pH が何より小さい？", options = new[]{ "7", "14", "0", "1" }, correctIndex = 0 },
        new QuizQuestion { question = "元素の周期表で 'Na' は何の元素？", options = new[]{ "ナトリウム", "酸素", "窒素", "炭素" }, correctIndex = 0 },
    };

    // UI 用オブジェクト参照（生成時に作る）
    private GameObject dialogRoot;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
    }

    /// <summary>
    /// ランダムな問題を出題して、回答結果をコールバックで受け取るコルーチン。
    /// </summary>
    public IEnumerator AskRandomQuestionRoutine(Action<bool> onComplete)
    {
        if (questions == null || questions.Count == 0)
        {
            onComplete?.Invoke(false);
            yield break;
        }

        var q = questions[UnityEngine.Random.Range(0, questions.Count)];

        bool answered = false;
        bool isCorrect = false;

        CreateDialog(q, (selectedIndex) => {
            answered = true;
            isCorrect = (selectedIndex == q.correctIndex);
        });

        // 回答されるまで待機
        while (!answered) yield return null;

        // ダイアログ破棄
        if (dialogRoot != null) Destroy(dialogRoot);

        onComplete?.Invoke(isCorrect);
    }

    void CreateDialog(QuizQuestion q, Action<int> onSelected)
    {
        // EventSystem が無ければ作る
        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }

        // Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        GameObject canvasGO = null;
        if (canvas == null)
        {
            canvasGO = new GameObject("QuizCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
            DontDestroyOnLoad(canvasGO);
        }
        else
        {
            canvasGO = canvas.gameObject;
        }

        dialogRoot = new GameObject("QuizDialog");
        dialogRoot.transform.SetParent(canvasGO.transform, false);

        var panel = new GameObject("Panel");
        panel.transform.SetParent(dialogRoot.transform, false);
        var img = panel.AddComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.75f);
        var rt = panel.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.1f, 0.25f);
        rt.anchorMax = new Vector2(0.9f, 0.75f);
        rt.offsetMin = rt.offsetMax = Vector2.zero;

        // Question Text
        var qGO = new GameObject("QuestionText");
        qGO.transform.SetParent(panel.transform, false);
        var qText = qGO.AddComponent<Text>();
        qText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        qText.alignment = TextAnchor.UpperCenter;
        qText.fontSize = 28;
        qText.color = Color.white;
        qText.text = q.question;
        var qrt = qGO.GetComponent<RectTransform>();
        qrt.anchorMin = new Vector2(0.05f, 0.6f);
        qrt.anchorMax = new Vector2(0.95f, 0.95f);
        qrt.offsetMin = qrt.offsetMax = Vector2.zero;

        // Buttons
        int optionCount = Mathf.Min(q.options.Length, 4);
        for (int i = 0; i < optionCount; i++)
        {
            var bGO = new GameObject($"Option_{i}");
            bGO.transform.SetParent(panel.transform, false);
            var bImg = bGO.AddComponent<Image>();
            bImg.color = new Color(1f, 1f, 1f, 0.9f);
            var btn = bGO.AddComponent<Button>();

            int index = i; // capture
            btn.onClick.AddListener(() => {
                onSelected?.Invoke(index);
            });

            var btTextGO = new GameObject("Text");
            btTextGO.transform.SetParent(bGO.transform, false);
            var btText = btTextGO.AddComponent<Text>();
            btText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            btText.alignment = TextAnchor.MiddleCenter;
            btText.color = Color.black;
            btText.text = q.options[i];

            var brt = bGO.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0.1f, 0.45f - i * 0.12f);
            brt.anchorMax = new Vector2(0.9f, 0.55f - i * 0.12f);
            brt.offsetMin = brt.offsetMax = Vector2.zero;

            var tRt = btTextGO.GetComponent<RectTransform>();
            tRt.anchorMin = Vector2.zero;
            tRt.anchorMax = Vector2.one;
            tRt.offsetMin = Vector2.zero;
            tRt.offsetMax = Vector2.zero;
        }
    }
}
