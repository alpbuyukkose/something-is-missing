using System.Collections.Generic;
using UnityEngine;

public class DialogView : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] DialogTypewriter typewriter;
    [Tooltip("Write *word* to emphasize a word.")]
    [SerializeField] string dishStartLine = "Feed me.";
    [SerializeField] string wrongLine = "Something is *missing*!";
    [SerializeField] string correctLine = "Delicious... but I'm still hungry.";
    [SerializeField] string timeUpLine = "Too slow. You'll do instead.";
    [SerializeField] string finalLine = "Perfect... but something is still missing - *YOU*!";

    void OnEnable()
    {
        game.DishStarted += OnDishStarted;
        game.Served += OnServed;
        game.DishWrong += OnWrong;
        game.DishCorrect += OnCorrect;
        game.TimeUp += OnTimeUp;
        game.Final += OnFinal;
    }

    void OnDisable()
    {
        game.DishStarted -= OnDishStarted;
        game.Served -= OnServed;
        game.DishWrong -= OnWrong;
        game.DishCorrect -= OnCorrect;
        game.TimeUp -= OnTimeUp;
        game.Final -= OnFinal;
    }

    void OnDishStarted(int index, int length) => typewriter.Play(dishStartLine);
    void OnServed(ServeResult result) => typewriter.Clear();
    void OnWrong(ServeResult result) => typewriter.Play(wrongLine);
    void OnCorrect(ServeResult result) => typewriter.Play(correctLine);
    void OnTimeUp(IReadOnlyList<IngredientData> recipe) => typewriter.Play(timeUpLine);
    void OnFinal() => typewriter.Play(finalLine);
}
