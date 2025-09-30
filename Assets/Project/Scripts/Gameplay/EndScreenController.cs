using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class EndScreenController : MonoBehaviour
{
    [Header("UI参照")]
    [SerializeField] CanvasGroup group;           // 無ければ自動取得
    [SerializeField] TMP_Text titleText;          // 任意
    [SerializeField] TMP_Text subtitleText;       // 任意

    [Header("演出")]
    [SerializeField] float fadeDuration = 0.25f;  // フェード時間

    void Awake()
    {
        if (group == null) group = GetComponent<CanvasGroup>();
        if (group != null)
        {
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
        }
        gameObject.SetActive(true); // フェード動作のため有効化
    }

    public void Show(string title = "ゴール！", string subtitle = "おめでとう")
    {
        if (titleText) titleText.text = title;
        if (subtitleText) subtitleText.text = subtitle;
        StopAllCoroutines();
        StartCoroutine(FadeIn());
    }

    IEnumerator FadeIn()
    {
        if (group == null) yield break;
        float t = 0f;
        group.blocksRaycasts = true;

        while (t < fadeDuration)
        {
            t += Time.unscaledDeltaTime;              // ポーズしても動く
            group.alpha = Mathf.Lerp(0f, 1f, t / fadeDuration);
            yield return null;
        }
        group.alpha = 1f;
        group.interactable = true;
    }

    public void Hide()
    {
        if (group == null) return;
        group.alpha = 0f;
        group.interactable = false;
        group.blocksRaycasts = false;
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
