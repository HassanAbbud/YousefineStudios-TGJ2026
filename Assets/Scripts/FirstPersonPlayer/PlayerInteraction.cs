using Lobby;
using Unity.Netcode;
using UnityEngine;
using TMPro;

/// <summary>
/// Player 1 - Interaction System. Owner-only input.
/// Lives on the shared NetworkPlayer prefab — gates by role so it doesn't run
/// on Player 2 (camera operator).
/// </summary>
public class PlayerInteraction : NetworkBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactableMask;
    [SerializeField] private Transform cameraTransform;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI promptText;
    [SerializeField] private GameObject promptPanel;

    [Header("Carry Settings")]
    [SerializeField] private Transform carryAnchor;

    public bool IsCarryingBody { get; private set; }
    public bool IsCarryingBag  { get; private set; }   // true only when holding the bagged body
    public bool IsCarryingItem { get; private set; }
    public GameObject CarriedItem { get; private set; }

    public bool HasCleaningKit { get; set; }   // set by CleaningSupplyInteractable
    private IInteractable _currentTarget;

    private bool _amFirstPerson;

    public override void OnNetworkSpawn()
    {
        _amFirstPerson = IsOwner
            && PlayerSession.Instance != null
            && PlayerSession.Instance.SelectedRole == PlayerRole.Player1_FirstPerson;

        // Make sure the prompt UI is hidden for everyone but the owning FP player.
        if (!_amFirstPerson)
        {
            if (promptPanel != null) promptPanel.SetActive(false);
        }
    }

    private void Update()
    {
        // Only the actual first-person owner runs raycasts and reads input.
        if (!IsOwner || !_amFirstPerson) return;

        ScanForInteractable();

        if (Input.GetKeyDown(KeyCode.E) && _currentTarget != null && _currentTarget.CanInteract(this))
            _currentTarget.Interact(this);

        if (Input.GetKeyDown(KeyCode.G) && IsCarryingItem)
            DropItem();
    }

    private void ScanForInteractable()
    {
        if (cameraTransform == null) return;

        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactableMask))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

            if (interactable != null)
            {
                _currentTarget = interactable;
                ShowPrompt(interactable.CanInteract(this)
                    ? interactable.GetPromptText()
                    : "<color=grey>" + interactable.GetPromptText() + "</color>");
                return;
            }
        }

        _currentTarget = null;
        HidePrompt();
    }

    public void PickUpItem(GameObject item, bool isBody = false, bool isBag = false)
    {
        if (CarriedItem != null) return;

        CarriedItem = item;
        IsCarryingBody = isBody;
        IsCarryingBag  = isBag;
        IsCarryingItem = true;

        item.transform.SetParent(carryAnchor);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = item.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    public void DropItem()
    {
        if (CarriedItem == null) return;

        CarriedItem.GetComponentInParent<IDropNotify>()?.OnDropped();

        CarriedItem.transform.SetParent(null);

        Rigidbody rb = CarriedItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        Collider col = CarriedItem.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        CarriedItem = null;
        IsCarryingBody = false;
        IsCarryingBag  = false;
        IsCarryingItem = false;
    }

    public void ConsumeCarriedItem()
    {
        if (CarriedItem == null) return;
        Destroy(CarriedItem);
        CarriedItem = null;
        IsCarryingBody = false;
        IsCarryingBag  = false;
        IsCarryingItem = false;
    }

    private void ShowPrompt(string text)
    {
        if (promptPanel != null) promptPanel.SetActive(true);
        if (promptText != null) promptText.text = text;
    }

    private void HidePrompt()
    {
        if (promptPanel != null) promptPanel.SetActive(false);
    }
}