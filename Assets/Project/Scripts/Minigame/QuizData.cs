using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "QuizData", menuName = "Minigame/QuizData", order = 1)]
public class QuizData : ScriptableObject
{
    [Serializable]
    public class QuestionData
    {
        [TextArea(2, 5)]
        public string question;
        public string[] choices = new string[4];
        public int correctIndex = 0;
    }

    // デフォルトの問題群をインスペクタで編集可能にする
    public List<QuestionData> questions = new List<QuestionData>()
    {
        new QuestionData {
            question = "水の化学式は？",
            choices = new string[]{"H2O", "CO2", "O2", "NaCl"},
            correctIndex = 0
        },
        new QuestionData {
            question = "原子番号1はどの元素？",
            choices = new string[]{"ヘリウム", "リチウム", "水素", "炭素"},
            correctIndex = 2
        },
        new QuestionData {
            question = "塩酸の主成分は？",
            choices = new string[]{"HCl", "H2SO4", "NaOH", "CH4"},
            correctIndex = 0
        },
        new QuestionData {
            question = "pHが7より小さい溶液は？",
            choices = new string[]{"中性", "塩基性", "酸性", "不活性"},
            correctIndex = 2
        }
    };
}