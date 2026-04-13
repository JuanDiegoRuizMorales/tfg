using Nekalypse.PlayerControl;
using UnityEngine;

public class SwayAndBob : MonoBehaviour
{
    // Referencia al PlayerController para leer estados como si esta en el suelo
    public PlayerController playerController;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    Vector3 swayPos;

    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    Vector3 swayEulerRot;

    public float smooth = 10f;
    float smoothRot = 12f;

    [Header("Bobbing")]
    public float speedCurve;
    float curveSin { get => Mathf.Sin(speedCurve); }
    float curveCos { get => Mathf.Cos(speedCurve); }

    public Vector3 travelLimit = Vector3.one * 0.025f;
    public Vector3 bobLimit = Vector3.one * 0.01f;
    Vector3 bobPosition;

    public float bobExaggeration;

    [Header("Bob Rotation")]
    public Vector3 multiplier;
    Vector3 bobEulerRotation;

    // POSICIÓN BASE original del WeaponHolder (soluciona el bug)

    private Vector3 originalLocalPos;


    private void Start()
    {
        // Guardamos la posición base del WeaponHolder
        originalLocalPos = transform.localPosition;
    }

    void Update()
    {
        GetInput();

        Sway();
        SwayRotation();
        BobOffset();
        BobRotation();

        CompositePositionRotation();
    }


    Vector2 walkInput;
    Vector2 lookInput;

    void GetInput()
    {
        walkInput.x = Input.GetAxis("Horizontal");
        walkInput.y = Input.GetAxis("Vertical");
        walkInput = walkInput.normalized;

        lookInput.x = Input.GetAxis("Mouse X");
        lookInput.y = Input.GetAxis("Mouse Y");
    }


    void Sway()
    {
        Vector3 invertLook = lookInput * -step;

        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        swayPos = invertLook;
    }

    void SwayRotation()
    {
        Vector2 invertLook = lookInput * -rotationStep;

        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);

        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    void CompositePositionRotation()
    {
        // CORRECCIÓN:
        // En lugar de usar solo swayPos + bobPosition,
        // sumamos la posición base del arma para no perder el offset inicial.

        Vector3 targetPos = originalLocalPos + swayPos + bobPosition;

        transform.localPosition = Vector3.Lerp(
            transform.localPosition,
            targetPos,
            Time.deltaTime * smooth
        );

        transform.localRotation =
            Quaternion.Slerp(
                transform.localRotation,
                Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation),
                Time.deltaTime * smoothRot
            );
    }

    void BobOffset()
    {
        speedCurve += Time.deltaTime *
            (playerController.isGrounded
            ? (Input.GetAxis("Horizontal") + Input.GetAxis("Vertical")) * bobExaggeration
            : 1f)
            + 0.01f;

        bobPosition.x =
            (curveCos * bobLimit.x * (playerController.isGrounded ? 1 : 0))
            - (walkInput.x * travelLimit.x);

        bobPosition.y =
            (curveSin * bobLimit.y)
            - (Input.GetAxis("Vertical") * travelLimit.y);

        bobPosition.z = -(walkInput.y * travelLimit.z);
    }

    void BobRotation()
    {
        bobEulerRotation.x =
            (walkInput != Vector2.zero
            ? multiplier.x * Mathf.Sin(2 * speedCurve)
            : multiplier.x * (Mathf.Sin(2 * speedCurve) / 2));

        bobEulerRotation.y =
            (walkInput != Vector2.zero
            ? multiplier.y * curveCos
            : 0);

        bobEulerRotation.z =
            (walkInput != Vector2.zero
            ? multiplier.z * curveCos * walkInput.x
            : 0);
    }
}
