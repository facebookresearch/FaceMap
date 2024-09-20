using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class responsible for object interaction.
/// </summary>
public class ObjectInteraction : MonoBehaviour
{
    // Interaction modifiers
    public bool autoRatioationEnabled = true;
    public bool rotationAllowed = true;
    public bool zoomAllowed = true;
    public bool translationAllowed = true;
    public bool onlyUPAxisRotation = false;

    // Rotation parameters
    float manualRotateSpeed = 5.0f;

    // Zoom temporary variable
    float wheelRotation = 0;
    float scaleSpeed = 0.03f;

    // Autorotation parameters
    float idleStartTime = 0.0f;
    float idleDuration = 0.2f;
    float autorotationEaseTime = 0.5f;
    float autoRotateSpeed = 10.0f;

    // Translation temporary variables
    private Vector3 screenPoint;
    private Vector3 offset;

    // Reset parameters
    float resetStartTime = 0.0f;
    float resetDuration = 2.0f;
    // Reset temporary variables
    Quaternion resetQuaternion;
    Vector3 resetScale;
    Vector3 resetPosition;

    // variable that stores current object state
    private InteractionState state;

    // rotate back and forth
    public float totalRotateY = 0;
    public bool rotateLeft = true;
    // Start is called before the first frame update
    void Start()
    {
        SetState(InteractionState.Idle);
    }


    // Update is called once per frame
    void Update()
    {
        if (SessionData.LockedUserInput)
            return;

        TriggerAutoRotation();
        UserInput();
        ObjectTransform();
    }

    /// <summary>
    /// Takes user mouse and keyboard input and sets states accordingly.
    /// </summary>
    void UserInput()
    {
        // Rotation
        if (rotationAllowed)
        {
            if (Input.GetKeyDown(KeyCode.Mouse1))
            {
                SetState(InteractionState.Rotation);
            }
            if (Input.GetKeyUp(KeyCode.Mouse1))
            {
                SetState(InteractionState.Idle);
            }
        }

        // Translation
        if (translationAllowed)
        {
            if (Input.GetKeyDown(KeyCode.Mouse2))
            {
                SetState(InteractionState.Translation);
            }
            if (Input.GetKeyUp(KeyCode.Mouse2))
            {
                SetState(InteractionState.Idle);
            }
        }

        // Zoom
        if (zoomAllowed)
        {
            if (Input.GetAxis("Mouse ScrollWheel") != 0)
            {
                SetState(InteractionState.Zoom);
                wheelRotation = Input.GetAxis("Mouse ScrollWheel");
            }
        }

        // Reseting
        if (Input.GetKeyDown(KeyCode.R))
        {
            SetState(InteractionState.Reset);
        }
    }


    /// <summary>
    /// Transforms objects according to active state.
    /// </summary>
    void ObjectTransform()
    {
        switch (state)
        {
            case InteractionState.AutoRotation:
                AutoRotate();
                break;
            case InteractionState.Rotation:
                ManualRotate();
                break;
            case InteractionState.Translation:
                ManualTranslate();
                break;
            case InteractionState.Zoom:
                ManualScale();
                break;
            case InteractionState.Reset:
                ResetTranslationRotationAndScale();
                break;
            default:
                break;
        }
    }


    /// <summary>
    /// Definition of manual rotation.
    /// </summary>
    private void ManualRotate()
    {
        float rotX = Input.GetAxis("Mouse X") * manualRotateSpeed;
        float rotY = Input.GetAxis("Mouse Y") * manualRotateSpeed;
        if (onlyUPAxisRotation)
            rotY = 0;
        totalRotateY -= rotX;
        transform.Rotate(Vector3.up, -rotX, Space.World);
        transform.Rotate(Vector3.right, rotY, Space.World);
    }


    /// <summary>
    /// Definition of manual translation.
    /// </summary>
    private void ManualTranslate()
    {
        Vector3 curScreenPoint = new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z);
        Vector3 curPosition = Camera.main.ScreenToWorldPoint(curScreenPoint) + offset;
        transform.position = curPosition;
    }


    /// <summary>
    /// Definition of manual zoom.
    /// </summary>
    private void ManualScale()
    {
        Vector3 scaleVector = new Vector3(scaleSpeed, scaleSpeed, scaleSpeed);

        if (wheelRotation > 0f) // forward
            transform.localScale *= (1f + scaleSpeed);
        else if (wheelRotation < 0f) // backwards
            transform.localScale *= (1f - scaleSpeed);

        SetState(InteractionState.Idle);
    }


    /// <summary>
    /// Definition of autorotation.
    /// </summary>
    private void AutoRotate()
    {
        float currentEaseTime = Time.time - idleStartTime;
        float currentEaseFraction = Mathf.Clamp01(currentEaseTime / autorotationEaseTime);
        currentEaseFraction = Easing.Quadratic.In(currentEaseFraction);

        float currentAutoRotateSpeed = Mathf.Lerp(0, autoRotateSpeed, currentEaseFraction);

        float rotate = 0;
        if (totalRotateY > 180 + 60)
            rotateLeft = false;
        if (totalRotateY < 120)
            rotateLeft = true;

        rotate = Time.deltaTime * currentAutoRotateSpeed;
        if (!rotateLeft)
            rotate = -rotate;

        totalRotateY += rotate;
        // rotate around up vector
        transform.Rotate(Vector3.up, rotate, Space.World);
    }


    /// <summary>
    /// Checks how long object was in idle state and starts autorotation if allowed.
    /// </summary>
    private void TriggerAutoRotation()
    {
        if (!autoRatioationEnabled)
            return;

        if (Time.time - idleStartTime >= idleDuration && state == InteractionState.Idle)
            state = InteractionState.AutoRotation;
    }


    /// <summary>
    /// Resets object transform to its original transform.
    /// </summary>
    private void ResetTranslationRotationAndScale()
    {
            float currentResetTime = Time.time - resetStartTime;
            float currentResetFraction = Mathf.Clamp01((Time.time - resetStartTime)/resetDuration);
            currentResetFraction = Easing.Quadratic.InOut(currentResetFraction);
            transform.rotation = Quaternion.Lerp(resetQuaternion, Quaternion.identity, currentResetFraction);
            transform.localScale = Vector3.Lerp(resetScale, Vector3.one, currentResetFraction);
            transform.position = Vector3.Lerp(resetPosition, Vector3.zero, currentResetFraction);

            if (currentResetTime >= resetDuration)
                SetState(InteractionState.Idle);
    }


    /// <summary>
    /// Sets state according to passed parameter and sets appropriate variables for given state.
    /// </summary>
    /// <param name="state"></param>
    private void SetState(InteractionState state)
    {
        switch (state)
        {
            case InteractionState.Idle:
                idleStartTime = Time.time;
                break;
            case InteractionState.AutoRotation:
                break;
            case InteractionState.Rotation:
                break;
            case InteractionState.Translation:
                screenPoint = Camera.main.WorldToScreenPoint(gameObject.transform.position);
                offset = gameObject.transform.position - Camera.main.ScreenToWorldPoint(new Vector3(Input.mousePosition.x, Input.mousePosition.y, screenPoint.z));
                break;
            case InteractionState.Zoom:
                break;
            case InteractionState.Reset:
                resetStartTime = Time.time;
                resetQuaternion = transform.rotation;
                resetScale = transform.localScale;
                resetPosition = transform.position;
                break;
            default:
                break;
        }

        this.state = state;
    }


    /// <summary>
    /// Object interaction state machine enum definition.
    /// </summary>
    private enum InteractionState
    {
        Idle,
        AutoRotation,
        Rotation,
        Translation,
        Zoom,
        Reset
    }
}
