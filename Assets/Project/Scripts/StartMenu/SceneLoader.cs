using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoader : MonoBehaviour
{
    // 遷移先のシーン名をUnityエディタから設定できるようにする
    public string nextSceneName = "GameScene";

    // Startボタンがクリックされたときに呼び出されるメソッド
    public void StartGame()
    {
        // 指定されたシーン名に移動する
        SceneManager.LoadScene(nextSceneName);
    }

    // (オプション) アプリケーションを終了するメソッド
    public void QuitGame()
    {
        // Unityエディタでは動作しません。ビルドされたアプリケーションでのみ有効です。
        Application.Quit();

        // エディタでテストする際のために追記しても良い
        #if UNITY_EDITOR
            Debug.Log("ゲームが終了しました");
            // UnityEditor.EditorApplication.isPlaying = false; // エディタ実行を停止する場合
        #endif
    }
}