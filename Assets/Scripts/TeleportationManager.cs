using NaughtyAttributes;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
[RequireComponent(typeof(TeleportationProvider))]
public class TeleportationManager : MonoBehaviour
{
    [BoxGroup("XR")] [SerializeField] private XRRayInteractor xrRayInteractor;
    
    [BoxGroup("Input")] [SerializeField] private InputActionAsset actionAsset;
    
    [BoxGroup("Player Attributes")] [SerializeField]
    private new Transform camera;
    
    [BoxGroup("Debug")] [SerializeField] private bool debugOn;
    [BoxGroup("Debug")] [SerializeField] private TMP_Text angleText;
    [BoxGroup("Debug")] [SerializeField] private TMP_Text rotationText;

    private TeleportationProvider _teleportationProvider;
    private bool _isTeleportActive;
    private InputAction _thumbstick;
    private Vector2 _thumbstickDirection;


    // Start is called before the first frame update
    private void Start()
    {
        SetDebug();
        TurnOffTeleport();

        _teleportationProvider = GetComponent<TeleportationProvider>(); 
        
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
        SetDebug();
        SetRotationText();

        if (!_isTeleportActive)
        {
            return;
        }

        if (_thumbstick.triggered)
        {
            var thumbstickValue = _thumbstick.ReadValue<Vector2>();
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

        _teleportationProvider.QueueTeleportRequest(request);
        TurnOffTeleport();
    }

    private void OnTeleportActivate(InputAction.CallbackContext callbackContext)
    {
        xrRayInteractor.enabled = true;
        _isTeleportActive = true;
    }

    private void OnTeleportCancel(InputAction.CallbackContext callbackContext)
    {
        TurnOffTeleport();
    }

    private void TurnOffTeleport()
    {
        xrRayInteractor.enabled = false;
        _isTeleportActive = false;
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
        SetAngleText(thumbstickAngle);
        return thumbstickAngle + camera.eulerAngles.y;
    }

    private void SetDebug()
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