using System.Collections.Generic;
using UnityEngine;

public class FeedbackView : MonoBehaviour
{
    [SerializeField] GameController game;
    [Tooltip("Colored base under each mini ingredient, pivot on its top surface.")]
    [SerializeField] GameObject tilePrefab;
    [SerializeField] GameObject greyboxPrefab;
    [SerializeField] float spacing = 0.3f;
    [SerializeField] float miniScale = 0.8f;
    [SerializeField] Color correctColor = new(0.25f, 0.68f, 0.29f);
    [SerializeField] Color presentColor = new(0.95f, 0.77f, 0.06f);
    [SerializeField] Color absentColor = new(0.3f, 0.3f, 0.3f);

    readonly List<GameObject> tiles = new();

    void OnEnable()
    {
        game.DishStarted += OnDishStarted;
        game.DishWrong += Show;
        game.DishCorrect += Show;
        game.Restarted += Clear;
    }

    void OnDisable()
    {
        game.DishStarted -= OnDishStarted;
        game.DishWrong -= Show;
        game.DishCorrect -= Show;
        game.Restarted -= Clear;
    }

    void OnDishStarted(int index, int length) => Clear();

    void Show(ServeResult result)
    {
        Clear();
        int count = result.Guess.Length;
        for (int i = 0; i < count; i++)
        {
            var tile = Instantiate(tilePrefab, transform, false);
            tile.transform.localPosition = new Vector3((i - (count - 1) * 0.5f) * spacing, 0f, 0f);
            IngredientVisual.Tint(tile, ColorOf(result.Feedback[i]));

            var mini = IngredientVisual.Create(result.Guess[i], greyboxPrefab, tile.transform);
            mini.transform.localScale *= miniScale;
            foreach (var c in mini.GetComponentsInChildren<Collider>()) Destroy(c);
            tiles.Add(tile);
        }
    }

    Color ColorOf(FeedbackType type) => type switch
    {
        FeedbackType.Correct => correctColor,
        FeedbackType.Present => presentColor,
        _ => absentColor,
    };

    void Clear()
    {
        foreach (var tile in tiles) Destroy(tile);
        tiles.Clear();
    }
}
