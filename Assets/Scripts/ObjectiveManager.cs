using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Singleton that tracks the 3 win-condition objectives.
/// Other scripts call ObjectiveManager.Instance.CompleteObjective(ObjectiveType)
/// </summary>
public class ObjectiveManager : MonoBehaviour
{
    public static ObjectiveManager Instance { get; private set; }

    public enum ObjectiveType
    {
        HideBody,       // Objective 1: Put body in coworker's locker
        CleanScene,     // Objective 2: Clean blood from floor + murder weapon
        HideWeapon      // Objective 3: Hide the murder weapon
    }

    [Header("Objective State (read-only in Inspector)")]
    [SerializeField] private bool bodyHidden;
    [SerializeField] private bool sceneClean;
    [SerializeField] private bool weaponHidden;

    [Header("Events — hook up UI, sfx, etc.")]
    public UnityEvent<ObjectiveType> OnObjectiveCompleted;
    public UnityEvent OnAllObjectivesComplete;     // Triggers win condition

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    public void CompleteObjective(ObjectiveType type)
    {
        switch (type)
        {
            case ObjectiveType.HideBody:
                if (bodyHidden) return;
                bodyHidden = true;
                break;

            case ObjectiveType.CleanScene:
                if (sceneClean) return;
                sceneClean = true;
                break;

            case ObjectiveType.HideWeapon:
                if (weaponHidden) return;
                weaponHidden = true;
                break;
        }

        Debug.Log($"[ObjectiveManager] Completed: {type}");
        OnObjectiveCompleted?.Invoke(type);

        if (AllComplete())
        {
            Debug.Log("[ObjectiveManager] ALL OBJECTIVES COMPLETE — YOU WIN!");
            OnAllObjectivesComplete?.Invoke();
        }
    }

    public bool IsComplete(ObjectiveType type) => type switch
    {
        ObjectiveType.HideBody    => bodyHidden,
        ObjectiveType.CleanScene  => sceneClean,
        ObjectiveType.HideWeapon  => weaponHidden,
        _                         => false
    };

    public bool AllComplete() => bodyHidden && sceneClean && weaponHidden;
}
