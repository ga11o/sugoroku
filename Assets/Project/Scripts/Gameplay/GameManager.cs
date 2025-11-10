using UnityEngine;
using UnityEngine.SceneManagement; // シーン切り替えに必須

public class GameManager : MonoBehaviour
{
    // 現在のシーン名を取得します。
    private string currentSceneName;

    void Start()
    {
        // 現在のシーン名を保存しておきます（再スタート時に使用）
        currentSceneName = SceneManager.GetActiveScene().name;
    }

    /// <summary>
    /// ゲームを再スタートさせるメソッド
    /// </summary>
    public void RestartGame()
    {
        Debug.Log("ゲーム再スタート！");
        
        // または、特定のメインのゲームプレイシーン名を使うこともできます。
        SceneManager.LoadScene("Main"); 
    }

/// <summary>
    /// ゲームを終了させるメソッド（または、StartSceaneに遷移させるメソッド）
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("StartSceaneに遷移します。");
        
        // StartSceaneという名前のシーンをロードします。
        SceneManager.LoadScene("StartSceane");

        // ※元々あったエディター/ビルド終了のコードは削除またはコメントアウトします。
        /* #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
        */
    }
}