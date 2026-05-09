using UnityEngine;

/// <summary>
/// Player 2 - First Person Movement Controller
/// Handles: WASD movement, mouse look, crouching, footstep audio toggle
/// Attach to: A GameObject with CharacterController + Camera child
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 3.5f;
    [SerializeField] private float crouchSpeed = 1.8f;
    [SerializeField] private float gravity = -9.81f;

    [Header("Mouse Look")]
    [SerializeField] private float mouseSensitivity = 2f;
    [SerializeField] private float maxLookAngle = 80f;

    [Header("Crouching")]
    [SerializeField] private float standHeight = 2f;
    [SerializeField] private float crouchHeight = 1f;
    [SerializeField] private float crouchTransitionSpeed = 8f;

    [Header("References")]
    [SerializeField] private Transform cameraTransform;   // Child camera object
    [SerializeField] private Transform cameraHolder;      // Empty parent that moves vertically for crouch bob

    // Internal state
    private CharacterController _cc;
    private Vector3 _velocity;
    private float _xRotation;
    private bool _isCrouching;
    private float _targetHeight;
    private float _targetCameraY;

    // Publicly readable for other systems (e.g. NPC detection — moving player is louder)
    public bool IsMoving { get; private set; }
    public bool IsCrouching => _isCrouching;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        // Force CharacterController to match standHeight regardless of Inspector values.
        // Center Y = height/2 so the pivot sits at the player's feet, not their torso.
        _cc.height = standHeight;
        _cc.center = new Vector3(0, standHeight / 2f, 0);
        _targetHeight = standHeight;
        _targetCameraY = standHeight * 0.85f;

        // Snap cameraHolder to eye height immediately — prevents it lerping up from 0 on play
        if (cameraHolder != null)
        {
            Vector3 pos = cameraHolder.localPosition;
            pos.y = _targetCameraY;
            cameraHolder.localPosition = pos;
        }

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleMouseLook();
        HandleCrouch();
        HandleMovement();
    }

    private void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        _xRotation -= mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -maxLookAngle, maxLookAngle);

        cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    private void HandleCrouch()
    {
        if (Input.GetKeyDown(KeyCode.C))
            _isCrouching = !_isCrouching;

        _targetHeight = _isCrouching ? crouchHeight : standHeight;

        // Smoothly adjust CharacterController height
        _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Time.deltaTime * crouchTransitionSpeed);

        // Keep controller grounded by adjusting center
        _cc.center = new Vector3(0, _cc.height / 2f, 0);

        // Move camera down with crouch
        _targetCameraY = _isCrouching ? crouchHeight * 0.8f : standHeight * 0.85f;
        Vector3 camPos = cameraHolder.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, _targetCameraY, Time.deltaTime * crouchTransitionSpeed);
        cameraHolder.localPosition = camPos;
    }

    private void HandleMovement()
    {
        // Grounded gravity reset
        if (_cc.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        IsMoving = (Mathf.Abs(x) > 0.05f || Mathf.Abs(z) > 0.05f);

        float speed = _isCrouching ? crouchSpeed : walkSpeed;
        Vector3 move = transform.right * x + transform.forward * z;
        _cc.Move(move * speed * Time.deltaTime);

        // Gravity
        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    // Called by networking layer to unlock/lock input (e.g. during cutscenes)
    public void SetInputEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!enabled) Cursor.lockState = CursorLockMode.None;
        else Cursor.lockState = CursorLockMode.Locked;
    }
}