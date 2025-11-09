using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.Atoms
{
    // 1枚のカードのUI
    public class AtomCardView : MonoBehaviour
    {
        public Image image;
        public TMP_Text symbolText;
        public TMP_Text nameText;
        AtomCardDef _data;

        public void Bind(AtomCardDef data)
        {
            _data = data;
            if (image) image.sprite = data ? data.cardSprite : null;
            if (symbolText) symbolText.text = data ? data.symbolJP : "";
            if (nameText) nameText.text = data ? data.nameJP : "";
        }
    }
}
