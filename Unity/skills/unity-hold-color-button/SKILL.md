---
name: unity-hold-color-button
description: Generate a uGUI button with three-state colors (normal, hover, pressed) in Unity. The button stays in the pressed color while the mouse is held down, unlike Unity's default Button which only flashes. Use when the user wants to create a button with distinct hover and hold colors, or asks for a button that changes color when pressed and held — e.g. "生成一个三态颜色按钮", "按钮按住变色", "create a button with hover and press colors", "make a button that stays colored while held".
---

# Unity Hold Color Button

Generate a uGUI button with three visual states: normal, hover, and pressed-and-held.

## Workflow

1. Copy `assets/HoldColorButton.cs` to `Assets/Scripts/` (runtime MonoBehaviour).
2. Copy `assets/StateColorButtonGenerator.cs` to `Assets/Scripts/Editor/` (editor window).
3. Compile and verify in Unity (no errors expected).
4. Open the generator via Unity menu **Tools → 生成三态颜色按钮**.
5. Configure button text, size, and the three colors (normal / hover / pressed).
6. Click **生成** to create the button in the scene.

## Key Design

- `HoldColorButton.cs` implements `IPointerDownHandler`, `IPointerUpHandler`, `IPointerEnterHandler`, `IPointerExitHandler` to manually control the button `Image` color.
- While the mouse button is held down, the button **stays** in `pressedColor` — it does not revert until the mouse is released.
- On release: if the pointer is still inside the button, switch to `hoverColor`; otherwise switch to `normalColor`.
- Color transitions use a coroutine with `Color.Lerp` over `fadeDuration` (default 0.1s). Set `fadeDuration = 0` for instant changes.
- The generator auto-creates a `Canvas` + `EventSystem` if none exists in the scene.
- Button text uses TextMeshPro (`TextMeshProUGUI`).

## Parameters

| Parameter | Type | Default | Description |
|-----------|------|---------|-------------|
| normalColor | Color | (0.2, 0.6, 1, 1) blue | Default resting state |
| hoverColor | Color | (1, 0.8, 0.2, 1) yellow | Mouse hovering over button |
| pressedColor | Color | (1, 0.3, 0.3, 1) red | Mouse button held down |
| fadeDuration | float | 0.1s | Color transition time; 0 = instant |

## Requirements

- Unity 2022.3+ (uses uGUI `Image`, `UnityEngine.UI`).
- TextMeshPro package (`com.unity.textmeshpro`).
- The project must use URP or Built-in RP — no shader dependencies.
