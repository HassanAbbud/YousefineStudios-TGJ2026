using UnityEngine;

/// <summary>
/// Step between body pickup and locker.
/// Player carries body → brings it to the bag → body merges into bag
/// → player now carries the bag → bring to locker to complete objective.
/// </summary>
public class GarbageBagInteractable : MonoBehaviour, IInteractable, IDropNotify
{
    [Tooltip("The dirty bag model (body inside). Swaps to this when body is loaded.")]
    [SerializeField] private GameObject dirtyBagVisual;   // garbage_bag_dirty model
    [Tooltip("The clean empty bag sitting in the world.")]
    [SerializeField] private GameObject cleanBagVisual;   // garbage_bag_clean model

    private bool _bodyLoaded;
    private bool _pickedUp;

    private void Awake()
    {
        // Start showing the clean bag
        if (cleanBagVisual != null) cleanBagVisual.SetActive(true);
        if (dirtyBagVisual != null) dirtyBagVisual.SetActive(false);
    }

    public string GetPromptText()
    {
        if (_pickedUp) return "";
        if (_bodyLoaded) return "[E] Pick up bag";
        return "[E] Put body in bag";
    }

    public bool CanInteract(PlayerInteraction player)
    {
        if (_pickedUp) return false;
        // Can load body into bag
        if (!_bodyLoaded && player.IsCarryingBody) return true;
        // Can pick up loaded bag (hands must be free)
        if (_bodyLoaded && !player.IsCarryingItem) return true;
        return false;
    }

    public void Interact(PlayerInteraction player)
    {
        if (!_bodyLoaded && player.IsCarryingBody)
        {
            // Drop the body, it disappears into the bag
            player.ConsumeCarriedItem();
            _bodyLoaded = true;

            // Swap to dirty bag visual
            if (cleanBagVisual != null) cleanBagVisual.SetActive(false);
            if (dirtyBagVisual != null) dirtyBagVisual.SetActive(true);

            Debug.Log("[GarbageBag] Body stuffed in bag. Now pick up the bag.");
        }
        else if (_bodyLoaded && !player.IsCarryingItem)
        {
            // Pick up the whole bag
            _pickedUp = true;
            player.PickUpItem(gameObject, isBody: true);  // isBody:true so locker accepts it
            Debug.Log("[GarbageBag] Bag picked up. Bring to locker.");
        }
    }

    // If player drops the bag, they can pick it up again
    public void OnDropped() => _pickedUp = false;
}