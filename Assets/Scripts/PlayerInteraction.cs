using UnityEngine;
using TMPro;                  // Remove if not using TextMeshPro — swap for UnityEngine.UI.Text

/// <summary>
/// Player 2 - Interaction System
/// Casts a ray from the camera to detect IInteractable objects.
/// Drives the HUD prompt ("E to pick up body", etc.)
/// Tracks what Player 2 is currently carrying.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [Header("Raycast Settings")]
    [SerializeField] private float interactRange = 2.5f;
    [SerializeField] private LayerMask interactableMask;   // Set to your "Interactable" layer in Inspector
    [SerializeField] private Transform cameraTransform;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI promptText;   // Assign a UI Text in Inspector
    [SerializeField] private GameObject promptPanel;       // The panel that wraps the prompt

    [Header("Carry Settings")]
    [SerializeField] private Transform carryAnchor;        // Empty child in front of camera for held items
    
    // --- Public state readable by other systems ---
    public bool IsCarryingBody { get; private set; }
    public bool IsCarryingItem { get; private set; }
    public GameObject CarriedItem { get; private set; }

    private IInteractable _currentTarget;

    private void Update()
    {
        ScanForInteractable();

        if (Input.GetKeyDown(KeyCode.E) && _currentTarget != null && _currentTarget.CanInteract(this))
            _currentTarget.Interact(this);

        // Drop carried item
        if (Input.GetKeyDown(KeyCode.G) && IsCarryingItem)
            DropItem();
    }

    private void ScanForInteractable()
    {
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

        // Nothing found
        _currentTarget = null;
        HidePrompt();
    }

    // ---- Called by Interactable objects ----

    /// <summary>Attach an item to the carry anchor (e.g. body in a trash bag, weapon)</summary>
    public void PickUpItem(GameObject item, bool isBody = false)
    {
        if (CarriedItem != null) return; // Already holding something

        CarriedItem = item;
        IsCarryingBody = isBody;
        IsCarryingItem = true;

        item.transform.SetParent(carryAnchor);
        item.transform.localPosition = Vector3.zero;
        item.transform.localRotation = Quaternion.identity;

        // Disable physics while held
        Rigidbody rb = item.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = true;

        Collider col = item.GetComponent<Collider>();
        if (col != null) col.enabled = false;
    }

    /// <summary>Remove item from carry anchor and place it in the world</summary>
    public void DropItem()
    {
        if (CarriedItem == null) return;

        CarriedItem.transform.SetParent(null);

        Rigidbody rb = CarriedItem.GetComponent<Rigidbody>();
        if (rb != null) rb.isKinematic = false;

        Collider col = CarriedItem.GetComponent<Collider>();
        if (col != null) col.enabled = true;

        CarriedItem = null;
        IsCarryingBody = false;
        IsCarryingItem = false;
    }

    /// <summary>Silently remove item (e.g. placed into locker — item is destroyed or hidden)</summary>
    public void ConsumeCarriedItem()
    {
        if (CarriedItem == null) return;
        Destroy(CarriedItem);
        CarriedItem = null;
        IsCarryingBody = false;
        IsCarryingItem = false;
    }

    // ---- HUD helpers ----
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
