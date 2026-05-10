using UnityEngine;
public class CleaningSupplyInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject cleaningKitPrefab;
    private bool _taken;
    public string GetPromptText() => _taken ? "" : "[E] Take cleaning supplies";
    public bool CanInteract(PlayerInteraction player) => !_taken && !player.IsCarryingItem;
    public void Interact(PlayerInteraction player)
    {
        _taken = true;
        gameObject.SetActive(false);
        player.HasCleaningKit = true;
        if (cleaningKitPrefab != null)
        {
            GameObject kit = Instantiate(cleaningKitPrefab);
            player.PickUpItem(kit);
        }
        Debug.Log("[CleaningSupply] Cleaning kit picked up.");
    }
}
