using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 自定义按钮：鼠标悬停、按下（按住不放）时显示不同颜色。
/// 按住期间始终保持按下颜色，松开后恢复为悬停或正常颜色。
/// </summary>
[RequireComponent(typeof(Image))]
public class HoldColorButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("三态颜色")]
    public Color normalColor  = new Color(0.2f, 0.6f, 1f, 1f);
    public Color hoverColor   = new Color(1f, 0.8f, 0.2f, 1f);
    public Color pressedColor = new Color(1f, 0.3f, 0.3f, 1f);

    [Header("颜色过渡时间(秒)")]
    public float fadeDuration = 0.1f;

    private Image _image;
    private bool _isPointerDown;
    private bool _isPointerInside;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _image.color = normalColor;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        _isPointerInside = true;
        if (!_isPointerDown)
            SetColor(hoverColor);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _isPointerInside = false;
        if (!_isPointerDown)
            SetColor(normalColor);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isPointerDown = true;
        SetColor(pressedColor);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isPointerDown = false;
        SetColor(_isPointerInside ? hoverColor : normalColor);
    }

    private void SetColor(Color target)
    {
        if (fadeDuration <= 0f)
        {
            _image.color = target;
            return;
        }

        // 停止上一个过渡协程，启动新的
        StopAllCoroutines();
        StartCoroutine(FadeColor(_image.color, target, fadeDuration));
    }

    private System.Collections.IEnumerator FadeColor(Color from, Color to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            _image.color = Color.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        _image.color = to;
    }

    /// <summary>
    /// 运行时调用此方法可重新刷新当前状态颜色（例如参数被修改后）。
    /// </summary>
    public void RefreshColor()
    {
        if (_isPointerDown)
            SetColor(pressedColor);
        else if (_isPointerInside)
            SetColor(hoverColor);
        else
            SetColor(normalColor);
    }
}
