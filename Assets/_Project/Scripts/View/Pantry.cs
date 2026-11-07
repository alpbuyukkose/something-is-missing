using System.Collections.Generic;
using UnityEngine;

// Creates a clickable view for every ingredient (and the fork) at the pose of its spot in the room.
public class Pantry : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] GameObject greyboxPrefab;
    [Tooltip("An ingredient whose model has a same-named child here rests at that child's pose (the imported room).")]
    [SerializeField] Transform modelSpots;
    [SerializeField] string interactableLayer = "Interactable";

    readonly List<IngredientView> views = new();
    readonly Dictionary<IngredientData, IngredientView> lookup = new();

    public IReadOnlyList<IngredientView> All => views;
    public IngredientView Fork { get; private set; }

    public IngredientView Get(IngredientData data) => lookup[data];

    void Awake()
    {
        int layer = Mathf.Max(0, LayerMask.NameToLayer(interactableLayer));
        var items = new List<IngredientData>(config.ingredients) { config.fork };
        foreach (var data in items)
        {
            if (data == null || lookup.ContainsKey(data)) continue;
            var view = new GameObject().AddComponent<IngredientView>();
            view.Init(data, greyboxPrefab, layer);
            view.transform.SetParent(transform, false);
            views.Add(view);
            lookup.Add(data, view);

            var spot = data.modelPrefab != null && modelSpots != null ? modelSpots.Find(data.modelPrefab.name) : null;
            if (spot != null) view.SetHome(spot.position, spot.rotation);
            else Debug.LogWarning($"{data.name} has no spot under {(modelSpots ? modelSpots.name : "Model Spots")}; it stays at the Pantry.", data);
        }
        if (config.fork != null) Fork = lookup[config.fork];
    }
}
