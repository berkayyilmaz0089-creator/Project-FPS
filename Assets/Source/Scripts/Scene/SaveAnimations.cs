using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class SaveAnimations : MonoBehaviour
{
    [Header("References")]
    public Image saveIcon;
    public Image loadingUi;
    public Image freezeLayer;

    public void ShowSaveIcon()
    {
        if (saveIcon == null)
        {
            return;
        }

        saveIcon.gameObject.SetActive(true);
        StopAllCoroutines();
        StartCoroutine(FadeImage(saveIcon, 1.0f, 0.3f));
    }

    public void HideSaveIcon()
    {
        if (saveIcon == null)
        {
            return;
        }

        StopAllCoroutines();
        StartCoroutine(FadeImage(saveIcon, 0.0f, 0.3f));
    }

    public void ShowLoadingUI(bool isVisible)
    {
        if (loadingUi != null)
        {
            loadingUi.gameObject.SetActive(isVisible);
        }
    }

    private IEnumerator FadeImage(Image img, float targetAlpha, float duration)
    {
        float startAlpha = img.color.a;
        float time = 0.0f;

        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            Color newColor = img.color;
            newColor.a = Mathf.Lerp(startAlpha, targetAlpha, time / duration);
            img.color = newColor;
            yield return null;
        }

        Color finalColor = img.color;
        finalColor.a = targetAlpha;
        img.color = finalColor;

        if (targetAlpha == 0.0f)
        {
            img.gameObject.SetActive(false);
        }
    }
}