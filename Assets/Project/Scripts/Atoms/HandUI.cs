using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// 原子手札の描画・選択・合成だけを担当（表示の ON/OFF は HandSwitchUI が行う）
    /// </summary>
    public class HandUI : MonoBehaviour
    {
        [Header("Wiring")]
        public GameObject rootPanel;           // 表示面（SetActive は HandSwitchUI が行う）
        public Transform gridRoot;             // GridLayoutGroup の Transform
        public AtomCardView cardViewPrefab;    // 1枚分カードUIプレハブ
        public PlayerHand playerHand;          // 原子の所持
        public GameStateMachine gsm; // (拡張用)現在のプレイヤーは、 GameStateMachine から取得する必要がある

        [Header("Synthesis (任意)")]
        public MoleculeRecipeDatabase recipeDb;
        public PlayerMoleculeHand moleculeHand;      // 生成先（任意）
        public Button synthesizeButton;              // 合成ボタン（任意）
        public TMP_Text synthesizeHintText;          // 「H + H + O」等ヒント（任意）

        // 内部
        private readonly List<AtomCardView> _pool = new();
        private readonly HashSet<AtomCardView> _selected = new();

        void Awake()
        {
            // デザイン時のダミーを掃除（ランタイム生成に統一）
            if (gridRoot)
            {
                for (int i = gridRoot.childCount - 1; i >= 0; i--)
                    Destroy(gridRoot.GetChild(i).gameObject);
            }

            if (!playerHand) playerHand = FindObjectOfType<PlayerHand>(true);
            if (playerHand != null) playerHand.OnChanged += OnHandChanged;

            if (synthesizeButton) synthesizeButton.onClick.AddListener(OnSynthesizeClicked);
        }

        void OnDestroy()
        {
            if (playerHand != null) playerHand.OnChanged -= OnHandChanged;
            if (synthesizeButton) synthesizeButton.onClick.RemoveListener(OnSynthesizeClicked);
        }

        /// <summary>
        /// 所持の変更通知。表示中なら即描画更新（表示のON/OFFは触らない）
        /// </summary>
        private void OnHandChanged()
        {
            _selected.Clear();
            if (rootPanel && rootPanel.activeInHierarchy) Refresh();
        }

        /// <summary>
        /// 現在の所持に基づいて UI を再構築（表示のON/OFFは触らない）
        /// </summary>
        public void Refresh()
        {
            if (!rootPanel || !rootPanel.activeInHierarchy) return;
            if (!playerHand || !gridRoot || !cardViewPrefab) return;

            var cards = playerHand.Cards;
            EnsurePool(cards.Count);

            for (int i = 0; i < _pool.Count; i++)
            {
                bool active = i < cards.Count;
                _pool[i].gameObject.SetActive(active);
                if (!active) continue;

                _pool[i].Bind(cards[i]);
                _pool[i].OnToggled = OnCardToggled;
                _pool[i].SetSelected(_selected.Contains(_pool[i]));
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)gridRoot);
            UpdateSynthesizeInteractable();
            UpdateHint();
        }

        private void EnsurePool(int need)
        {
            while (_pool.Count < need)
            {
                var v = Instantiate(cardViewPrefab, gridRoot);
                _pool.Add(v);
            }
        }

        // ---- 選択管理 ----
        private void OnCardToggled(AtomCardView view, bool on)
        {
            if (on) _selected.Add(view);
            else    _selected.Remove(view);

            UpdateSynthesizeInteractable();
            UpdateHint();
        }

        private List<AtomCardDef> GetSelectedAtoms()
        {
            var list = new List<AtomCardDef>();
            foreach (var v in _selected)
                if (v && v.Data) list.Add(v.Data);
            return list;
        }

        // ---- 合成UI状態 ----
        private void UpdateSynthesizeInteractable()
        {
            if (!synthesizeButton) return;
            if (recipeDb == null) { synthesizeButton.interactable = false; return; }
            synthesizeButton.interactable = recipeDb.CanMatchAny(GetSelectedAtoms());
        }

        private void UpdateHint()
        {
            if (!synthesizeHintText) return;
            if (recipeDb == null) { synthesizeHintText.text = ""; return; }

            var match = recipeDb.FirstMatch(GetSelectedAtoms());
            synthesizeHintText.text = (match != null) ? $"→ {match.productDisplayJP}" : "";
        }

        // ---- 合成実行（表示は触らない）----
        private void OnSynthesizeClicked()
        {
            if (recipeDb == null) return;
            var selected = GetSelectedAtoms();

            if (recipeDb.TrySynthesize(selected, out var product, out var consumed))
            {
                playerHand.RemoveMany(consumed);                 // 消費
                if (moleculeHand) moleculeHand.Add(product);     // 生成

                _selected.Clear();
                Refresh();
                Debug.Log($"[Synthesize] 成功: {product.nameJP}（{product.formula}）");
                Sugoroku.UI.MessageManager.Important($"『{product.nameJP}（{product.formula}）』を合成した！");
            }
            else
            {
                Debug.Log("[Synthesize] レシピ不一致");
            }
        }
    }
}
