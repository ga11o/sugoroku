using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// 1枚の原子カードUI（クリック選択トグル＋ハイライト）
    /// </summary>
    public class AtomCardView : MonoBehaviour
    {
        [Header("UI")]
        public Image image;
        public TMP_Text symbolText;
        public TMP_Text nameText;

        [Header("Selection")]
        public Button clickArea;        // カード全体のButton
        public Image selectionOverlay;  // 単色オーバーレイ（半透明）

        public AtomCardDef Data { get; private set; }
        public bool Selected { get; private set; }

        /// <summary>選択状態が変わった時に HandUI へ通知</summary>
        public System.Action<AtomCardView, bool> OnToggled;

        void Awake()
        {
            if (clickArea) clickArea.onClick.AddListener(ToggleSelect);

            // 保険：Overlayを親いっぱい・最前面・初期OFFに
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

        public void Bind(AtomCardDef data)
        {
            Data = data;
            if (image) image.sprite = data ? data.cardSprite : null;
            if (symbolText) symbolText.text = data ? data.symbolJP : "";
            if (nameText)   nameText.text   = data ? data.nameJP   : "";
        }

        public void ToggleSelect()
        {
            SetSelected(!Selected);
            OnToggled?.Invoke(this, Selected);
            Debug.Log($"[Card Click] {Data?.symbolJP ?? "None"} → Selected = {Selected}");
        }

        public void SetSelected(bool on)
        {
            Selected = on;
            if (selectionOverlay) selectionOverlay.enabled = on;
        }
    }
}
