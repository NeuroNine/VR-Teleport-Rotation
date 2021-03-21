using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;

public class TeleportationManager : MonoBehaviour
{
    [SerializeField] private InputActionAsset actionAsset;
    [SerializeField] private XRRayInteractor xrRayInteractor;
    [SerializeField] private TeleportationProvider provider;

    [BoxGroup("Player Attributes")] [SerializeField]
    private new Transform camera;

    [BoxGroup("Debug")] [SerializeField] private bool debugOn;
    [BoxGroup("Debug")] [SerializeField] private TMP_Text angleText;
    [BoxGroup("Debug")] [SerializeField] private TMP_Text rotationText;
    private InputAction _thumbstick;
    private bool _isActive;
    private Vector2 _thumbstickDirection;

    // Start is called before the first frame update
    private void Start()
    {
        TurnOffTeleport();
        CheckDebugValue();
        
        var inputActionMap = actionAsset.FindActionMap("XRI LeftHand");

        var activate = inputActionMap.FindAction("Teleport Mode Activate");
        activate.Enable();
        activate.performed += OnTeleportActivate;

        var cancel = inputActionMap.FindAction("Teleport Mode Cancel");
        cancel.Enable();
        cancel.performed += OnTeleportCancel;

        _thumbstick = inputActionMap.FindAction("Move");
        _thumbstick.Enable();
    }

    // Update is called once per frame
    private void Update()
    {
        CheckDebugValue();
        SetRotationText();
        
        if (!_isActive)
        {
            return;
        }

        if (_thumbstick.triggered)
        {
            Vector2 thumbstickValue = _thumbstick.ReadValue<Vector2>();
            // if the thumbstick is pushed far enough in any direction we'll
            // save it off for use in the rotation angle calculation.
            if (thumbstickValue.sqrMagnitude >= 1)
            {
                _thumbstickDirection = thumbstickValue.normalized;
            }

            return;
        }

        if (!xrRayInteractor.TryGetCurrent3DRaycastHit(out var hit))
        {
            TurnOffTeleport();
            return;
        }

        var rotationAngle = CalculateTeleportRotationAngle();
        var request = new TeleportRequest()
        {
            destinationPosition = hit.point,
            destinationRotation = Quaternion.Euler(0,
                rotationAngle, 0),
            matchOrientation = MatchOrientation.TargetUpAndForward
        };

        provider.QueueTeleportRequest(request);
        TurnOffTeleport();
    }

    private void OnTeleportActivate(InputAction.CallbackContext callbackContext)
    {
        xrRayInteractor.enabled = true;
        _isActive = true;
    }

    private void OnTeleportCancel(InputAction.CallbackContext callbackContext)
    {
        TurnOffTeleport();
    }

    private void TurnOffTeleport()
    {
        xrRayInteractor.enabled = false;
        _isActive = false;
        _thumbstickDirection = Vector2.zero;
    }

    /// <summary>
    /// Calculates the angle by looking at the thumbstick direction, converting
    /// it into an angle, grabbing the camera's Y angle, and adding those two
    /// together.
    /// </summary>
    /// <returns>An angle in degrees as a float.</returns>
    private float CalculateTeleportRotationAngle()
    {
        var thumbstickAngle = Mathf.Atan2(_thumbstickDirection.x,
                                  _thumbstickDirection.y)
                              * Mathf.Rad2Deg;
        var cameraYAngle = camera.eulerAngles.y;
        SetAngleText(thumbstickAngle);

        return thumbstickAngle + cameraYAngle;
    }

    private void CheckDebugValue()
    {
        rotationText.gameObject.SetActive(debugOn);
        angleText.gameObject.SetActive(debugOn);
    }

    private void SetRotationText()
    {
        if (debugOn)
        {
            rotationText.text = "Rotation: " + camera.eulerAngles.y;
        }
    }

    private void SetAngleText(float angle)
    {
        if (debugOn)
        {
            angleText.text = "Angle: " + angle;
        }
    }
}