using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Networked
{
    [RequireComponent(typeof(Button))]
    public class DebugHostStarter : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                NetworkManager.Singleton.StartHost();
                gameObject.SetActive(false);
            });
        }
    }
}