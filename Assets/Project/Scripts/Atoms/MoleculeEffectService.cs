using System.Collections.Generic;
using UnityEngine;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// 分子カードの効果適用サービス。
    /// Arm(selected) で「発動待ち」をセットし、次の Before/AfterRoll/AfterMove で適用。
    /// consumeOnUse が true の効果を使ったら 1枚消費し、Arm を解除。
    /// </summary>
    public class MoleculeEffectService : MonoBehaviour
    {
        public static MoleculeEffectService Instance { get; private set; }

        // 現在「発動待ち」にしている分子（単一）
        private MoleculeCardDef _armed;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ====== 外部API ======
        public void Arm(MoleculeCardDef card)
        {
            _armed = card;
            Debug.Log(_armed
                ? $"[MoleculeEffect] Armed: {_armed.nameJP}（{_armed.formula}）"
                : "[MoleculeEffect] Disarmed");
        }

        public void Disarm() => Arm(null);

        public bool IsArmed => _armed != null;
        public MoleculeCardDef ArmedCard => _armed;

        // ★ dice制限解除（1–6 クランプを外す）判定
        private bool LiftDiceLimit =>
            IsArmed && _armed && _armed.effects != null &&
            _armed.effects.Exists(e => e != null && e.liftDiceLimit);

        public bool ShouldLiftDiceLimit() => LiftDiceLimit;

        // ====== 効果適用 ======
        public void ApplyBeforeRoll(PlayerMoleculeHand hand, ref int roll)
        {
            if (!IsArmed || !hand || _armed.effects == null) return;

            foreach (var eff in _armed.effects)
            {
                if (eff == null || eff.trigger != MoleculeEffectTrigger.BeforeRoll) continue;

                switch (eff.kind)
                {
                    case MoleculeEffectKind.DiceClampRange:
                    {
                        int min = LiftDiceLimit ? eff.min : Mathf.Max(1, eff.min);
                        int max = LiftDiceLimit ? eff.max : Mathf.Min(6, eff.max);
                        if (min > max) { var tmp = min; min = max; max = tmp; }
                        roll = Random.Range(min, max + 1);
                        TryConsume(hand, _armed, eff);
                        break;
                    }

                    case MoleculeEffectKind.DiceForceParity:
                    {
                        roll = ForceParity(roll, eff.parity);
                        TryConsume(hand, _armed, eff);
                        break;
                    }
                }
            }
        }

        public void ApplyAfterRoll(PlayerMoleculeHand hand, ref int roll)
        {
            if (!IsArmed || !hand || _armed.effects == null) return;

            foreach (var eff in _armed.effects)
            {
                if (eff == null || eff.trigger != MoleculeEffectTrigger.AfterRoll) continue;

                switch (eff.kind)
                {
                    case MoleculeEffectKind.DiceAdd:
                    {
                        int v = roll + eff.min; // ±N
                        roll = LiftDiceLimit ? v : Mathf.Clamp(v, 1, 6);
                        TryConsume(hand, _armed, eff);
                        break;
                    }

                    case MoleculeEffectKind.DiceForceParity:
                    {
                        roll = ForceParity(roll, eff.parity);
                        TryConsume(hand, _armed, eff);
                        break;
                    }

                    case MoleculeEffectKind.DiceClampRange:
                    {
                        int min = LiftDiceLimit ? eff.min : Mathf.Max(1, eff.min);
                        int max = LiftDiceLimit ? eff.max : Mathf.Min(6, eff.max);
                        if (min > max) { var tmp = min; min = max; max = tmp; }
                        roll = Mathf.Clamp(roll, min, max);
                        TryConsume(hand, _armed, eff);
                        break;
                    }
                }
            }
        }

        public void ApplyAfterMove(PlayerMoleculeHand hand, ref int boardIndex)
        {
            if (!IsArmed || !hand || _armed.effects == null) return;

            foreach (var eff in _armed.effects)
            {
                if (eff == null || eff.trigger != MoleculeEffectTrigger.AfterMove) continue;

                if (eff.kind == MoleculeEffectKind.MoveOffset)
                {
                    boardIndex += eff.min; // +/-
                    TryConsume(hand, _armed, eff);
                }
            }
        }

        // ====== 内部ユーティリティ ======
        private static int ForceParity(int value, Parity p)
        {
            bool even = (value % 2 == 0);
            if (p == Parity.Even && !even) value = value + 1;                     // 例：3→4
            if (p == Parity.Odd  &&  even) value = (value == 6) ? 5 : (value + 1); // 例：2→3、6→5
            return value;
        }

        private void TryConsume(PlayerMoleculeHand hand, MoleculeCardDef card, MoleculeEffectEntry eff)
        {
            if (!eff.consumeOnUse) return;

            bool removed = hand.RemoveOne(card);
            Debug.Log(removed
                ? $"[MoleculeEffect] Consumed: {card.nameJP}（{card.formula}）"
                : $"[MoleculeEffect] Consume failed: {card.nameJP}");
            Sugoroku.UI.MessageManager.Important(removed
                ? $"『{card.nameJP}（{card.formula}）』を使用した！"
                : $"『{card.nameJP}（{card.formula}）』の使用に失敗した！");

            // どれかの効果で消費が発生したら解除
            if (removed) Disarm();
        }
    }
}
