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
        // 現在のシーンを再ロードします。
        SceneManager.LoadScene(currentSceneName); 
        
        // または、特定のメインのゲームプレイシーン名を使うこともできます。
        // SceneManager.LoadScene("MainGameScene"); 
    }

    /// <summary>
    /// ゲームを終了させるメソッド
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("ゲームを終了します。");
        
        // Unityエディターで実行している場合は、Playモードを停止します。
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        // ビルドしたアプリケーションで実行している場合は、アプリケーションを閉じます。
        #else
            Application.Quit();
        #endif
    }
}