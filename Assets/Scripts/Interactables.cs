using UnityEngine;

// ============================================================
//  BODY  —  Objective 1 (step 1 of 2: pick it up)
//  Requires: Player is NOT already carrying something
//  After pickup: Player carries body to the Locker
// ============================================================
public class BodyInteractable : MonoBehaviour, IInteractable, IDropNotify
{
    private bool _pickedUp;
    private SuspiciousObject _suspicious;

    private void Awake() => _suspicious = GetComponent<SuspiciousObject>();

    public string GetPromptText() => "[E] Pick up body";

    public bool CanInteract(PlayerInteraction player)
        => !_pickedUp && !player.IsCarryingItem;

    public void Interact(PlayerInteraction player)
    {
        _pickedUp = true;
        _suspicious?.SetVisible(false);   // NPCs no longer see the body while it's carried
        player.PickUpItem(gameObject, isBody: true);
        Debug.Log("[Body] Picked up. Carry it to the locker.");
    }

    public void OnDropped()
    {
        _pickedUp = false;
        _suspicious?.SetVisible(true);    // Dropped on floor — NPCs can spot it again
    }
}


// ============================================================
//  LOCKER  —  Objective 1 (step 2 of 2: hide body)
//  Requires: Player is carrying the body
// ============================================================
public class LockerInteractable : MonoBehaviour, IInteractable
{
    private bool _bodyStored;

    public string GetPromptText() => _bodyStored
        ? "Locker (full)"
        : "[E] Hide body in locker";

    public bool CanInteract(PlayerInteraction player)
        => !_bodyStored && player.IsCarryingBody;

    public void Interact(PlayerInteraction player)
    {
        _bodyStored = true;
        player.ConsumeCarriedItem();    // Body disappears into locker
        ObjectiveManager.Instance.CompleteObjective(ObjectiveManager.ObjectiveType.HideBody);
        Debug.Log("[Locker] Body hidden. Objective 1 complete!");
    }
}


// ============================================================
//  CLEANING SUPPLY  —  gives player the cleaning kit
//  Required before player can clean blood stains
// ============================================================
public class CleaningSupplyInteractable : MonoBehaviour, IInteractable
{
    [SerializeField] private GameObject cleaningKitPrefab; // Visual held object (mop/bucket/bag)
    private bool _taken;

    public string GetPromptText() => _taken ? "" : "[E] Take cleaning supplies";

    public bool CanInteract(PlayerInteraction player)
        => !_taken && !player.IsCarryingItem;

    public void Interact(PlayerInteraction player)
    {
        _taken = true;
        gameObject.SetActive(false);

        if (cleaningKitPrefab != null)
        {
            GameObject kit = Instantiate(cleaningKitPrefab);
            player.PickUpItem(kit);
        }

        Debug.Log("[CleaningSupply] Cleaning kit picked up.");
    }
}


// ============================================================
//  BLOOD STAIN  —  Objective 2 (part of clean scene)
//  Requires: Player is carrying cleaning supplies
// ============================================================
public class BloodStainInteractable : MonoBehaviour, IInteractable
{
    private static int _totalStains;
    private static int _cleanedStains;
    private bool _cleaned;
    private SuspiciousObject _suspicious;

    private void Awake()
    {
        _totalStains++;
        _suspicious = GetComponent<SuspiciousObject>();
    }

    public string GetPromptText() => "[E] Clean blood";

    public bool CanInteract(PlayerInteraction player)
        => !_cleaned && player.IsCarryingItem && !player.IsCarryingBody;

    public void Interact(PlayerInteraction player)
    {
        _cleaned = true;
        _suspicious?.SetVisible(false);   // Cleaned — no longer suspicious
        gameObject.SetActive(false);
        _cleanedStains++;

        Debug.Log($"[BloodStain] Cleaned {_cleanedStains}/{_totalStains}");

        if (_cleanedStains >= _totalStains)
            Debug.Log("[BloodStain] All stains cleaned.");
    }
}


// ============================================================
//  WEAPON  —  Objective 3: hide the murder weapon
//  Step 1: pick it up. Step 2: put in drawer (DrawerInteractable)
// ============================================================
public class WeaponInteractable : MonoBehaviour, IInteractable, IDropNotify
{
    private bool _pickedUp;
    private SuspiciousObject _suspicious;

    private void Awake() => _suspicious = GetComponent<SuspiciousObject>();

    public string GetPromptText() => "[E] Pick up weapon";

    public bool CanInteract(PlayerInteraction player)
        => !_pickedUp && !player.IsCarryingItem;

    public void Interact(PlayerInteraction player)
    {
        _pickedUp = true;
        _suspicious?.SetVisible(false);   // Carried — NPCs won't see it
        player.PickUpItem(gameObject);
        Debug.Log("[Weapon] Picked up. Hide it in a drawer.");
    }

    public void OnDropped()
    {
        _pickedUp = false;
        _suspicious?.SetVisible(true);    // Dropped — NPCs can spot it again
    }
}


// ============================================================
//  DRAWER  —  Objective 3 (step 2): hide the weapon
//  Requires: Player is carrying the weapon (not the body)
// ============================================================
public class DrawerInteractable : MonoBehaviour, IInteractable
{
    private bool _weaponStored;

    public string GetPromptText() => _weaponStored
        ? "Drawer (full)"
        : "[E] Hide weapon in drawer";

    public bool CanInteract(PlayerInteraction player)
        => !_weaponStored && player.IsCarryingItem && !player.IsCarryingBody;

    public void Interact(PlayerInteraction player)
    {
        _weaponStored = true;
        player.ConsumeCarriedItem();
        ObjectiveManager.Instance.CompleteObjective(ObjectiveManager.ObjectiveType.HideWeapon);
        Debug.Log("[Drawer] Weapon hidden. Objective 3 complete!");
    }
}