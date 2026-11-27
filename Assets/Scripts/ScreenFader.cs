using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;
    public Image fadeImage; // повноекранний чорний Image, alpha=0

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else { Destroy(gameObject); return; }
        DontDestroyOnLoad(gameObject);
    }

    public IEnumerator FadeOut(float duration)
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            var c = fadeImage.color; c.a = Mathf.Clamp01(t); fadeImage.color = c;
            yield return null;
        }
    }

    public IEnumerator FadeIn(float duration)
    {
        float t = 0f;
        var c0 = fadeImage.color;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            var c = fadeImage.color; c.a = Mathf.Clamp01(1f - t); fadeImage.color = c;
            yield return null;
        }
    }
}