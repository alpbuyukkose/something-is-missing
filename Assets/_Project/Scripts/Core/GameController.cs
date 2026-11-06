using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { Playing, Serving, DishTransition, TimeUp, Final }

public class GameController : MonoBehaviour
{
    [SerializeField] GameConfig config;

    public event Action<int, int> DishStarted;
    public event Action<IngredientData, int> IngredientSelected;
    public event Action<IngredientData, int> IngredientRemoved;
    public event Action<ServeResult> Served;
    public event Action<ServeResult> DishWrong;
    public event Action<ServeResult> DishCorrect;
    public event Action<IReadOnlyList<IngredientData>> TimeUp;
    public event Action Final;
    public event Action Restarted;

    public GameState State { get; private set; }
    public int DishIndex { get; private set; }
    public float TimeRemaining { get; private set; }
    public float TimeNormalized => TimeRemaining / config.dishDuration;
    public IReadOnlyList<IngredientData> CurrentRecipe => recipe;
    public IReadOnlyList<IngredientData> Plate => plate;

    List<IngredientData> recipe = new();
    IngredientData[] plate = Array.Empty<IngredientData>();

    void Start() => StartDish(0);

    void Update()
    {
        if (State != GameState.Playing) return;
        TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
        if (TimeRemaining > 0f) return;
        State = GameState.TimeUp;
        TimeUp?.Invoke(recipe);
    }

    public bool TrySelect(IngredientData ingredient)
    {
        int slot = Array.IndexOf(plate, null);
        if (State != GameState.Playing || ingredient == null || slot < 0 || Array.IndexOf(plate, ingredient) >= 0) return false;
        plate[slot] = ingredient;
        IngredientSelected?.Invoke(ingredient, slot);
        if (Array.IndexOf(plate, null) < 0) StartCoroutine(Serve());
        return true;
    }

    public bool TryRemove(int slot)
    {
        if (State != GameState.Playing || slot < 0 || slot >= plate.Length || plate[slot] == null) return false;
        var ingredient = plate[slot];
        plate[slot] = null;
        IngredientRemoved?.Invoke(ingredient, slot);
        return true;
    }

    public bool TryRestart()
    {
        if (State != GameState.TimeUp && State != GameState.Final) return false;
        StopAllCoroutines();
        Restarted?.Invoke();
        StartDish(0);
        return true;
    }

    void StartDish(int index)
    {
        DishIndex = index;
        recipe = Recipe.Generate(config.ingredients, config.recipeLengths[index]);
        plate = new IngredientData[recipe.Count];
        TimeRemaining = config.dishDuration;
        State = GameState.Playing;
        DishStarted?.Invoke(index, recipe.Count);
    }

    IEnumerator Serve()
    {
        State = GameState.Serving;
        var result = Recipe.Evaluate(recipe, plate);
        Served?.Invoke(result);
        yield return new WaitForSeconds(config.resolveDelay);

        Array.Clear(plate, 0, plate.Length);
        if (!result.IsCorrect)
        {
            State = GameState.Playing;
            DishWrong?.Invoke(result);
            yield break;
        }

        State = GameState.DishTransition;
        DishCorrect?.Invoke(result);
        yield return new WaitForSeconds(config.nextDishDelay);

        if (DishIndex + 1 < config.recipeLengths.Count)
        {
            StartDish(DishIndex + 1);
        }
        else
        {
            State = GameState.Final;
            Final?.Invoke();
        }
    }
}
