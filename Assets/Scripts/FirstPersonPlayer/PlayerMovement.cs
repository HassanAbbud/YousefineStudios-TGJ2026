using Lobby;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Player 1 (first-person) movement controller.
/// Networked: input only runs for the owner client when they actually picked
/// the first-person role. Position syncs via ClientNetworkTransform (owner authoritative).
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : NetworkBehaviour
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
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private Transform cameraHolder;

    private CharacterController _cc;
    private Vector3 _velocity;
    private float _xRotation;
    private bool _isCrouching;
    private float _targetHeight;
    private float _targetCameraY;

    public bool IsMoving { get; private set; }
    public bool IsCrouching => _isCrouching;

    private bool _amFirstPerson;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        _cc.height = standHeight;
        _cc.center = new Vector3(0, standHeight / 2f, 0);
        _targetHeight = standHeight;
        _targetCameraY = standHeight * 0.85f;

        if (cameraHolder != null)
        {
            Vector3 pos = cameraHolder.localPosition;
            pos.y = _targetCameraY;
            cameraHolder.localPosition = pos;
        }
    }

    public override void OnNetworkSpawn()
    {
        _amFirstPerson = IsOwner
            && PlayerSession.Instance != null
            && PlayerSession.Instance.SelectedRole == PlayerRole.Player1_FirstPerson;

        if (_amFirstPerson)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        if (!IsOwner)
        {
            // Disable the CharacterController for non-owners — they receive position
            // updates via ClientNetworkTransform, no need for local CC simulation.
            _cc.enabled = false;
        }
    }

    private void Update()
    {
        // Only the owning first-person player runs input.
        if (!IsOwner || !_amFirstPerson) return;

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
        _cc.height = Mathf.Lerp(_cc.height, _targetHeight, Time.deltaTime * crouchTransitionSpeed);
        _cc.center = new Vector3(0, _cc.height / 2f, 0);

        _targetCameraY = _isCrouching ? crouchHeight * 0.8f : standHeight * 0.85f;
        Vector3 camPos = cameraHolder.localPosition;
        camPos.y = Mathf.Lerp(camPos.y, _targetCameraY, Time.deltaTime * crouchTransitionSpeed);
        cameraHolder.localPosition = camPos;
    }

    private void HandleMovement()
    {
        if (_cc.isGrounded && _velocity.y < 0)
            _velocity.y = -2f;

        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");

        IsMoving = (Mathf.Abs(x) > 0.05f || Mathf.Abs(z) > 0.05f);

        float speed = _isCrouching ? crouchSpeed : walkSpeed;
        Vector3 move = transform.right * x + transform.forward * z;
        _cc.Move(move * speed * Time.deltaTime);

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

    public void SetInputEnabled(bool enabled)
    {
        this.enabled = enabled;
        if (!_amFirstPerson) return;
        Cursor.lockState = enabled ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !enabled;
    }
}