// ============================================================
//  NETWORKING STUBS — For your Netcode teammate
//  These mark exactly WHERE to add NetworkBehaviour calls.
//
//  If using Unity Netcode for GameObjects (NGO):
//    - PlayerMovement, PlayerInteraction → inherit NetworkBehaviour
//    - Add [ClientRpc] / [ServerRpc] where marked
//  If using Photon (PUN2):
//    - Inherit MonoBehaviourPun + IPunObservable
//    - Serialize marked values in OnPhotonSerializeView
// ============================================================

/*

── PlayerMovement ──────────────────────────────────────────
□  IsMoving           → sync to P1 camera view (NPC noise calc)
□  IsCrouching        → sync to P1 (shows player icon on camera feed)
□  transform.position → sync (already handled by NetworkTransform)

── PlayerInteraction ────────────────────────────────────────
□  PickUpItem(item)   → [ServerRpc] so server owns item parenting
□  DropItem()         → [ServerRpc]
□  ConsumeCarriedItem → [ServerRpc] + destroy over network
□  IsCarryingBody     → sync to P1 (camera overlay icon)

── ObjectiveManager ─────────────────────────────────────────
□  CompleteObjective() → [ServerRpc] → [ClientRpc] broadcast to BOTH players
                         so P1's HUD also updates
□  OnAllObjectivesComplete → triggers win screen on BOTH clients

── Interactables ────────────────────────────────────────────
□  All "interacted" booleans (_pickedUp, _cleaned, etc.)
   should live on the server. Use NetworkVariable<bool> or
   call a [ServerRpc] to flip them so late-joining doesn't desync.

── Suggested component setup (NGO) ─────────────────────────
  Player2 GameObject:
    ├─ NetworkObject          ← required
    ├─ NetworkTransform       ← handles position sync
    ├─ PlayerMovement         ← inherit NetworkBehaviour, IsOwner checks
    ├─ PlayerInteraction      ← inherit NetworkBehaviour
    └─ Camera (child)         ← only active for owner

  Each Interactable GameObject:
    ├─ NetworkObject
    └─ [Script]Interactable   ← inherit NetworkBehaviour

*/
