using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class TokenMover : MonoBehaviour
{
    public BoardBuilder board;
    public float moveSpeed = 4f;          // 1マスの移動速度
    public float arriveThreshold = 0.02f;
    public int currentIndex = 0;

    bool isMoving;

    void Start()
    {
        if (board != null && board.Count > 0)
            transform.position = board.GetPoint(0); // 2DなのでそのままXY
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
            RollAndMove();
    }

    public void RollAndMove()
    {
        if (isMoving || board == null) return;
        int dice = Random.Range(1, 7); // 1〜6
        StartCoroutine(MoveSteps(dice));
    }

    IEnumerator MoveSteps(int steps)
    {
        isMoving = true;

        for (int s = 0; s < steps; s++)
        {
            int next = Mathf.Min(currentIndex + 1, board.Count - 1);
            yield return MoveToIndex(next);
            currentIndex = next;

            if (currentIndex == board.Count - 1) break; // ゴール
        }

        isMoving = false;
    }

    IEnumerator MoveToIndex(int target)
    {
        Vector3 targetPos = board.GetPoint(target);

        while ((transform.position - targetPos).sqrMagnitude > arriveThreshold * arriveThreshold)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPos, moveSpeed * Time.deltaTime);
            yield return null;
        }
    }
}
