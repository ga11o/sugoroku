using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Sugoroku.Atoms
{
    [CreateAssetMenu(menuName = "Sugoroku/Molecule Recipe DB", fileName = "MoleculeRecipes")]
    public class MoleculeRecipeDatabase : ScriptableObject
    {
        [System.Serializable]
        public class Requirement
        {
            public AtomCardDef atom;
            public int count = 1;
        }

        [System.Serializable]
        public class Recipe
        {
            public MoleculeCardDef product;
            [Tooltip("ヒント表示用（例：H + H + O）")]
            public string productDisplayJP;
            public List<Requirement> requires = new();
        }

        public List<Recipe> recipes = new();

        public Recipe FirstMatch(List<AtomCardDef> selected)
        {
            if (selected == null) return null;
            foreach (var r in recipes)
                if (Matches(r, selected)) return r;
            return null;
        }

        public bool CanMatchAny(List<AtomCardDef> selected) => FirstMatch(selected) != null;

        public bool TrySynthesize(List<AtomCardDef> selected,
                                  out MoleculeCardDef product,
                                  out List<AtomCardDef> consumed)
        {
            product = null; consumed = null;
            var r = FirstMatch(selected);
            if (r == null) return false;

            // 実際に消費する個体（同一アセットの枚数ぶん）
            consumed = new List<AtomCardDef>();
            foreach (var req in r.requires)
            {
                int need = req.count;
                foreach (var s in selected.Where(s => s == req.atom))
                {
                    consumed.Add(s);
                    if (--need == 0) break;
                }
                if (need > 0) return false;
            }

            product = r.product;
            return true;
        }

        private bool Matches(Recipe r, List<AtomCardDef> selected)
        {
            // 選択のカウント
            var selCount = new Dictionary<AtomCardDef, int>();
            foreach (var s in selected)
            {
                if (!s) continue;
                selCount.TryGetValue(s, out int c);
                selCount[s] = c + 1;
            }

            // 必要数を満たすか
            foreach (var req in r.requires)
            {
                if (!req.atom) return false;
                selCount.TryGetValue(req.atom, out int have);
                if (have < req.count) return false;
            }

            // ちょうど一致（余分な選択は不可）
            int needTotal = r.requires.Sum(x => x.count);
            return selected.Count == needTotal;
        }
    }
}
