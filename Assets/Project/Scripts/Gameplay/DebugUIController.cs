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
    public TMP_Text infoText;              // 現在マス/状態表示

    void Awake()
    {
        if (token == null) token = FindObjectOfType<TokenMover>();
        if (dice  == null) dice  = FindObjectOfType<DiceUIController>();
        if (gsm   == null) gsm   = FindObjectOfType<GameStateMachine>();

        if (useFixedToggle != null)
        {
            useFixedToggle.isOn = dice ? dice.debugUseFixed : false;
            useFixedToggle.onValueChanged.AddListener(v => { if (dice) dice.debugUseFixed = v; });
        }

        if (fixedValueInput != null)
        {
            int init = (dice != null) ? dice.debugFixedValue : 6;
            fixedValueInput.text = init.ToString();
            fixedValueInput.onEndEdit.AddListener(s =>
            {
                if (dice == null) return;
                if (int.TryParse(s, out int v))
                    dice.debugFixedValue = Mathf.Clamp(v, 1, 6);
                fixedValueInput.text = dice.debugFixedValue.ToString();
            });
        }

        if (stepPlusBtn != null)  stepPlusBtn.onClick.AddListener(OnStepPlus);
        if (stepMinusBtn != null) stepMinusBtn.onClick.AddListener(OnStepMinus);
    }

    void Update()
    {
        // 表示を更新
        if (infoText != null && token != null)
        {
            int idx = token.currentIndex;
            int total = (token.board != null) ? token.board.Count : 0;
            string state = gsm ? gsm.State.ToString() : "(no GSM)";
            infoText.text = $"Index: {idx}/{Mathf.Max(0,total-1)}   State: {state}";
        }

        // 移動中はステップボタンを無効化
        bool canPress = token != null && !token.isMoving;
        if (gsm) canPress &= (gsm.State != GameStateMachine.GameState.End);
        if (stepPlusBtn)  stepPlusBtn.interactable  = canPress;
        if (stepMinusBtn) stepMinusBtn.interactable = canPress;
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
}
