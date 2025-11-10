using UnityEngine;

namespace Sugoroku.Atoms
{
    /// <summary>
    /// マス/ボタンイベントから原子カードを付与/複製/破棄する受け口
    /// </summary>
    public class AtomCardEventBridge : MonoBehaviour
    {
        [Tooltip("カードを付与する対象の手札（未設定なら自動検索）")]
        public PlayerHand targetHand;

        [Tooltip("手札UI（設定されていればAdd後に自動リフレッシュ）")]
        public HandUI handUI;

        public GameStateMachine gsm;
        
        void Start()
        {
            if (gsm == null) gsm = FindObjectOfType<GameStateMachine>();
        }

        PlayerHand EnsureHand()
        {
            targetHand = gsm.CurrentPlayer.atomHand;
            if (!targetHand)
            {
                Debug.LogWarning("[AtomCardEventBridge] targetHand が見つかりません。PlayerHand をシーンに配置/割当してください。");
            }
            return targetHand;
        }

        void RefreshUIIfOpen()
        {
            if (handUI && handUI.rootPanel && handUI.rootPanel.activeSelf)
                handUI.Refresh();
        }

        public void Gain(AtomCardDef card)
        {
            var hand = EnsureHand();
            if (!hand) return;
            hand.Add(card);
            Debug.Log($"[Event] 原子カード獲得: {card?.nameJP}（合計: {hand.Cards.Count}）");
            Sugoroku.UI.MessageManager.Important($"『{card?.nameJP}』を獲得した！");
            RefreshUIIfOpen();
        }

        public void Duplicate(AtomCardDef card)
        {
            var hand = EnsureHand();
            if (!hand) return;
            hand.Duplicate(card);
            Debug.Log($"[Event] 原子カード複製: {card?.nameJP}（合計: {hand.Cards.Count}）");
            RefreshUIIfOpen();
        }

        public void RemoveOne(AtomCardDef card)
        {
            var hand = EnsureHand();
            if (!hand) return;

            if (hand.RemoveOne(card))
            {
                Debug.Log($"[Event] 原子カード破棄: {card?.nameJP}（合計: {hand.Cards.Count}）");
                RefreshUIIfOpen();
            }
            else
            {
                Debug.Log($"[Event] 破棄失敗: {card?.nameJP}（手札にありません）");
            }
        }
    }
}
