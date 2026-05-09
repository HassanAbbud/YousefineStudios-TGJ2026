/// <summary>
/// Any object in the world that Player 2 can interact with must implement this.
/// Examples: Body, CleaningSupply, Locker, Drawer, Door
/// </summary>
public interface IInteractable
{
    /// <summary>Text shown in the HUD prompt, e.g. "[E] Pick up body"</summary>
    string GetPromptText();

    /// <summary>Called when Player 2 presses the interact key while looking at this object.</summary>
    void Interact(PlayerInteraction player);

    /// <summary>Can the player interact with this right now? (e.g. locker is full, hands are full)</summary>
    bool CanInteract(PlayerInteraction player);
}
