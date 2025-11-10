using UnityEngine;

namespace Sugoroku.UI
{
    /// <summary>
    /// ログ＆ポップの窓口。どこからでも MessageManager.Instance.Log / Important で呼べる。
    /// </summary>
    public class MessageManager : MonoBehaviour
    {
        public static MessageManager Instance { get; private set; }

        [Header("Refs")]
        public MessageWindow window;
        public MessagePopup popup;

        void Awake()
        {
            if (Instance && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // 下部ログ
        public static void Log(string msg)
        {
            if (Instance?.window != null) Instance.window.Add(msg);
        }

        // 重要ポップ＋ログにも残す
        public static void Important(string msg)
        {
            if (Instance?.popup != null) Instance.popup.Enqueue(msg);
            if (Instance?.window != null) Instance.window.Add(msg);
        }

        // 便利メソッド（必要なら呼ぶ）
        public static void OnDiceRolled(int value) => Log($"Dice: {value}");
        public static void OnCardUsed(string nameJP) => Log($"カード使用: {nameJP}");
        public static void OnTileEvent(string title) => Important($"イベント: {title}");
    }
}
