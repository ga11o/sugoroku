using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Sugoroku.UI
{
    /// <summary>
    /// 画面下部のログウィンドウ。Add で追記、スクロール自動追従。
    /// </summary>
    public class MessageWindow : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;          // パネル本体（任意で非表示可）
        public TMP_Text logText;         // 内容テキスト
        public ScrollRect scroll;        // スクロール

        [Header("設定")]
        public int maxLines = 50;        // ログ最大行
        public bool autoOpenOnLog = true;

        readonly List<string> _lines = new();
        readonly StringBuilder _sb = new();

        public void Clear()
        {
            _lines.Clear();
            if (logText) logText.text = "";
        }

        public void Add(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;

            _lines.Add(msg);
            while (_lines.Count > maxLines) _lines.RemoveAt(0);

            // 再構築
            _sb.Clear();
            for (int i = 0; i < _lines.Count; i++)
            {
                _sb.AppendLine(_lines[i]);
            }
            if (logText) logText.text = _sb.ToString();

            if (autoOpenOnLog && root && !root.activeSelf) root.SetActive(true);

            // スクロール最下部へ
            if (scroll)
            {
                Canvas.ForceUpdateCanvases();
                scroll.verticalNormalizedPosition = 0f;
            }
        }
    }
}
