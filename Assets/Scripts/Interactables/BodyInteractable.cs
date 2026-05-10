using UnityEngine;
public class BodyInteractable : MonoBehaviour, IInteractable, IDropNotify
{
    private bool _pickedUp;
    private SuspiciousObject _suspicious;
    private void Awake() => _suspicious = GetComponent<SuspiciousObject>();
    public string GetPromptText() => "[E] Pick up body";
    public bool CanInteract(PlayerInteraction player) => !_pickedUp && !player.IsCarryingItem;
    public void Interact(PlayerInteraction player)
    {
        _pickedUp = true;
        _suspicious?.SetVisible(false);
        player.PickUpItem(gameObject, isBody: true);
        Debug.Log("[Body] Picked up. Carry it to the locker.");
    }
    public void OnDropped() { _pickedUp = false; _suspicious?.SetVisible(true); }
}
