using System.Collections.Generic;
using UnityEngine;

public class Waypoint : MonoBehaviour
{
    public TileEvent tileEvent;  // このマスに割り当てられたイベント
    public Waypoint mainNext; //本道
    public Waypoint previous;
    public List<Waypoint> branchNexts = new List<Waypoint>(); //分岐先リスト

    // ★ ゴール判定用フラグ
    public bool isGoal = false;
}
