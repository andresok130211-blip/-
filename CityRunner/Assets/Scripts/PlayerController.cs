using UnityEngine;

/// <summary>Процедурный человечек с анимацией ходьбы/бега/прыжка + управление через CharacterController.</summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    public float walkSpeed = 3.0f;
    public float runSpeed = 7.5f;
    public float jumpHeight = 1.1f;
    public float gravity = -22f;
    public Transform cameraTransform;

    CharacterController cc;
    Transform model, hips, torso, head, lArm, rArm, lLeg, rLeg, lShin, rShin;
    float verticalVel;
    float phase;
    float speedSmooth;
    Vector3 velocityXZ;

    public static PlayerController Instance;
    public float CurrentSpeed => speedSmooth;

    public static PlayerController Create(Vector3 position)
    {
        var go = new GameObject("Player");
        go.transform.position = position;
        var cc = go.AddComponent<CharacterController>();
        cc.height = 1.8f; cc.radius = 0.32f; cc.center = new Vector3(0, 0.9f, 0);
        cc.stepOffset = 0.3f; cc.skinWidth = 0.03f; cc.slopeLimit = 50f;
        var pc = go.AddComponent<PlayerController>();
        pc.BuildModel();
        Instance = pc;
        return pc;
    }

    void Awake() { cc = GetComponent<CharacterController>(); }

    // ---------- модель ----------
    Transform Part(string name, Transform parent, PrimitiveType type, Vector3 localPos, Vector3 scale, Material mat)
    {
        var g = GameObject.CreatePrimitive(type);
        g.name = name;
        Destroy(g.GetComponent<Collider>());
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        g.transform.localScale = scale;
        g.GetComponent<Renderer>().sharedMaterial = mat;
        return g.transform;
    }

    Transform Joint(string name, Transform parent, Vector3 localPos)
    {
        var g = new GameObject(name);
        g.transform.SetParent(parent, false);
        g.transform.localPosition = localPos;
        return g.transform;
    }

    void BuildModel()
    {
        cc = GetComponent<CharacterController>();
        var skin = ProceduralTextures.Solid(new Color(0.85f, 0.68f, 0.55f), 0.35f, 0f);
        var shirt = ProceduralTextures.Solid(new Color(0.75f, 0.12f, 0.10f), 0.3f, 0f);
        var jeans = ProceduralTextures.Solid(new Color(0.12f, 0.18f, 0.32f), 0.2f, 0f);
        var shoe = ProceduralTextures.Solid(new Color(0.9f, 0.9f, 0.9f), 0.4f, 0f);
        var hair = ProceduralTextures.Solid(new Color(0.12f, 0.08f, 0.05f), 0.4f, 0f);

        model = Joint("Model", transform, Vector3.zero);
        hips = Joint("Hips", model, new Vector3(0, 0.95f, 0));
        torso = Part("Torso", hips, PrimitiveType.Cube, new Vector3(0, 0.3f, 0), new Vector3(0.42f, 0.6f, 0.22f), shirt);
        head = Joint("Head", hips, new Vector3(0, 0.72f, 0));
        Part("Skull", head, PrimitiveType.Sphere, new Vector3(0, 0.12f, 0), new Vector3(0.22f, 0.26f, 0.24f), skin);
        Part("Hair", head, PrimitiveType.Sphere, new Vector3(0, 0.18f, -0.02f), new Vector3(0.235f, 0.22f, 0.25f), hair);
        Part("Neck", hips, PrimitiveType.Cylinder, new Vector3(0, 0.63f, 0), new Vector3(0.09f, 0.04f, 0.09f), skin);

        lArm = Joint("LArm", hips, new Vector3(-0.27f, 0.55f, 0));
        rArm = Joint("RArm", hips, new Vector3(0.27f, 0.55f, 0));
        foreach (var arm in new[] { lArm, rArm })
        {
            Part("Sleeve", arm, PrimitiveType.Capsule, new Vector3(0, -0.17f, 0), new Vector3(0.12f, 0.2f, 0.12f), shirt);
            Part("Forearm", arm, PrimitiveType.Capsule, new Vector3(0, -0.4f, 0), new Vector3(0.1f, 0.16f, 0.1f), skin);
        }
        lLeg = Joint("LLeg", hips, new Vector3(-0.11f, 0f, 0));
        rLeg = Joint("RLeg", hips, new Vector3(0.11f, 0f, 0));
        foreach (var leg in new[] { lLeg, rLeg })
        {
            Part("Thigh", leg, PrimitiveType.Capsule, new Vector3(0, -0.22f, 0), new Vector3(0.17f, 0.24f, 0.17f), jeans);
        }
        lShin = Joint("LShin", lLeg, new Vector3(0, -0.47f, 0));
        rShin = Joint("RShin", rLeg, new Vector3(0, -0.47f, 0));
        foreach (var shin in new[] { lShin, rShin })
        {
            Part("Calf", shin, PrimitiveType.Capsule, new Vector3(0, -0.2f, 0), new Vector3(0.14f, 0.22f, 0.14f), jeans);
            Part("Shoe", shin, PrimitiveType.Cube, new Vector3(0, -0.45f, 0.05f), new Vector3(0.14f, 0.08f, 0.28f), shoe);
        }
    }

    // ---------- управление ----------
    void Update()
    {
        if (cc == null || cameraTransform == null) return;

        Vector2 inp = GameInput.Move;
        Vector3 fwd = cameraTransform.forward; fwd.y = 0; fwd.Normalize();
        Vector3 right = cameraTransform.right; right.y = 0; right.Normalize();
        Vector3 wish = fwd * inp.y + right * inp.x;
        if (wish.sqrMagnitude > 1f) wish.Normalize();

        bool sprint = GameInput.Sprint;
        float targetSpeed = wish.magnitude * (sprint ? runSpeed : walkSpeed);
        Vector3 targetVel = wish.normalized * targetSpeed;
        float accel = cc.isGrounded ? 14f : 4f;
        velocityXZ = Vector3.MoveTowards(velocityXZ, targetVel, accel * Time.deltaTime);

        if (cc.isGrounded && verticalVel < 0f) verticalVel = -2f;
        if (cc.isGrounded && GameInput.JumpPressed) verticalVel = Mathf.Sqrt(jumpHeight * -2f * gravity);
        verticalVel += gravity * Time.deltaTime;

        Vector3 move = velocityXZ + Vector3.up * verticalVel;
        cc.Move(move * Time.deltaTime);

        if (velocityXZ.sqrMagnitude > 0.05f)
        {
            Quaternion want = Quaternion.LookRotation(velocityXZ.normalized, Vector3.up);
            model.rotation = Quaternion.Slerp(model.rotation, want, 1f - Mathf.Exp(-14f * Time.deltaTime));
        }

        speedSmooth = Mathf.Lerp(speedSmooth, velocityXZ.magnitude, 1f - Mathf.Exp(-10f * Time.deltaTime));
        Animate(sprint);
    }

    void Animate(bool sprint)
    {
        float s = speedSmooth;
        bool air = !cc.isGrounded;
        float stride = Mathf.Clamp01(s / runSpeed);
        phase += Time.deltaTime * Mathf.Lerp(5f, 11f, stride) * Mathf.Clamp01(s / 1.2f);

        float swing = Mathf.Lerp(28f, 55f, stride) * Mathf.Clamp01(s / 1.2f);
        float knee = Mathf.Lerp(20f, 85f, stride) * Mathf.Clamp01(s / 1.2f);
        float armSwing = Mathf.Lerp(25f, 60f, stride) * Mathf.Clamp01(s / 1.2f);
        float s1 = Mathf.Sin(phase), s2 = Mathf.Sin(phase + Mathf.PI);

        if (air)
        {
            lLeg.localRotation = Quaternion.Euler(-35f, 0, 0);
            rLeg.localRotation = Quaternion.Euler(20f, 0, 0);
            lShin.localRotation = Quaternion.Euler(50f, 0, 0);
            rShin.localRotation = Quaternion.Euler(20f, 0, 0);
            lArm.localRotation = Quaternion.Euler(-120f, 0, 15f);
            rArm.localRotation = Quaternion.Euler(-120f, 0, -15f);
        }
        else
        {
            lLeg.localRotation = Quaternion.Euler(s1 * swing, 0, 0);
            rLeg.localRotation = Quaternion.Euler(s2 * swing, 0, 0);
            lShin.localRotation = Quaternion.Euler(Mathf.Max(0f, -Mathf.Cos(phase)) * knee + 4f, 0, 0);
            rShin.localRotation = Quaternion.Euler(Mathf.Max(0f, -Mathf.Cos(phase + Mathf.PI)) * knee + 4f, 0, 0);
            lArm.localRotation = Quaternion.Euler(s2 * armSwing, 0, 4f);
            rArm.localRotation = Quaternion.Euler(s1 * armSwing, 0, -4f);
        }
        // наклон корпуса и покачивание таза
        float lean = Mathf.Lerp(0f, 14f, stride);
        hips.localRotation = Quaternion.Euler(lean, 0, 0);
        float bob = Mathf.Abs(Mathf.Cos(phase)) * 0.05f * Mathf.Clamp01(s / 1.2f);
        hips.localPosition = new Vector3(0, 0.95f - 0.03f * stride + bob, 0);
    }
}
