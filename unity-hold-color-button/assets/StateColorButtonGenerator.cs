using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 编辑器窗口工具：生成一个带三态颜色（正常、悬停、按下）的 uGUI 按钮。
/// </summary>
public class StateColorButtonGeneratorWindow : EditorWindow
{
    private Color normalColor  = new Color(0.2f, 0.6f, 1f, 1f);  // 蓝
    private Color hoverColor   = new Color(1f, 0.8f, 0.2f, 1f);  // 黄
    private Color pressedColor = new Color(1f, 0.3f, 0.3f, 1f);  // 红

    private string buttonText  = "Button";
    private float buttonWidth  = 160f;
    private float buttonHeight = 60f;

    [MenuItem("Tools/生成三态颜色按钮")]
    public static void ShowWindow()
    {
        GetWindow<StateColorButtonGeneratorWindow>("三态颜色按钮生成器");
    }

    private void OnGUI()
    {
        GUILayout.Label("三态颜色按钮生成器", EditorStyles.boldLabel);
        GUILayout.Label("生成一个 uGUI 按钮，鼠标悬停和按下时显示不同颜色", EditorStyles.wordWrappedLabel);

        EditorGUILayout.Space();

        buttonText = EditorGUILayout.TextField("按钮文字", buttonText);
        buttonWidth  = EditorGUILayout.FloatField("按钮宽", buttonWidth);
        buttonHeight = EditorGUILayout.FloatField("按钮高", buttonHeight);

        EditorGUILayout.Space();

        normalColor  = EditorGUILayout.ColorField("正常颜色", normalColor);
        hoverColor   = EditorGUILayout.ColorField("悬停颜色", hoverColor);
        pressedColor = EditorGUILayout.ColorField("按下颜色", pressedColor);

        EditorGUILayout.Space();

        // 预览
        GUILayout.Label("颜色预览", EditorStyles.miniBoldLabel);
        Rect previewRect = GUILayoutUtility.GetRect(0, 40);
        DrawColorPreview(previewRect);

        EditorGUILayout.Space();

        if (GUILayout.Button("生成", GUILayout.Height(30)))
        {
            GenerateButton();
        }
    }

    private void DrawColorPreview(Rect rect)
    {
        float w = rect.width / 3f;
        DrawRectLabel(new Rect(rect.x, rect.y, w, rect.height), "正常", normalColor);
        DrawRectLabel(new Rect(rect.x + w, rect.y, w, rect.height), "悬停", hoverColor);
        DrawRectLabel(new Rect(rect.x + w * 2f, rect.y, w, rect.height), "按下", pressedColor);
    }

    private static void DrawRectLabel(Rect rect, string label, Color color)
    {
        Color old = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = old;
        // 描边
        Handles.DrawSolidRectangleWithOutline(rect, Color.clear, new Color(0.1f, 0.1f, 0.1f, 0.5f));
        // 文字
        GUI.Label(rect, label, new GUIStyle(EditorStyles.whiteBoldLabel)
        {
            alignment = TextAnchor.MiddleCenter
        });
    }

    private void GenerateButton()
    {
        if (buttonWidth <= 0 || buttonHeight <= 0)
        {
            EditorUtility.DisplayDialog("参数错误", "按钮宽高必须大于 0", "确定");
            return;
        }

        // 确保 Canvas 和 EventSystem 存在
        Canvas canvas = Object.FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // 创建 Canvas
            GameObject canvasGo = new GameObject("Canvas");
            canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGo.AddComponent<CanvasScaler>();
            canvasGo.AddComponent<GraphicRaycaster>();
            Undo.RegisterCreatedObjectUndo(canvasGo, "Generate Button");

            // 创建 EventSystem
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGo.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Undo.RegisterCreatedObjectUndo(esGo, "Generate Button");
        }

        // 创建 Button
        GameObject buttonGo = new GameObject("StateColorButton");
        buttonGo.transform.SetParent(canvas.transform, false);
        RectTransform rt = buttonGo.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(buttonWidth, buttonHeight);

        Image image = buttonGo.AddComponent<Image>();
        image.color = normalColor;

        // 使用自定义 HoldColorButton，按住期间始终保持按下颜色
        HoldColorButton holdBtn = buttonGo.AddComponent<HoldColorButton>();
        holdBtn.normalColor  = normalColor;
        holdBtn.hoverColor   = hoverColor;
        holdBtn.pressedColor = pressedColor;
        holdBtn.fadeDuration  = 0.1f;

        // 创建文字子物体
        GameObject textGo = new GameObject("Text");
        textGo.transform.SetParent(buttonGo.transform, false);
        TMPro.TextMeshProUGUI tmp = textGo.AddComponent<TMPro.TextMeshProUGUI>();
        tmp.text = buttonText;
        tmp.alignment = TMPro.TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.fontSize = 24;
        RectTransform textRt = textGo.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;

        Undo.RegisterCreatedObjectUndo(buttonGo, "Generate Button");

        Selection.activeGameObject = buttonGo;
        Debug.Log($"已生成三态颜色按钮: {buttonText} (正常={normalColor}, 悬停={hoverColor}, 按下={pressedColor})");
    }
}
