using UnityEngine;
#if ENABLE_INPUT_SYSTEM && !ENABLE_LEGACY_INPUT_MANAGER
using UnityEngine.InputSystem;
#endif

/// <summary>
/// Единая обёртка над вводом: работает и со старым Input Manager, и с новым Input System.
/// </summary>
public static class GameInput
{
#if ENABLE_LEGACY_INPUT_MANAGER
    public static Vector2 Move
    {
        get
        {
            var v = new Vector2(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"));
            if (TouchControls.Active) v += TouchControls.Instance.move;
            return Vector2.ClampMagnitude(v, 1f);
        }
    }
    public static Vector2 Look
    {
        get
        {
            var v = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            if (TouchControls.Active) v = TouchControls.Instance.look;
            return v;
        }
    }
    public static bool JumpPressed => Input.GetKeyDown(KeyCode.Space) || (TouchControls.Active && TouchControls.Instance.jump);
    public static bool Sprint => Input.GetKey(KeyCode.LeftShift) || (TouchControls.Active && TouchControls.Instance.sprint);
    public static float Scroll => Input.mouseScrollDelta.y;
    public static bool EscPressed => Input.GetKeyDown(KeyCode.Escape);
    public static bool ClickPressed => Input.GetMouseButtonDown(0);
    public static bool TimeJumpPressed => Input.GetKeyDown(KeyCode.T);
    public static bool TimePausePressed => Input.GetKeyDown(KeyCode.Y);
    public static bool HelpPressed => Input.GetKeyDown(KeyCode.F1);
#elif ENABLE_INPUT_SYSTEM
    public static Vector2 Move
    {
        get
        {
            var k = Keyboard.current;
            if (k == null) return Vector2.zero;
            float x = (k.dKey.isPressed ? 1f : 0f) - (k.aKey.isPressed ? 1f : 0f);
            float y = (k.wKey.isPressed ? 1f : 0f) - (k.sKey.isPressed ? 1f : 0f);
            return new Vector2(x, y);
        }
    }
    public static Vector2 Look => Mouse.current != null ? Mouse.current.delta.ReadValue() * 0.05f : Vector2.zero;
    public static bool JumpPressed => Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
    public static bool Sprint => Keyboard.current != null && Keyboard.current.leftShiftKey.isPressed;
    public static float Scroll => Mouse.current != null ? Mouse.current.scroll.ReadValue().y / 120f : 0f;
    public static bool EscPressed => Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
    public static bool ClickPressed => Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
    public static bool TimeJumpPressed => Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
    public static bool TimePausePressed => Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame;
    public static bool HelpPressed => Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame;
#else
    public static Vector2 Move => Vector2.zero;
    public static Vector2 Look => Vector2.zero;
    public static bool JumpPressed => false;
    public static bool Sprint => false;
    public static float Scroll => 0f;
    public static bool EscPressed => false;
    public static bool ClickPressed => false;
    public static bool TimeJumpPressed => false;
    public static bool TimePausePressed => false;
    public static bool HelpPressed => false;
#endif
}
