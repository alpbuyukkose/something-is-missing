using System.Collections.Generic;
using UnityEngine;

public class Pantry : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] GameObject greyboxPrefab;
    [SerializeField] ShelfView leftShelf;
    [SerializeField] ShelfView rightShelf;
    [SerializeField] string interactableLayer = "Interactable";

    readonly List<IngredientView> views = new();
    readonly Dictionary<IngredientData, IngredientView> lookup = new();

    public IReadOnlyList<IngredientView> All => views;

    public IngredientView Get(IngredientData data) => lookup[data];

    void Awake()
    {
        int layer = Mathf.Max(0, LayerMask.NameToLayer(interactableLayer));
        foreach (var data in config.ingredients)
        {
            if (data == null || lookup.ContainsKey(data)) continue;
            var view = new GameObject().AddComponent<IngredientView>();
            view.Init(data, greyboxPrefab, layer);
            view.transform.SetParent(transform, false);
            views.Add(view);
            lookup.Add(data, view);
        }

        int half = (views.Count + 1) / 2;
        leftShelf.Layout(views.GetRange(0, half));
        rightShelf.Layout(views.GetRange(half, views.Count - half));
    }
}
