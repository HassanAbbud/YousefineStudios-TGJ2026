using TMPro;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

namespace Lobby
{
    public class LobbySlotUI : MonoBehaviour
    {
        [Header("Display")]
        public TMP_Text titleLabel;
        public TMP_Text occupantLabel;
        public Button selectButton;
        public GameObject takenIndicator;
        public GameObject mineIndicator;

        PlayerRole assignedRole;

        public void Setup(PlayerRole role, string title)
        {
            assignedRole = role;
            if (titleLabel != null) titleLabel.text = title;
            selectButton.onClick.AddListener(OnSelect);
        }

        public void Refresh(Unity.Services.Lobbies.Models.Lobby lobby, string myId)
        {
            string occupantName = "(empty)";
            bool taken = false;
            bool mine = false;

            foreach (var p in lobby.Players)
            {
                var role = LobbyManager.Instance.GetRoleOf(p.Id);
                if (role != assignedRole) continue;

                taken = true;
                mine = (p.Id == myId);
                if (p.Data != null && p.Data.TryGetValue(LobbyManager.KEY_PLAYER_NAME, out var nd))
                    occupantName = nd.Value;
                break;
            }

            if (occupantLabel != null) occupantLabel.text = taken ? occupantName : "(empty)";
            if (takenIndicator != null) takenIndicator.SetActive(taken && !mine);
            if (mineIndicator != null) mineIndicator.SetActive(mine);

            // Disable the button if someone else has this role
            selectButton.interactable = !taken || mine;
        }

        async void OnSelect()
        {
            string myId = AuthenticationService.Instance.PlayerId;
            // Already mine? Do nothing.
            if (LobbyManager.Instance.GetRoleOf(myId) == assignedRole) return;
            // Don't allow stealing
            if (LobbyManager.Instance.IsRoleTakenByOther(assignedRole)) return;

            await LobbyManager.Instance.UpdateMyRoleAsync(assignedRole);
        }
    }
}