using UnityEngine;
public class DrawerInteractable : MonoBehaviour, IInteractable
{
    private bool _weaponStored;
    public string GetPromptText() => _weaponStored ? "Drawer (full)" : "[E] Hide weapon in drawer";
    public bool CanInteract(PlayerInteraction player) => !_weaponStored && player.IsCarryingItem && !player.IsCarryingBody;
    public void Interact(PlayerInteraction player)
    {
        _weaponStored = true;
        player.ConsumeCarriedItem();
        ObjectiveManager.Instance.CompleteObjective(ObjectiveManager.ObjectiveType.HideWeapon);
        Debug.Log("[Drawer] Weapon hidden — Objective 3 complete!");
    }
}
