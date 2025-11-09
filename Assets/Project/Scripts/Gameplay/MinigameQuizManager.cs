using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

// 4択の化学クイズを表示するシンプルなシングルトンマネージャ
// 使用方法（GameStateMachine から）：
// yield return MinigameQuizManager.Instance.AskRandomQuestionRoutine(callback);
public class MinigameQuizManager : MonoBehaviour
{
    public static MinigameQuizManager Instance { get; private set; }

    // プレイヤーが正答したときに呼ばれる。Inspector からハンドラを割り当て可能で柔軟な処理ができる。
    public UnityEvent OnCorrectAnswer;

    [Tooltip("任意: クイズ UI を上に表示するための Canvas のソート順")]
    public int canvasSortOrder = 5000;

    // 内部で使う UI 参照
    private Canvas quizCanvas;
    private Text questionText;
    private Button[] choiceButtons = new Button[4];

    private List<Question> questions = new List<Question>();

    [Serializable]
    private class Question
    {
        public string question;
        public string[] choices = new string[4];
        public int correctIndex;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        BuildDefaultQuestions();
    // UI の構築を try/catch の中で行い、UI 構築に失敗してもインスタンスが壊れた状態で残らないようにする
        try
        {
            BuildUI();
            Hide();
        }
        catch (Exception ex)
        {
            Debug.LogError("MinigameQuizManager: UI build failed: " + ex.Message + "\n" + ex.StackTrace);
            // インスタンスは残すが UI が存在しない可能性がある。UI がない場合は AskRandomQuestionRoutine が対処する。
        }
    }

    void BuildDefaultQuestions()
    {
        // 簡単な化学の問題をいくつか用意。後で JSON 等から読み込むよう拡張可能。
        questions.Add(new Question {
            question = "水の化学式は？",
            choices = new string[]{"H2O", "CO2", "O2", "NaCl"},
            correctIndex = 0
        });

        questions.Add(new Question {
            question = "原子番号1はどの元素？",
            choices = new string[]{"ヘリウム", "リチウム", "水素", "炭素"},
            correctIndex = 2
        });

        questions.Add(new Question {
            question = "塩酸の主成分は？",
            choices = new string[]{"HCl", "H2SO4", "NaOH", "CH4"},
            correctIndex = 0
        });

        questions.Add(new Question {
            question = "pHが7より小さい溶液は？",
            choices = new string[]{"中性", "塩基性", "酸性", "不活性"},
            correctIndex = 2
        });
    }

    void BuildUI()
    {
        Debug.Log("MinigameQuizManager: BuildUI start");
        try
        {
    // ルート Canvas を作成
        var goCanvas = new GameObject("MinigameQuizCanvas");
        goCanvas.transform.SetParent(this.transform, false);
        quizCanvas = goCanvas.AddComponent<Canvas>();
        quizCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        quizCanvas.sortingOrder = canvasSortOrder;
        var cs = goCanvas.AddComponent<CanvasScaler>();
    // 画面解像度に合わせて UI をスケーリングする設定
        cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        cs.referenceResolution = new Vector2(1920, 1080);
        cs.matchWidthOrHeight = 0.5f;
        goCanvas.AddComponent<GraphicRaycaster>();

    Debug.Log("MinigameQuizManager: created canvas");
    // 背景パネル（ゲームへのクリックをブロックするため）
    var bg = new GameObject("BG");
    bg.transform.SetParent(goCanvas.transform, false);
    var img = bg.AddComponent<Image>();
    if (img == null) throw new Exception("Failed to create Image component for BG");
    Debug.Log("MinigameQuizManager: created BG image");
    img.color = new Color(0f, 0f, 0f, 0.5f);
    // ensure bg has a sprite so it renders correctly on all Unity versions
    var defaultSprite = Resources.GetBuiltinResource<Sprite>("UI/Skin/UISprite.psd");
    if (defaultSprite != null) img.sprite = defaultSprite;
    var rect = img.rectTransform;
    if (rect == null) throw new Exception("BG Image rectTransform is null");
    rect.anchorMin = Vector2.zero;
    rect.anchorMax = Vector2.one;
    rect.offsetMin = rect.offsetMax = Vector2.zero;

    // 中央のパネル
    var panel = new GameObject("Panel");
    panel.transform.SetParent(goCanvas.transform, false);
    var panelImg = panel.AddComponent<Image>();
    if (panelImg == null) throw new Exception("Failed to create Image component for Panel");
    Debug.Log("MinigameQuizManager: created Panel image");
    panelImg.color = new Color(1f, 1f, 1f, 0.95f);
    if (defaultSprite != null) panelImg.sprite = defaultSprite;
    var pr = panelImg.rectTransform;
    if (pr == null) throw new Exception("Panel Image rectTransform is null");
    pr.sizeDelta = new Vector2(600, 320);
    pr.anchorMin = pr.anchorMax = new Vector2(0.5f, 0.5f);
    pr.anchoredPosition = Vector2.zero;

    Debug.Log("MinigameQuizManager: setup panel layout");
    // 縦方向レイアウト
        var layout = panel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.padding = new RectOffset(12,12,12,12);
        layout.spacing = 8;

    // 問題文用のテキスト
    var qObj = new GameObject("QuestionText");
        qObj.transform.SetParent(panel.transform, false);
        questionText = qObj.AddComponent<Text>();
        if (questionText == null) throw new Exception("Failed to create Question Text component");
    Debug.Log("MinigameQuizManager: created QuestionText");
    // Unity の組み込みフォント（環境によって存在しない場合がある）を試す
    var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (font == null)
        {
            // fallback to any dynamic font on the system
            Debug.LogWarning("MinigameQuizManager: LegacyRuntime.ttf not found via Resources.GetBuiltinResource; attempting OS font fallback.");
            var osFont = Font.CreateDynamicFontFromOSFont("Arial", 36);
            if (osFont == null) throw new Exception("Failed to acquire fallback OS font");
            questionText.font = osFont;
        }
        else
        {
            questionText.font = font;
        }
        questionText.fontSize = 36;
        questionText.alignment = TextAnchor.MiddleCenter;
        questionText.color = Color.black;
        questionText.horizontalOverflow = HorizontalWrapMode.Wrap;
        questionText.verticalOverflow = VerticalWrapMode.Truncate;
        // allow the text to resize a bit to fit the panel
        questionText.resizeTextForBestFit = true;
        questionText.resizeTextMinSize = 18;
        questionText.resizeTextMaxSize = 48;
        var qrt = questionText.rectTransform;
        if (qrt == null) throw new Exception("QuestionText rectTransform is null");
        qrt.sizeDelta = new Vector2(560, 100);

    // 選択肢を格納するコンテナ
        var choicesContainer = new GameObject("Choices");
        choicesContainer.transform.SetParent(panel.transform, false);
        var chLayout = choicesContainer.AddComponent<VerticalLayoutGroup>();
        chLayout.childForceExpandHeight = false;
        chLayout.childForceExpandWidth = true;
        chLayout.spacing = 6;
    var chRt = choicesContainer.GetComponent<RectTransform>();
    if (chRt == null) chRt = choicesContainer.AddComponent<RectTransform>();
    chRt.sizeDelta = new Vector2(560, 200);

    // 選択肢ボタン配列を初期化
    choiceButtons = new Button[4];
        for (int i = 0; i < 4; i++)
        {
            var btnGO = new GameObject("ChoiceButton" + i);
            btnGO.transform.SetParent(choicesContainer.transform, false);
            var btnImg = btnGO.AddComponent<Image>();
            // ensure button image has a sprite so the rectangle is visible
            if (defaultSprite != null) btnImg.sprite = defaultSprite;
            btnImg.color = new Color(0.9f, 0.9f, 0.9f, 1f);
            var btn = btnGO.AddComponent<Button>();
            Debug.Log("MinigameQuizManager: created ChoiceButton" + i);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(540, 40);

            // VerticalLayoutGroup がボタンサイズを安定して扱えるよう LayoutElement を追加
            var le = btnGO.AddComponent<LayoutElement>();
            le.preferredHeight = 40f;
            le.preferredWidth = 540f;

            var txtGO = new GameObject("Text");
            txtGO.transform.SetParent(btnGO.transform, false);
            var txt = txtGO.AddComponent<Text>();
            if (txt == null) throw new Exception("Failed to create Text component for choice button");
            var choiceFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (choiceFont == null)
            {
                choiceFont = Font.CreateDynamicFontFromOSFont("Arial", 20);
                if (choiceFont == null) Debug.LogWarning("MinigameQuizManager: choice font fallback failed; text may not render");
            }
            txt.font = choiceFont;
            txt.fontSize = 20;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = Color.black;
            txt.rectTransform.anchorMin = Vector2.zero;
            txt.rectTransform.anchorMax = Vector2.one;
            txt.rectTransform.offsetMin = txt.rectTransform.offsetMax = Vector2.zero;
            txt.horizontalOverflow = HorizontalWrapMode.Wrap;
            txt.verticalOverflow = VerticalWrapMode.Truncate;
            txt.resizeTextForBestFit = true;
            txt.resizeTextMinSize = 14;
            txt.resizeTextMaxSize = 24;
            choiceButtons[i] = btn;
        }
        Debug.Log("MinigameQuizManager: BuildUI finished, buttons count=" + choiceButtons.Length);
        }
        catch(Exception e)
        {
            Debug.LogError("MinigameQuizManager: BuildUI exception: " + e.Message + "\n" + e.StackTrace);
            throw;
        }
    }

    void Show()
    {
        if (quizCanvas != null) quizCanvas.gameObject.SetActive(true);
    }

    void Hide()
    {
        if (quizCanvas != null) quizCanvas.gameObject.SetActive(false);
    }

    // GameStateMachine から使われる公開コルーチン。ランダムな問題を出題し、正解なら onComplete(true)、不正解なら onComplete(false) を呼ぶ。
    public IEnumerator AskRandomQuestionRoutine(Action<bool> onComplete)
    {
        if (questions.Count == 0)
        {
            onComplete?.Invoke(false);
            yield break;
        }

    // ランダムな問題を選択
        var q = questions[UnityEngine.Random.Range(0, questions.Count)];

    // UI が存在するか確認。なければ BuildUI を試みる
        if (quizCanvas == null || questionText == null || choiceButtons == null)
        {
            try
            {
                BuildUI();
            }
            catch (Exception ex)
            {
                Debug.LogError("MinigameQuizManager: Failed to build UI in AskRandomQuestionRoutine: " + ex.Message);
                onComplete?.Invoke(false);
                yield break;
            }
        }

        Show();

    // UI 要素の参照が設定されているか確認。BuildUI が正常に動作していれば割り当て済みのはずだが、
    // 部分的に失敗している場合は Canvas 以下を名前で検索して補充する。
        if (questionText == null)
        {
            var qtTrans = quizCanvas != null ? quizCanvas.transform.Find("Panel/QuestionText") : null;
            if (qtTrans != null) questionText = qtTrans.GetComponent<Text>();
            if (questionText == null) Debug.LogWarning("MinigameQuizManager: questionText still null after search");
        }

        for (int i = 0; i < 4; i++)
        {
            if (choiceButtons == null) choiceButtons = new Button[4];
            if (choiceButtons[i] == null && quizCanvas != null)
            {
                var btnTrans = quizCanvas.transform.Find($"Panel/Choices/ChoiceButton{i}");
                if (btnTrans != null)
                {
                    var btnComp = btnTrans.GetComponent<Button>();
                    if (btnComp != null) choiceButtons[i] = btnComp;
                }
            }
        }

    // 検索しても選択肢ボタンが見つからなければ、ログを出して安全に終了する
        bool anyButtons = false;
        for (int i = 0; i < 4; i++) if (choiceButtons != null && choiceButtons[i] != null) anyButtons = true;
        if (!anyButtons)
        {
            Debug.LogError("MinigameQuizManager: no choice buttons available after BuildUI/search; aborting quiz.");
            onComplete?.Invoke(false);
            yield break;
        }

        // UI に問題文と選択肢を設定
        questionText.text = q.question;
        bool answered = false;
        bool correct = false;

        for (int i = 0; i < 4; i++)
        {
            int idx = i; // ローカル変数にキャプチャ
            var btn = choiceButtons[i];
            if (btn == null)
            {
                Debug.LogWarning("MinigameQuizManager: choiceButtons[" + i + "] is null");
                continue;
            }
            var txt = btn.GetComponentInChildren<Text>();
            if (txt != null) txt.text = q.choices.Length > i ? q.choices[i] : "";
            btn.interactable = true;
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                if (answered) return;
                answered = true;
                correct = (idx == q.correctIndex);
                // 多重クリックを防ぐため全てのボタンを無効化
                for (int j = 0; j < 4; j++) choiceButtons[j].interactable = false;
                // 簡単な視覚フィードバック（将来拡張可能）
                if (correct)
                {
                    // Inspector から結び付けられたイベントを呼び出す（柔軟な正解時処理）
                    OnCorrectAnswer?.Invoke();
                }
            });
        }

        // プレイヤーの回答があるまで待機
        yield return new WaitUntil(() => answered == true);

        // 任意の短い遅延（結果を見せるため）
        yield return new WaitForSeconds(0.35f);

        Hide();
        onComplete?.Invoke(correct);
    }
}
