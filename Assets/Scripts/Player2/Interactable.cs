using UnityEngine;

namespace Shared
{
    public interface IPlayer2Interactable
    {
        void OnPlayer2Click();
        Transform Transform { get; }
    }
}