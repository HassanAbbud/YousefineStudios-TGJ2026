//using UnityEngine;

//// ── BODY ─────────────────────────────────────────────────────────────────────
//public class BodyInteractable : MonoBehaviour, IInteractable, IDropNotify
//{
//    private bool _pickedUp;
//    private SuspiciousObject _suspicious;

//    private void Awake() => _suspicious = GetComponent<SuspiciousObject>();

//    public string GetPromptText() => "[E] Pick up body";

//    public bool CanInteract(PlayerInteraction player)
//        => !_pickedUp && !player.IsCarryingItem;

//    public void Interact(PlayerInteraction player)
//    {
//        _pickedUp = true;
//        _suspicious?.SetVisible(false);
//        player.PickUpItem(gameObject, isBody: true);
//        Debug.Log("[Body] Picked up. Carry it to the locker.");
//    }

//    public void OnDropped()
//    {
//        _pickedUp = false;
//        _suspicious?.SetVisible(true);
//    }
//}


//// ── LOCKER ────────────────────────────────────────────────────────────────────
//public class LockerInteractable : MonoBehaviour, IInteractable
//{
//    private bool _bodyStored;

//    public string GetPromptText() => _bodyStored ? "Locker (full)" : "[E] Hide body in locker";

//    public bool CanInteract(PlayerInteraction player)
//        => !_bodyStored && player.IsCarryingBody;

//    public void Interact(PlayerInteraction player)
//    {
//        _bodyStored = true;
//        player.ConsumeCarriedItem();
//        ObjectiveManager.Instance.CompleteObjective(ObjectiveManager.ObjectiveType.HideBody);
//        Debug.Log("[Locker] Body hidden — Objective 1 complete!");
//    }
//}


//// ── CLEANING SUPPLY ───────────────────────────────────────────────────────────
//// FIX: sets HasCleaningKit on the player instead of requiring a physical prefab.
//// Player can then mop without needing to hold a visual object.
//// If you DO have a kit prefab, assign it — it'll still be picked up visually.
//public class CleaningSupplyInteractable : MonoBehaviour, IInteractable
//{
//    [SerializeField] private GameObject cleaningKitPrefab; // optional visual
//    private bool _taken;

//    public string GetPromptText() => _taken ? "" : "[E] Take cleaning supplies";

//    public bool CanInteract(PlayerInteraction player)
//        => !_taken && !player.IsCarryingItem;

//    public void Interact(PlayerInteraction player)
//    {
//        _taken = true;
//        gameObject.SetActive(false);

//        // Give the player the cleaning kit flag so they can mop
//        player.HasCleaningKit = true;

//        // If a visual prefab is assigned, also carry it
//        if (cleaningKitPrefab != null)
//        {
//            GameObject kit = Instantiate(cleaningKitPrefab);
//            player.PickUpItem(kit);
//        }

//        Debug.Log("[CleaningSupply] Cleaning kit picked up.");
//    }
//}


//// ── BLOOD STAIN ───────────────────────────────────────────────────────────────
//// FIX 1: Only ONE stain — no static counter needed. Directly fires CleanScene.
//// FIX 2: Checks HasCleaningKit instead of IsCarryingItem so the player doesn't
////         need to be actively holding the mop (just needs to have picked it up).
//public class BloodStainInteractable : MonoBehaviour, IInteractable
//{
//    private bool _cleaned;
//    private SuspiciousObject _suspicious;

//    private void Awake() => _suspicious = GetComponent<SuspiciousObject>();

//    public string GetPromptText() => _cleaned ? "" : "[E] Clean blood";

//    public bool CanInteract(PlayerInteraction player)
//        => !_cleaned && player.HasCleaningKit && !player.IsCarryingBody;

//    public void Interact(PlayerInteraction player)
//    {
//        _cleaned = true;
//        _suspicious?.SetVisible(false);
//        gameObject.SetActive(false);

//        // ← THIS WAS MISSING — objective 2 now actually completes
//        ObjectiveManager.Instance.CompleteObjective(ObjectiveManager.ObjectiveType.CleanScene);
//        Debug.Log("[BloodStain] Cleaned — Objective 2 complete!");
//    }
//}


//// ── WEAPON ────────────────────────────────────────────────────────────────────
//public class WeaponInteractable : MonoBehaviour, IInteractable, IDropNotify
//{
//    private bool _pickedUp;
//    private SuspiciousObject _suspicious;

//    private void Awake() => _suspicious = GetComponent<SuspiciousObject>();

//    public string GetPromptText() => "[E] Pick up weapon";

//    public bool CanInteract(PlayerInteraction player)
//        => !_pickedUp && !player.IsCarryingItem;

//    public void Interact(PlayerInteraction player)
//    {
//        _pickedUp = true;
//        _suspicious?.SetVisible(false);
//        player.PickUpItem(gameObject);
//        Debug.Log("[Weapon] Picked up. Hide it in a drawer.");
//    }

//    public void OnDropped()
//    {
//        _pickedUp = false;
//        _suspicious?.SetVisible(true);
//    }
//}


//// ── DRAWER ────────────────────────────────────────────────────────────────────
//public class DrawerInteractable : MonoBehaviour, IInteractable
//{
//    private bool _weaponStored;

//    public string GetPromptText() => _weaponStored ? "Drawer (full)" : "[E] Hide weapon in drawer";

//    public bool CanInteract(PlayerInteraction player)
//        => !_weaponStored && player.IsCarryingItem && !player.IsCarryingBody;

//    public void Interact(PlayerInteraction player)
//    {
//        _weaponStored = true;
//        player.ConsumeCarriedItem();
//        ObjectiveManager.Instance.CompleteObjective(ObjectiveManager.ObjectiveType.HideWeapon);
//        Debug.Log("[Drawer] Weapon hidden — Objective 3 complete!");
//    }
//}