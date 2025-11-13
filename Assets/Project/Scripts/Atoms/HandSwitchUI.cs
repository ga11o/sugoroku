using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// 原子/分子どちらを“表示対象”にするかだけを管理。
    /// 実際の表示/非表示は ShowCardToggle からの呼び出しと、
    /// スイッチ押下時の再適用で行う。
    /// </summary>
    public class HandSwitchUI : MonoBehaviour
    {
        [Header("参照")]
        public HandUI atomHandUI;
        public MoleculeHandUI moleculeHandUI;

        [Header("Switch Button")]
        public Button switchButton;
        public TMP_Text switchButtonText;
        public string toMoleculeText;
        public string toAtomText;

        // 現在どちらを表示対象にするか（表示中とは限らない）
        public bool showingAtoms = true;

        void Awake()
        {
            if (switchButton) switchButton.onClick.AddListener(OnSwitchClicked);

            // 起動時は両方非表示にしておく
            if (atomHandUI?.rootPanel)     atomHandUI.rootPanel.SetActive(false);
            if (moleculeHandUI?.rootPanel) moleculeHandUI.rootPanel.SetActive(false);

            UpdateLabel();
            UpdateSwitchInteractable();
        }

        void OnDestroy()
        {
            if (switchButton) switchButton.onClick.RemoveListener(OnSwitchClicked);
        }

        void Update()
        {
            UpdateSwitchInteractable();
        }

        void OnSwitchClicked()
        {
            showingAtoms = !showingAtoms;
            UpdateLabel();

            // ★ ここが重要：もし現在どちらかが表示中なら、
            //     「可視のまま対象だけ入れ替える」ために再適用する
            if (IsAnyVisible())
            {
                ApplyCurrentTargetVisibility(true);
            }
        }

        void UpdateLabel()
        {
            if (!switchButtonText) return;
            switchButtonText.text = showingAtoms ? toMoleculeText : toAtomText;
        }

        void UpdateSwitchInteractable()
        {
            if (!switchButton) return;
            switchButton.interactable = IsAnyVisible();
        }

        bool IsAnyVisible()
        {
            return (atomHandUI && atomHandUI.rootPanel && atomHandUI.rootPanel.activeSelf)
                || (moleculeHandUI && moleculeHandUI.rootPanel && moleculeHandUI.rootPanel.activeSelf);
        }

        /// <summary>
        /// ShowCardToggle から or 自身のスイッチ押下後に呼ぶ。
        /// visible=true のときは showingAtoms 側だけを ON、もう片方は OFF。
        /// visible=false のときは両方 OFF。
        /// </summary>
        public void ApplyCurrentTargetVisibility(bool visible)
        {
            if (!atomHandUI || !moleculeHandUI) return;

            if (!visible)
            {
                if (atomHandUI.rootPanel)     atomHandUI.rootPanel.SetActive(false);
                if (moleculeHandUI.rootPanel) moleculeHandUI.rootPanel.SetActive(false);
                return;
            }

            if (showingAtoms)
            {
                if (moleculeHandUI.rootPanel) moleculeHandUI.rootPanel.SetActive(false);
                if (atomHandUI.rootPanel)     atomHandUI.rootPanel.SetActive(true);
                atomHandUI.Refresh();
            }
            else
            {
                if (atomHandUI.rootPanel)     atomHandUI.rootPanel.SetActive(false);
                if (moleculeHandUI.rootPanel) moleculeHandUI.rootPanel.SetActive(true);
                moleculeHandUI.Refresh();
            }
        }
    }
}
