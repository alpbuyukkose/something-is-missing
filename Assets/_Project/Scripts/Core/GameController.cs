using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum GameState { NotStarted, Playing, Serving, DishTransition, TimeUp, Final, ForkEaten }

public class GameController : MonoBehaviour
{
    [SerializeField] GameConfig config;
    [SerializeField] RunTimer timer;
    [Tooltip("Optional. After a correct dish, the next one waits until this line is fully shown.")]
    [SerializeField] DialogTypewriter dialog;

    public event Action<int, int> DishStarted;
    public event Action<IngredientData, int> IngredientSelected;
    public event Action<IngredientData, int> IngredientRemoved;
    public event Action<ServeResult> Served;
    public event Action<ServeResult> DishWrong;
    public event Action<ServeResult> DishCorrect;
    public event Action<IReadOnlyList<IngredientData>> TimeUp;
    public event Action Final;
    public event Action ForkEaten;
    public event Action Restarted;

    public GameState State { get; private set; }
    public int Difficulty { get; private set; }
    public IReadOnlyList<GameConfig.Difficulty> Difficulties => config.difficulties;
    public int DishIndex { get; private set; }
    public IReadOnlyList<IngredientData> CurrentRecipe => recipe;
    public IReadOnlyList<IngredientData> Plate => plate;
    public IngredientData Fork => config.fork;

    List<IngredientData> recipe = new();
    IngredientData[] plate = Array.Empty<IngredientData>();

    void OnEnable() => timer.Expired += OnTimeUp;
    void OnDisable() => timer.Expired -= OnTimeUp;

    // Starts a run at this difficulty, or restarts the current one. Called by the menu.
    public void StartRun(int difficulty)
    {
        if (State != GameState.NotStarted)
        {
            StopAllCoroutines();
            Restarted?.Invoke();
        }
        Difficulty = Mathf.Clamp(difficulty, 0, config.difficulties.Count - 1);
        timer.Restart(config.difficulties[Difficulty].runDuration);
        StartDish(0);
    }

    // Time keeps running while serving, so this can cut a serve short.
    void OnTimeUp()
    {
        StopAllCoroutines();
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

    // Same difficulty again, after an ending.
    public bool TryRestart()
    {
        if (State is not (GameState.TimeUp or GameState.Final or GameState.ForkEaten)) return false;
        StartRun(Difficulty);
        return true;
    }

    void StartDish(int index)
    {
        DishIndex = index;
        recipe = Recipe.Generate(config.ingredients, config.recipeLengths[index]);
        plate = new IngredientData[recipe.Count];
        State = GameState.Playing;
        DishStarted?.Invoke(index, recipe.Count);
    }

    IEnumerator Serve()
    {
        State = GameState.Serving;
        var result = Recipe.Evaluate(recipe, plate);
        Served?.Invoke(result);
        yield return new WaitForSeconds(config.resolveDelay);

        bool fork = config.fork != null && Array.IndexOf(plate, config.fork) >= 0;
        Array.Clear(plate, 0, plate.Length);
        if (fork)
        {
            timer.Stop();
            State = GameState.ForkEaten;
            ForkEaten?.Invoke();
            yield break;
        }
        if (!result.IsCorrect)
        {
            State = GameState.Playing;
            DishWrong?.Invoke(result);
            yield break;
        }

        State = GameState.DishTransition;
        DishCorrect?.Invoke(result);
        yield return null; // the line starts on the same event
        while (dialog != null && dialog.IsTyping) yield return null;
        yield return new WaitForSeconds(config.nextDishDelay);

        if (DishIndex + 1 < config.recipeLengths.Count)
        {
            StartDish(DishIndex + 1);
        }
        else
        {
            timer.Stop();
            State = GameState.Final;
            Final?.Invoke();
        }
    }
}
