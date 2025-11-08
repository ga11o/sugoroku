using UnityEngine;

namespace Sugoroku.Atoms
{
    // 原子カードの定義（ScriptableObject）
    [CreateAssetMenu(menuName = "Sugoroku/Atom Card", fileName = "AtomCard")]
    public class AtomCardDef : ScriptableObject
    {
        [Header("ID")]
        [Tooltip("原子番号 (1=H など)")]
        public int atomicNumber;

        [Header("Label")]
        public string symbolJP = "H";      // 例: H
        public string nameJP   = "水素";    // 例: 水素

        [Header("Visual")]
        [Tooltip("カード画像（例: Assets/Project/Prefabs/atom/001.png）")]
        public Sprite cardSprite;
    }
}
