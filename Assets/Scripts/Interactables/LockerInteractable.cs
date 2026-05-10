using UnityEngine;
public class LockerInteractable : MonoBehaviour, IInteractable
{
    private bool _bodyStored;
    public string GetPromptText() => _bodyStored ? "Locker (full)" : "[E] Hide body in locker";

    // Only accepts the BAGGED body — raw body is rejected
    public bool CanInteract(PlayerInteraction player) => !_bodyStored && player.IsCarryingBag;

    public void Interact(PlayerInteraction player)
    {
        _bodyStored = true;
        player.ConsumeCarriedItem();
        ObjectiveManager.Instance.CompleteObjective(ObjectiveManager.ObjectiveType.HideBody);
        Debug.Log("[Locker] Body hidden — Objective 1 complete!");
    }
}