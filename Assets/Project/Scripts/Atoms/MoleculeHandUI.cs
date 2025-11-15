using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Linq;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// 分子手札UI（カード選択→Use可能）
    /// ・カードを1枚だけ選択
    /// ・選択中のみUseボタンが有効
    /// ・Use押下で MoleculeEffectService に Armed 設定（消費は効果適用時）
    /// 表示ON/OFFは HandSwitchUI 側で管理する前提
    /// </summary>
    public class MoleculeHandUI : MonoBehaviour
    {
        [Header("UI")]
        public GameObject rootPanel;                 // 表示面（SetActiveは外部で）
        public Transform gridRoot;                   // Grid の Transform
        public MoleculeCardView cardViewPrefab;      // 1枚分のカードUIプレハブ
        public Button useButton;                     // 「使用」ボタン
        public TMP_Text selectedLabel;               // 「〇〇を選択中」等

        [Header("Data")]
        public PlayerMoleculeHand moleculeHand;      // 分子手札

        // 内部
        private readonly List<MoleculeCardView> _views = new();
        private MoleculeCardView _selected;          // 単一選択

        void Awake()
        {
            // ダミー掃除（ランタイム生成に統一）
            if (gridRoot)
            {
                for (int i = gridRoot.childCount - 1; i >= 0; i--)
                    Destroy(gridRoot.GetChild(i).gameObject);
            }

            if (!moleculeHand) moleculeHand = FindObjectOfType<PlayerMoleculeHand>(true);
            if (moleculeHand != null) moleculeHand.OnChanged += OnHandChanged;

            if (useButton)
            {
                useButton.onClick.AddListener(OnClickUse);
                useButton.interactable = false; // 初期は無効
            }

            UpdateSelectedLabel(null);
        }

        void OnDestroy()
        {
            if (moleculeHand != null) moleculeHand.OnChanged -= OnHandChanged;
            if (useButton) useButton.onClick.RemoveListener(OnClickUse);
        }

        // HandSwitchUI から呼ばれる表示更新
        public void Refresh()
        {
            if (!rootPanel || !rootPanel.activeInHierarchy) return;
            if (!moleculeHand || !gridRoot || !cardViewPrefab) return;

            BuildViews();
            // 選択の整合性（手札変更で消えた場合は解除）
            if (_selected && _selected.Data && !moleculeHand.Cards.Contains(_selected.Data))
            {
                _selected = null;
            }
            ApplySelectionVisuals();
            UpdateUseButton();
            UpdateSelectedLabel(_selected);
        }

        // 手札変更イベント
        private void OnHandChanged()
        {
            // 表示中のみ即反映
            if (rootPanel && rootPanel.activeInHierarchy) Refresh();
        }

        private void BuildViews()
        {
            // 既存破棄
            foreach (Transform ch in gridRoot) Destroy(ch.gameObject);
            _views.Clear();

            var cards = moleculeHand.Cards;
            for (int i = 0; i < cards.Count; i++)
            {
                var view = Instantiate(cardViewPrefab, gridRoot);
                view.Bind(cards[i]);

                // MoleculeCardView 側の選択トグル通知を購読
                view.OnToggled = OnCardToggled;

                // 既存選択の復元
                bool sel = (_selected && _selected.Data == cards[i]);
                view.SetSelected(sel);
                if (sel) _selected = view;

                _views.Add(view);
            }

            // レイアウト即時更新
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)gridRoot);
        }

        // MoleculeCardView からのコールバック（単一選択に統一）
        private void OnCardToggled(MoleculeCardView view, bool on)
        {
            if (on)
            {
                // 他を全解除してこの1枚だけON
                foreach (var v in _views)
                {
                    if (v != view) v.SetSelected(false);
                }
                _selected = view;
            }
            else
            {
                if (_selected == view) _selected = null;
            }

            ApplySelectionVisuals();
            UpdateUseButton();
            UpdateSelectedLabel(_selected);
        }

        private void ApplySelectionVisuals()
        {
            foreach (var v in _views)
            {
                bool sel = (_selected != null && v == _selected);
                v.SetSelected(sel);
            }
        }

        private void UpdateUseButton()
        {
            if (!useButton) return;
            // ★選択しているときのみ有効
            useButton.interactable = (_selected != null && _selected.Data != null);
        }

        private void UpdateSelectedLabel(MoleculeCardView sel)
        {
            if (!selectedLabel) return;
            if (sel && sel.Data)
                selectedLabel.text = $"{sel.Data.nameJP} を選択中";
            else
                selectedLabel.text = "選択なし";
        }

        private void ClearSelection()
        {
            _selected = null;
            ApplySelectionVisuals();
            UpdateUseButton();
            UpdateSelectedLabel(null);
        }

        // 「使用」押下：Armed 設定のみ（消費は効果適用時に MoleculeEffectService 側で行う）
        private void OnClickUse()
        {
            if (_selected == null || _selected.Data == null)
            {
                Debug.LogWarning("[MoleculeHandUI] 使用カードが選択されていません");
                return;
            }

            var card = _selected.Data;
            if (MoleculeEffectService.Instance != null)
            {
                MoleculeEffectService.Instance.Arm(card);
                Debug.Log($"[MoleculeHandUI] 使用準備: {card.nameJP}（{card.formula}）");
            }
            else
            {
                Debug.LogWarning("[MoleculeHandUI] MoleculeEffectService が見つかりません");
            }

            // 選択は残しておいても良いが、誤操作防止で解除するなら下を有効化
            // ClearSelection();
        }
    }
}
