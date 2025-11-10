using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// 1枚の分子カードUI（単一選択用のハイライト付き）
    /// </summary>
    public class MoleculeCardView : MonoBehaviour
    {
        [Header("UI")]
        public Image image;
        public TMP_Text nameText;
        public TMP_Text formulaText;

        [Header("Selection")]
        public Button clickArea;        // 全面ボタン
        public Image selectionOverlay;  // 半透明オーバーレイ

        public MoleculeCardDef Data { get; private set; }
        public bool Selected { get; private set; }

        public System.Action<MoleculeCardView, bool> OnToggled;

        void Awake()
        {
            if (clickArea) clickArea.onClick.AddListener(ToggleSelect);

            if (selectionOverlay)
            {
                var rt = selectionOverlay.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
                selectionOverlay.raycastTarget = false;
                selectionOverlay.transform.SetAsLastSibling();
                selectionOverlay.enabled = false;
            }
            SetSelected(false);
        }

        public void Bind(MoleculeCardDef data)
        {
            Data = data;
            if (image)       image.sprite  = data ? data.cardSprite : null;
            if (nameText)    nameText.text = data ? data.nameJP     : "";
            if (formulaText) formulaText.text = data ? data.formula : "";
        }

        public void ToggleSelect()
        {
            SetSelected(!Selected);
            OnToggled?.Invoke(this, Selected);
        }

        public void SetSelected(bool on)
        {
            Selected = on;
            if (selectionOverlay) selectionOverlay.enabled = on;
        }
    }
}
