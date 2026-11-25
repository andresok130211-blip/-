using UnityEngine;

/// <summary>Камера от третьего лица: мышь вращает, колесо — зум, столкновение со стенами.</summary>
public class ThirdPersonCamera : MonoBehaviour
{
    public Transform target;
    public float distance = 5.5f;
    public float minDistance = 2f, maxDistance = 16f;
    public float heightOffset = 1.6f;
    public float sensitivity = 2.2f;
    public LayerMask obstacles = ~0;

    float yaw, pitch = 15f;
    float currentDist;
    bool cursorLocked;

    void Start()
    {
        currentDist = distance;
        yaw = target != null ? target.eulerAngles.y : 0f;
        if (Application.isMobilePlatform) cursorLocked = true; else SetCursor(true);
    }

    void SetCursor(bool locked)
    {
        cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    void Update()
    {
        if (Application.isMobilePlatform || TouchControls.Active) return; // на телефоне камера всегда активна
        if (GameInput.EscPressed) SetCursor(!cursorLocked);
        if (!cursorLocked && GameInput.ClickPressed) SetCursor(true);
    }

    void LateUpdate()
    {
        if (target == null) return;
        if (cursorLocked)
        {
            Vector2 look = GameInput.Look;
            yaw += look.x * sensitivity;
            pitch -= look.y * sensitivity;
            pitch = Mathf.Clamp(pitch, -35f, 70f);
        }
        float scroll = GameInput.Scroll;
        if (Mathf.Abs(scroll) > 0.01f) distance = Mathf.Clamp(distance - Mathf.Sign(scroll) * 1.0f, minDistance, maxDistance);

        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 pivot = target.position + Vector3.up * heightOffset;
        Vector3 dir = rot * Vector3.back;

        float wanted = distance;
        RaycastHit hit;
        if (Physics.SphereCast(pivot, 0.25f, dir, out hit, distance, obstacles, QueryTriggerInteraction.Ignore))
            wanted = Mathf.Max(0.6f, hit.distance - 0.1f);
        currentDist = Mathf.Lerp(currentDist, wanted, 1f - Mathf.Exp(-(wanted < currentDist ? 30f : 6f) * Time.deltaTime));

        transform.position = pivot + dir * currentDist;
        transform.rotation = rot;
    }
}
