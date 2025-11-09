using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sugoroku.Atoms
{
    public enum HandSortMode { AtomicNumberAsc, SymbolAsc, NameAsc }

    /// <summary>
    /// 原子カードの所持管理（自動ソート、変更通知つき）
    /// </summary>
    public class PlayerHand : MonoBehaviour
    {
        [Tooltip("初期手札（テスト用）")]
        public List<AtomCardDef> initialCards = new();

        [Header("Sort")]
        public bool autoSortOnAdd = true;
        public HandSortMode sortMode = HandSortMode.AtomicNumberAsc;

        [SerializeField] private List<AtomCardDef> _cards = new();
        public IReadOnlyList<AtomCardDef> Cards => _cards;

        /// <summary>
        /// 手札の変化を通知するイベント
        /// </summary>
        public event Action OnChanged;

        void Awake()
        {
            _cards = new List<AtomCardDef>(initialCards ?? new List<AtomCardDef>());
            if (autoSortOnAdd) SortInPlace();
        }

        public void Add(AtomCardDef card)
        {
            if (!card) return;
            _cards.Add(card);
            if (autoSortOnAdd) SortInPlace();
            OnChanged?.Invoke();
        }

        public void Duplicate(AtomCardDef card)
        {
            if (!card) return;
            _cards.Add(card);
            if (autoSortOnAdd) SortInPlace();
            OnChanged?.Invoke();
        }

        public bool RemoveOne(AtomCardDef card)
        {
            bool ok = _cards.Remove(card);
            if (ok) OnChanged?.Invoke();
            return ok;
        }

        /// <summary>
        /// 複数のカードをまとめて削除
        /// </summary>
        public void RemoveMany(IEnumerable<AtomCardDef> cards)
        {
            if (cards == null) return;
            bool changed = false;
            foreach (var c in cards.ToList())
                changed |= _cards.Remove(c);
            if (changed) OnChanged?.Invoke();
        }

        public void Clear()
        {
            if (_cards.Count == 0) return;
            _cards.Clear();
            OnChanged?.Invoke();
        }

        public void SortInPlace()
        {
            switch (sortMode)
            {
                case HandSortMode.AtomicNumberAsc:
                    _cards = _cards.OrderBy(c => c.atomicNumber).ThenBy(c => c.symbolJP).ToList();
                    break;
                case HandSortMode.SymbolAsc:
                    _cards = _cards.OrderBy(c => c.symbolJP).ThenBy(c => c.atomicNumber).ToList();
                    break;
                case HandSortMode.NameAsc:
                    _cards = _cards.OrderBy(c => c.nameJP).ThenBy(c => c.atomicNumber).ToList();
                    break;
            }
        }
    }
}
