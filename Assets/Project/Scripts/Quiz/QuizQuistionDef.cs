using UnityEngine;
using Sugoroku.Atoms;

namespace Sugoroku.Quiz
{
    /// <summary>
    /// 1問分のクイズ定義アセット
    /// </summary>
    [CreateAssetMenu(
        fileName = "QuizQuestion",
        menuName = "Sugoroku/Quiz Question",
        order = 10)]
    public class QuizQuestionDef : ScriptableObject
    {
        [Header("問題文")]
        [TextArea]
        public string question;

        [Header("選択肢（4択）")]
        [Tooltip("4択の選択肢（要素数4を推奨）")]
        public string[] choices = new string[4];

        [Tooltip("正解インデックス（0〜3）")]
        [Range(0, 3)]
        public int correctIndex = 0;

        [Header("正解時の報酬")]
        [Tooltip("正解したときに獲得する原子カード")]
        public AtomCardDef rewardCard;
    }
}
