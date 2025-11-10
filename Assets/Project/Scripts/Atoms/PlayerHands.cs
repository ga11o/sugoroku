using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Atoms
{
    // プレイヤーが所有する原子カードの手札
    public class PlayerHand : MonoBehaviour
    {
        [Tooltip("初期手札（テスト用に Inspector から設定可）")]
        public List<AtomCardDef> initialCards = new List<AtomCardDef>();

        [SerializeField] private List<AtomCardDef> _cards = new List<AtomCardDef>();
        public IReadOnlyList<AtomCardDef> Cards => _cards;

        void Awake()
        {
            _cards.Clear();
            if (initialCards != null) _cards.AddRange(initialCards);
        }

        // 取得
        public void Add(AtomCardDef card) { if (card) _cards.Add(card); }

        // 破棄（1枚だけ）
        public bool RemoveOne(AtomCardDef card)
        {
            return _cards.Remove(card);
        }

        // 複製（同じカードを1枚追加）
        public void Duplicate(AtomCardDef card)
        {
            if (card) _cards.Add(card);
        }

        // 全破棄
        public void Clear() => _cards.Clear();
    }
}
