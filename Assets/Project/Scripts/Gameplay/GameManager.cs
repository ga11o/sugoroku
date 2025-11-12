using UnityEngine;
using UnityEngine.SceneManagement; // シーン切り替えに必須

public class GameManager : MonoBehaviour
{
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
    }
}