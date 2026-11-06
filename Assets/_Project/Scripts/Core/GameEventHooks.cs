using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Inspector-bindable mirrors of the GameController events (Animator triggers, audio, VFX).
public class GameEventHooks : MonoBehaviour
{
    [SerializeField] GameController game;
    public UnityEvent onDishStarted = new();
    public UnityEvent onIngredientSelected = new();
    public UnityEvent onIngredientRemoved = new();
    public UnityEvent onServed = new();
    public UnityEvent onDishWrong = new();
    public UnityEvent onDishCorrect = new();
    public UnityEvent onTimeUp = new();
    public UnityEvent onFinal = new();
    public UnityEvent onRestarted = new();

    void OnEnable()
    {
        game.DishStarted += DishStarted;
        game.IngredientSelected += IngredientSelected;
        game.IngredientRemoved += IngredientRemoved;
        game.Served += Served;
        game.DishWrong += DishWrong;
        game.DishCorrect += DishCorrect;
        game.TimeUp += TimeUp;
        game.Final += Final;
        game.Restarted += Restarted;
    }

    void OnDisable()
    {
        game.DishStarted -= DishStarted;
        game.IngredientSelected -= IngredientSelected;
        game.IngredientRemoved -= IngredientRemoved;
        game.Served -= Served;
        game.DishWrong -= DishWrong;
        game.DishCorrect -= DishCorrect;
        game.TimeUp -= TimeUp;
        game.Final -= Final;
        game.Restarted -= Restarted;
    }

    void DishStarted(int index, int length) => onDishStarted.Invoke();
    void IngredientSelected(IngredientData ingredient, int slot) => onIngredientSelected.Invoke();
    void IngredientRemoved(IngredientData ingredient, int slot) => onIngredientRemoved.Invoke();
    void Served(ServeResult result) => onServed.Invoke();
    void DishWrong(ServeResult result) => onDishWrong.Invoke();
    void DishCorrect(ServeResult result) => onDishCorrect.Invoke();
    void TimeUp(IReadOnlyList<IngredientData> recipe) => onTimeUp.Invoke();
    void Final() => onFinal.Invoke();
    void Restarted() => onRestarted.Invoke();
}
