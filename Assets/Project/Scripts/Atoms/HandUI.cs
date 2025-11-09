using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// 手札UI（ボタンで常時トグル開閉）
    /// </summary>
    public class HandUI : MonoBehaviour
    {
        [Header("UI Elements")]
        public GameObject rootPanel;           // 手札パネル（開閉対象）
        public Transform gridRoot;             // GridLayoutGroup の Transform
        public AtomCardView cardViewPrefab;    // カード表示用プレハブ
        public PlayerHand playerHand;          // プレイヤーの手札データ
        public GameStateMachine gsm; // (拡張用)現在のプレイヤーは、 GameStateMachine から取得する必要がある

        [Header("Toggle Button")]
        public Button toggleButton;            // トグルボタン
        public TMP_Text toggleButtonText;      // ボタンラベル
        public string openText = "手札を閉じる";
        public string closedText = "手札を開く";

        private readonly List<AtomCardView> _pool = new();

        void Awake()
        {
            if (!gsm) gsm = FindObjectOfType<GameStateMachine>();

            // 起動時は閉じた状態
            if (rootPanel) rootPanel.SetActive(false);

            // ボタン設定
            if (toggleButton)
            {
                toggleButton.onClick.AddListener(OnToggleButtonClicked);
            }

            UpdateButtonLabel();
        }

        private void OnDestroy()
        {
            if (toggleButton)
                toggleButton.onClick.RemoveListener(OnToggleButtonClicked);
        }

        private void OnToggleButtonClicked()
        {
            if (!rootPanel) return;

            bool nextState = !rootPanel.activeSelf;
            rootPanel.SetActive(nextState);
            UpdateButtonLabel();

            if (nextState) Refresh();
        }

        private void UpdateButtonLabel()
        {
            if (toggleButtonText)
            {
                toggleButtonText.text = rootPanel && rootPanel.activeSelf ? openText : closedText;
            }
        }

        public void Refresh()
        {
            playerHand = gsm.CurrentPlayer.atomHand;
            if (!playerHand || !gridRoot || !cardViewPrefab) return;

            var cards = playerHand.Cards;
            EnsurePool(cards.Count);

            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < cards.Count;
                _pool[i].gameObject.SetActive(active);
                if (active)
                    _pool[i].Bind(cards[i]);
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)gridRoot);
        }

        private void EnsurePool(int need)
        {
            while (_pool.Count < need)
            {
                var v = Instantiate(cardViewPrefab, gridRoot);
                _pool.Add(v);
            }
        }
    }
}
