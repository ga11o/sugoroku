using System;
using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Atoms
{
    public class PlayerMoleculeHand : MonoBehaviour
    {
        [SerializeField] private List<MoleculeCardDef> _cards = new();
        public IReadOnlyList<MoleculeCardDef> Cards => _cards;

        public event Action OnChanged;

        public void Add(MoleculeCardDef c)
        {
            if (!c) return;
            _cards.Add(c);
            OnChanged?.Invoke();
        }

        public bool RemoveOne(MoleculeCardDef c)
        {
            bool ok = _cards.Remove(c);
            if (ok) OnChanged?.Invoke();
            return ok;
        }
    }
}
