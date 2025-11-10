using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// カード表示/非表示のトグル。HandSwitchUI と連携して対象の手札を表示。
    /// </summary>
    public class ShowCardToggle : MonoBehaviour
    {
        [Header("参照")]
        public HandSwitchUI handSwitch;     // 上の HandSwitchUI を割当
        public Button toggleButton;         // このボタン自身
        public TMP_Text buttonText;         // ボタンラベル

        [Header("文言")]
        public string openText  = "Show Cards";
        public string closeText = "Hide Cards";

        bool visible = false; // 起動時は非表示

        void Awake()
        {
            if (toggleButton) toggleButton.onClick.AddListener(OnClickToggle);

            // ★ 起動時は非表示を徹底（誤表示対策）
            if (handSwitch?.atomHandUI?.rootPanel)     handSwitch.atomHandUI.rootPanel.SetActive(false);
            if (handSwitch?.moleculeHandUI?.rootPanel) handSwitch.moleculeHandUI.rootPanel.SetActive(false);

            UpdateLabel();
        }

        void OnDestroy()
        {
            if (toggleButton) toggleButton.onClick.RemoveListener(OnClickToggle);
        }

        void OnClickToggle()
        {
            visible = !visible;
            // showingAtoms で選ばれている側だけを表示/非表示
            handSwitch?.ApplyCurrentTargetVisibility(visible);
            UpdateLabel();
        }

        void UpdateLabel()
        {
            if (!buttonText) return;
            buttonText.text = visible ? closeText : openText;
        }
    }
}
