using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Atoms
{
    [CreateAssetMenu(menuName = "Sugoroku/Molecule Card", fileName = "MoleculeCard")]
    public class MoleculeCardDef : ScriptableObject
    {
        public string nameJP;     // 例: 水
        public string formula;    // 例: H2O
        public Sprite cardSprite; // 見た目（任意）

        // この分子が持つ効果
        public List<MoleculeEffectEntry> effects = new();
    }

    // ===== 効果のトリガ/種類 =====
    public enum MoleculeEffectTrigger
    {
        BeforeRoll,   // サイコロを振る直前（出目確定前）
        AfterRoll,    // 出目確定直後
        AfterMove,    // 盤上の移動完了直後
        OnLand        // マスに着地した瞬間（将来用）
    }

    public enum MoleculeEffectKind
    {
        DiceClampRange,   // 出目の範囲固定（min〜max）
        DiceForceParity,  // 出目の偶奇固定（Even/Odd）
        DiceAdd,          // 出目に加算/減算（min を量として使用）
        MoveOffset        // 現在マスに加算/減算（min を量として使用）
    }

    public enum Parity { Even, Odd }

    [System.Serializable]
    public class MoleculeEffectEntry
    {
        public MoleculeEffectTrigger trigger;
        public MoleculeEffectKind kind;

        [Header("Parameters")]
        public int min = 1;     // ClampRange: 最小 / Add: 量（±可）/ MoveOffset: 量（±可）
        public int max = 6;     // ClampRange: 最大
        public Parity parity;   // ForceParity 用

        [Header("Consumption / Duration")]
        public bool consumeOnUse = true; // 使ったら分子カードを1枚消費
        public int turns = 0;            // 将来の継続用（未使用）

        [Header("Dice Limit")]
        public bool liftDiceLimit = false; // ★使用中は 1–6 のクランプを外す
    }
}
