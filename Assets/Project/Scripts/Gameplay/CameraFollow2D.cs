using UnityEngine;

[RequireComponent(typeof(Camera))]
public class CameraFollow2D : MonoBehaviour
{
    [Header("対象")]
    public Transform target;           // 追従対象（Token）
    public Vector2 offset = Vector2.zero;

    [Header("スムーズ追従")]
    [Tooltip("目標位置に到達するまでのおおよその時間（秒）")]
    public float smoothTime = 0.15f;

    [Header("ルックアヘッド（進行方向に少し先を見る）")]
    public bool lookAhead = true;
    public float lookAheadDistance = 0.6f;   // どれくらい先を見るか（ワールド単位）
    public float lookAheadDamping = 0.25f;   // 先読みのなまり

    [Header("盤面内にクランプ")]
    public bool clampToBoard = true;
    public BoardBuilder board;         // 自動で見つけますが Inspector から指定可能
    public float boundsPadding = 1.0f; // 余白（ワールド単位）

    [Header("その他")]
    public bool keepZAtMinus10 = true; // 2Dなら -10 を維持

    Camera cam;
    Vector3 velocity;
    Vector3 lastTargetPos;

    void Awake()
    {
        cam = GetComponent<Camera>();

        // 自動解決（設定が空でも動く）
        if (target == null)
        {
            var token = FindObjectOfType<TokenMover>();
            if (token != null) target = token.transform;
        }
        if (board == null) board = FindObjectOfType<BoardBuilder>();

        lastTargetPos = target ? target.position : Vector3.zero;
    }

    void LateUpdate()
    {
        if (target == null) return;

        // 目標位置
        Vector3 desired = target.position + (Vector3)offset;

        // 進行方向に先読み
        if (lookAhead)
        {
            Vector3 delta = target.position - lastTargetPos;
            Vector2 lead = Vector2.Lerp(Vector2.zero, new Vector2(delta.x, delta.y) * lookAheadDistance, 1f - lookAheadDamping);
            desired += (Vector3)lead;
        }

        // Z固定
        desired.z = keepZAtMinus10 ? -10f : transform.position.z;

        // スムーズに追従
        Vector3 newPos = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);

        // 盤面に収める
        if (clampToBoard && board != null && board.Count > 0)
        {
            Bounds b = GetBoardBounds(board);
            Vector2 ext = GetCameraHalfExtents(cam);

            float minX = b.min.x - boundsPadding + ext.x;
            float maxX = b.max.x + boundsPadding - ext.x;
            float minY = b.min.y - boundsPadding + ext.y;
            float maxY = b.max.y + boundsPadding - ext.y;

            // 盤よりカメラが大きい場合のはみ出し対策
            if (minX > maxX) { float c = (minX + maxX) * 0.5f; minX = maxX = c; }
            if (minY > maxY) { float c = (minY + maxY) * 0.5f; minY = maxY = c; }

            newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
            newPos.y = Mathf.Clamp(newPos.y, minY, maxY);
        }

        transform.position = newPos;
        lastTargetPos = target.position;
    }

    static Bounds GetBoardBounds(BoardBuilder bb)
    {
        Bounds b = new Bounds(bb.GetPoint(0), Vector3.one * 0.001f);
        for (int i = 1; i < bb.Count; i++) b.Encapsulate(bb.GetPoint(i));
        return b;
    }

    static Vector2 GetCameraHalfExtents(Camera cam)
    {
        float halfH = cam.orthographicSize;
        float halfW = halfH * cam.aspect;
        return new Vector2(halfW, halfH);
    }
}
