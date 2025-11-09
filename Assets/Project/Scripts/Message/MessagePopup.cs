using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

namespace Sugoroku.UI
{
    /// <summary>
    /// 重要メッセージ用ポップアップ。
    /// ・キューに積んだ複数メッセージを Prev/Next で前後移動
    /// ・タイプライタ中に Prev/Next を押すと全文表示にスキップ
    /// ・末尾では Next が Close になる
    /// </summary>
    public class MessagePopup : MonoBehaviour
    {
        [Header("UI")]
        public GameObject root;      // パネル本体
        public TMP_Text text;        // 本文
        public Button nextButton;    // 次へ / 閉じる
        public Button prevButton;    // もどる

        [Header("演出")]
        [Tooltip("1文字の表示間隔(秒)")]
        public float charInterval = 0.02f;

        // 内部状態
        private readonly List<string> _messages = new();
        private int _index = -1;         // 現在表示中インデックス
        private bool _visible = false;
        private bool _typing = false;     // タイプライタ中？
        private string _currentFull = ""; // 現在の全文（スキップ用）

        void Awake()
        {
            if (root) root.SetActive(false);
            if (nextButton) nextButton.onClick.AddListener(OnClickNext);
            if (prevButton) prevButton.onClick.AddListener(OnClickPrev);
        }

        void OnDestroy()
        {
            if (nextButton) nextButton.onClick.RemoveListener(OnClickNext);
            if (prevButton) prevButton.onClick.RemoveListener(OnClickPrev);
        }

        /// <summary>1件追加。非表示なら直ちに表示を開始。</summary>
        public void Enqueue(string msg)
        {
            if (string.IsNullOrWhiteSpace(msg)) return;
            _messages.Add(msg);

            if (!_visible)
            {
                _index = Mathf.Clamp(_index, 0, _messages.Count - 1);
                _index = 0;          // 先頭から開始
                ShowCurrent();
            }
            else
            {
                // 既に表示中なら、末尾が増えるだけ（必要なら自動で末尾へ飛ばさない）
                UpdateButtons();
            }
        }

        /// <summary>複数追加。</summary>
        public void EnqueueMany(IEnumerable<string> msgs)
        {
            bool had = _messages.Count > 0;
            foreach (var m in msgs) if (!string.IsNullOrWhiteSpace(m)) _messages.Add(m);
            if (!had) { _index = 0; ShowCurrent(); }
            else UpdateButtons();
        }

        // === ボタン ===

        void OnClickNext()
        {
            if (!_visible) return;

            // タイプ中は全文表示にスキップ
            if (_typing)
            {
                ForceShowFull();
                return;
            }

            if (_index < _messages.Count - 1)
            {
                _index++;
                ShowCurrent();
            }
            else
            {
                // 末尾なら閉じる
                Hide();
            }
        }

        void OnClickPrev()
        {
            if (!_visible) return;

            // タイプ中は全文表示にスキップ（戻る前に内容を確定）
            if (_typing)
            {
                ForceShowFull();
                return;
            }

            if (_index > 0)
            {
                _index--;
                ShowCurrent();
            }
            // 先頭では何もしない
        }

        // === 表示制御 ===

        void ShowCurrent()
        {
            if (_messages.Count == 0 || _index < 0 || _index >= _messages.Count) return;

            if (root) root.SetActive(true);
            _visible = true;

            // タイプライタ開始
            StopAllCoroutines();
            StartCoroutine(TypeRoutine(_messages[_index]));
            UpdateButtons();
        }

        void Hide()
        {
            StopAllCoroutines();
            if (root) root.SetActive(false);
            _visible = false;
            _typing = false;
            _currentFull = "";
            // キューは残しても良いが、ここではクリア（必要に応じて外す）
            _messages.Clear();
            _index = -1;
            UpdateButtons();
        }

        System.Collections.IEnumerator TypeRoutine(string msg)
        {
            _typing = true;
            _currentFull = msg;
            if (text) text.text = "";

            foreach (char c in msg)
            {
                if (text) text.text += c;
                yield return new WaitForSecondsRealtime(charInterval);
            }

            _typing = false;
            UpdateButtons();
        }

        void ForceShowFull()
        {
            _typing = false;
            StopAllCoroutines();
            if (text) text.text = _currentFull;
            UpdateButtons();
        }

        void UpdateButtons()
        {
            // Prev は先頭で無効化
            if (prevButton) prevButton.interactable = _visible && _index > 0;

            // Next は常に押せるが、末尾＆タイプ完了時は「Close」相当
            if (nextButton)
            {
                var label = nextButton.GetComponentInChildren<TMP_Text>();
                bool isLast = _index >= 0 && _index == _messages.Count - 1;
                string s = (_typing || !isLast) ? "Next" : "Close";
                if (label) label.text = s;
            }
        }
    }
}
