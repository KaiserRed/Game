using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerCamera : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private InputActionAsset inputActions;
    [SerializeField] private Transform target;
    [SerializeField] private Camera childCamera;

    [Header("Offset")]
    [SerializeField] private Vector3 targetOffset = new Vector3(0f, 1.4f, 0f);
    [SerializeField, Min(0.5f)] private float distance = 6f;

    [Header("Rotation Limits")]
    [SerializeField] private float pitchMin = -25f;
    [SerializeField] private float pitchMax = 65f;

    [Header("Sensitivity")]
    [SerializeField] private float yawSensitivity = 180f;
    [SerializeField] private float pitchSensitivity = 120f;
    [SerializeField] private bool invertY = false;

    [Header("Smoothing")]
    [SerializeField] private bool smoothFollow = true;
    [SerializeField, Min(0f)] private float positionSmoothTime = 0.06f;

    [Header("Cursor")]
    [SerializeField] private bool lockCursor = true;

    private InputAction lookAction;
    private float yaw;
    private float pitch = 20f;
    private Vector3 posVel;

    private void Awake()
    {
        if (childCamera == null) childCamera = GetComponentInChildren<Camera>();

        if (inputActions != null)
        {
            var map = inputActions.FindActionMap("Player", true);
            lookAction = map.FindAction("Look", true);
        }

        if (childCamera != null)
        {
            childCamera.transform.localPosition = new Vector3(0f, 0f, -distance);
            childCamera.transform.localRotation = Quaternion.identity;
        }

        yaw = transform.eulerAngles.y;
    }

    private void OnEnable()
    {
        lookAction?.Enable();
        if (lockCursor)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private void OnDisable()
    {
        lookAction?.Disable();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        if (lookAction == null) return;
        Vector2 look = lookAction.ReadValue<Vector2>();
        yaw += look.x * yawSensitivity * Time.deltaTime;
        pitch += (invertY ? look.y : -look.y) * pitchSensitivity * Time.deltaTime;
        pitch = Mathf.Clamp(pitch, pitchMin, pitchMax);
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPos = target.position + targetOffset;
        if (smoothFollow)
            transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVel, positionSmoothTime);
        else
            transform.position = desiredPos;

        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);

        if (childCamera != null)
        {
            childCamera.transform.localPosition = new Vector3(0f, 0f, -distance);
            childCamera.transform.localRotation = Quaternion.identity;
        }
    }
}
