using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Sugoroku/BoardGrid")]
public class BoardPath : ScriptableObject
{
    [Tooltip("タイル間隔（XYの距離）")]
    public float tileSpacing = 5f;

    [Tooltip("3桁目 3=右, 4=左, 5=上, 6=下")]
    //3桁目は進行方向 下2桁はイベントマス　10=前進　11=後退　12=1回休み　13=もう一度振る　14=スタートに戻る  15=クイズ
    public int[,] grid = new int[,] {
        {301,310,300,311,315,600,0},   // 1=スタート
        {0,0,0,0,0,612,0},
        {0,600,400,400,400,413,0},   //
        {0,600,0,0,0,0,0},
        {0,300,314,313,300,302,0},   // 2=ゴール
    };
}
