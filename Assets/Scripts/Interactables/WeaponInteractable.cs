using UnityEngine;
public class WeaponInteractable : MonoBehaviour, IInteractable, IDropNotify
{
    private bool _pickedUp;
    private SuspiciousObject _suspicious;
    private void Awake() => _suspicious = GetComponent<SuspiciousObject>();
    public string GetPromptText() => "[E] Pick up weapon";
    public bool CanInteract(PlayerInteraction player) => !_pickedUp && !player.IsCarryingItem;
    public void Interact(PlayerInteraction player)
    {
        _pickedUp = true;
        _suspicious?.SetVisible(false);
        player.PickUpItem(gameObject);
        Debug.Log("[Weapon] Picked up. Hide it in a drawer.");
    }
    public void OnDropped() { _pickedUp = false; _suspicious?.SetVisible(true); }
}
