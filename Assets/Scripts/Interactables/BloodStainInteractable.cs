using UnityEngine;
public class BloodStainInteractable : MonoBehaviour, IInteractable
{
    private bool _cleaned;
    private SuspiciousObject _suspicious;
    private void Awake() => _suspicious = GetComponent<SuspiciousObject>();
    public string GetPromptText() => _cleaned ? "" : "[E] Clean blood";
    public bool CanInteract(PlayerInteraction player) => !_cleaned && player.HasCleaningKit && !player.IsCarryingBody;
    public void Interact(PlayerInteraction player)
    {
        _cleaned = true;
        _suspicious?.SetVisible(false);
        gameObject.SetActive(false);
        ObjectiveManager.Instance.CompleteObjective(ObjectiveManager.ObjectiveType.CleanScene);
        Debug.Log("[BloodStain] Cleaned — Objective 2 complete!");
    }
}
