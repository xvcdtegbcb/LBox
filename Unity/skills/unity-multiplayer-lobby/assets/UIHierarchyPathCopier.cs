using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[InitializeOnLoad]
public static class UIBuilderSelectionPathCopier
{
    static UIBuilderSelectionPathCopier()
    {
        EditorApplication.update += TryHookBuilderWindows;
    }

    private static void TryHookBuilderWindows()
    {
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            var type = window.GetType();
            if (type.Name != "BuilderWindow" && type.Name != "Builder" &&
                window.titleContent.text != "UI Builder")
                continue;

            if (window.rootVisualElement?.userData is string hooked && hooked == "path-copier-hooked")
                continue;

            HookBuilderWindow(window);
        }
    }

    private static void HookBuilderWindow(EditorWindow builder)
    {
        var root = builder.rootVisualElement;
        if (root == null) return;

        root.RegisterCallback<ContextualMenuPopulateEvent>(evt =>
        {
            var target = evt.target as VisualElement;
            if (target == null) return;

            var selected = GetBuilderSelection(builder);
            if (selected == null) return;

            // Climb up from the raw selection to the nearest UXML-declared element
            var uxmlElement = ResolveUxmlElement(selected, builder);
            if (uxmlElement == null) return;

            evt.menu.AppendSeparator();
            evt.menu.AppendAction("复制层级路径", _ =>
            {
                var path = BuildVisualElementPath(uxmlElement, builder);
                GUIUtility.systemCopyBuffer = path;
                Debug.Log($"[UI Path Copier] 层级路径已复制: {path}");
            });

            evt.menu.AppendAction("复制 Q() 查询代码", _ =>
            {
                var query = BuildQueryString(uxmlElement);
                GUIUtility.systemCopyBuffer = query;
                Debug.Log($"[UI Path Copier] Q()查询已复制: {query}");
            });

            evt.menu.AppendAction("复制路径 + Q() 代码", _ =>
            {
                var path = BuildVisualElementPath(uxmlElement, builder);
                var query = BuildQueryString(uxmlElement);
                var combined = $"// 路径: {path}\n{query}";
                GUIUtility.systemCopyBuffer = combined;
                Debug.Log($"[UI Path Copier] 路径+Q()已复制:\n  路径: {path}\n  Q查询: {query}");
            });
        }, TrickleDown.TrickleDown);

        root.userData = "path-copier-hooked";
    }

    [MenuItem("Tools/复制 UI Builder 选中元素路径 %#C", priority = 100)]
    public static void CopySelectedElementPath()
    {
        var builder = FindBuilderWindow();
        if (builder == null)
        {
            Debug.LogWarning("[UI Path Copier] 未找到 UI Builder 窗口。");
            return;
        }

        var selected = GetBuilderSelection(builder);
        if (selected == null)
        {
            Debug.LogWarning("[UI Path Copier] UI Builder 中没有选中任何元素。");
            return;
        }

        var uxmlElement = ResolveUxmlElement(selected, builder);
        if (uxmlElement == null) uxmlElement = selected;

        var path = BuildVisualElementPath(uxmlElement, builder);
        var query = BuildQueryString(uxmlElement);
        GUIUtility.systemCopyBuffer = path;
        Debug.Log($"[UI Path Copier] 层级路径已复制:\n  路径: {path}\n  Q查询: {query}");
    }

    [MenuItem("Tools/复制 UI Builder 选中元素路径 %#C", validate = true)]
    public static bool ValidateCopySelectedElementPath()
    {
        return FindBuilderWindow() != null;
    }

    #region Reflection

    private static EditorWindow FindBuilderWindow()
    {
        foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
        {
            if (window.titleContent.text == "UI Builder" || window.GetType().Name == "Builder")
                return window;
        }
        return null;
    }

    private static VisualElement GetBuilderSelection(EditorWindow builder)
    {
        var type = builder.GetType();
        var selProp = type.GetProperty("selection",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var selObj = selProp?.GetValue(builder);
        if (selObj == null) return null;

        var selType = selObj.GetType();
        var listProp = selType.GetProperty("selection",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        var list = listProp?.GetValue(selObj) as IList<VisualElement>;
        if (list == null || list.Count == 0) return null;

        return list[0];
    }

    private static VisualElement GetDocumentRoot(EditorWindow builder)
    {
        var type = builder.GetType();
        var prop = type.GetProperty("documentRootElement",
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
        return prop?.GetValue(builder) as VisualElement;
    }

    #endregion

    #region UXML Element Resolution

    /// <summary>
    /// From the raw selected VisualElement, walk up to find the nearest UXML-declared element.
    /// 
    /// Internal control elements are identified by:
    /// 1. Name starts with "unity-" (e.g. unity-checkmark, unity-input)
    /// 2. The element IS the contentContainer of its parent (controls like Toggle/TextField
    ///    delegate content to an internal VisualElement; that internal element is not UXML-declared)
    /// </summary>
    private static VisualElement ResolveUxmlElement(VisualElement selected, EditorWindow builder)
    {
        var docRoot = GetDocumentRoot(builder);
        var current = selected;

        while (current != null && current != docRoot)
        {
            // If this element is inside Builder chrome, stop
            if (IsBuilderChrome(current)) return null;

            // Check: is this an internal control element?
            if (IsInternalControlElement(current))
            {
                current = current.parent;
                continue;
            }

            // This is a UXML-declared element
            return current;
        }

        return selected;
    }

    /// <summary>
    /// Determine if a VisualElement is an internal control element (not declared in UXML).
    /// 
    /// Heuristics:
    /// - Name starts with "unity-": always internal (e.g. unity-checkmark, unity-input, unity-label)
    /// - Element is the contentContainer of its parent: internal (controls delegate to internal containers)
    /// - Parent is a control type AND this element has no name: likely internal structure
    /// </summary>
    private static bool IsInternalControlElement(VisualElement element)
    {
        if (element == null) return true;

        // 1. Unity-internal named elements
        if (!string.IsNullOrEmpty(element.name) && element.name.StartsWith("unity-"))
            return true;

        // 2. Check if this element is the contentContainer of its parent
        //    Controls like Toggle, TextField, ScrollView create internal VisualElements
        //    and set them as their contentContainer. These are NOT UXML-declared.
        if (element.parent != null)
        {
            // contentContainer for a plain VisualElement returns itself.
            // For controls (Toggle, Button, etc.), it returns an internal child.
            // So if parent.contentContainer == element AND parent is not a plain VisualElement,
            // then element is internal.
            var parentCC = element.parent.contentContainer;
            if (parentCC == element && !IsPlainVisualElement(element.parent))
                return true;
        }

        return false;
    }

    /// <summary>
    /// Check if an element is a plain VisualElement (not a control with internal structure).
    /// </summary>
    private static bool IsPlainVisualElement(VisualElement element)
    {
        if (element == null) return false;
        var typeName = element.GetType().Name;

        // These types have internal structure (contentContainer != self)
        var controlTypes = new HashSet<string>
        {
            "Toggle", "TextField", "Button", "ScrollView", "Foldout",
            "Slider", "Scroller", "DropdownField", "EnumField",
            "IntegerField", "FloatField", "DoubleField", "LongField",
            "CurveField", "ColorField", "ObjectField", "Vector2Field",
            "Vector3Field", "Vector4Field", "RectField", "BoundsField",
            "MaskField", "LayerMaskField", "LayerField", "TagField",
            "BasePopupField", "ProgressBar"
        };

        return !controlTypes.Contains(typeName);
    }

    #endregion

    #region Path Builders

    private static string BuildVisualElementPath(VisualElement element, EditorWindow builder)
    {
        var docRoot = GetDocumentRoot(builder);
        var parts = new StringBuilder();
        var current = element;

        while (current != null)
        {
            if (current == docRoot || current.parent == docRoot)
            {
                if (current != docRoot)
                {
                    var part = GetElementName(current);
                    if (parts.Length > 0) parts.Insert(0, "/");
                    parts.Insert(0, part);
                }
                break;
            }

            if (IsBuilderChrome(current)) break;

            var name = GetElementName(current);
            if (parts.Length > 0) parts.Insert(0, "/");
            parts.Insert(0, name);

            current = current.parent;
        }

        return parts.ToString();
    }

    private static string GetElementName(VisualElement element)
    {
        if (!string.IsNullOrEmpty(element.name))
            return element.name;
        return element.GetType().Name;
    }

    private static string BuildQueryString(VisualElement element)
    {
        var typeName = GetSimpleTypeName(element);

        if (!string.IsNullOrEmpty(element.name) && !element.name.StartsWith("unity-"))
            return $"root.Q<{typeName}>(\"{element.name}\")";
        else
            return $"root.Q<{typeName}>()";
    }

    private static string GetSimpleTypeName(VisualElement element)
    {
        var name = element.GetType().Name;
        if (name == "Label" || name == "Button" || name == "TextField" ||
            name == "Toggle" || name == "ScrollView" || name == "VisualElement" ||
            name == "Foldout" || name == "Slider" || name == "Scroller" || name == "Box")
            return name;
        return name;
    }

    private static bool IsBuilderChrome(VisualElement element)
    {
        if (element == null) return true;
        foreach (var cls in element.GetClasses())
        {
            if (cls.StartsWith("unity-builder") || cls.StartsWith("builder-"))
                return true;
        }
        return false;
    }

    #endregion
}
