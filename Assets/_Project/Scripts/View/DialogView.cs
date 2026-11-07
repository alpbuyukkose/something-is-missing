using System.Collections.Generic;
using UnityEngine;

public class DialogView : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] DialogTypewriter typewriter;
    [Tooltip("Write *word* to emphasize a word, | to start a new sentence on a cleared line.")]
    [TextArea(1, 3)]
    [SerializeField] string dishStartLine = "Feed me.";
    [TextArea(1, 3)]
    [SerializeField] string wrongLine = "Something is *missing*!";
    [TextArea(1, 3)]
    [SerializeField] string correctLine = "Delicious... but I'm still hungry.";
    [TextArea(1, 3)]
    [SerializeField] string timeUpLine = "Too slow. You'll do instead.";
    [TextArea(1, 3)]
    [SerializeField] string finalLine = "Perfect... but something is still missing|*YOU*!";
    [TextArea(1, 3)]
    [SerializeField] string forkLine = "A *fork*?! Then I'll have the hand that holds it.";

    void OnEnable()
    {
        game.DishStarted += OnDishStarted;
        game.Served += OnServed;
        game.DishWrong += OnWrong;
        game.DishCorrect += OnCorrect;
        game.TimeUp += OnTimeUp;
        game.Final += OnFinal;
        game.ForkEaten += OnForkEaten;
    }

    void OnDisable()
    {
        game.DishStarted -= OnDishStarted;
        game.Served -= OnServed;
        game.DishWrong -= OnWrong;
        game.DishCorrect -= OnCorrect;
        game.TimeUp -= OnTimeUp;
        game.Final -= OnFinal;
        game.ForkEaten -= OnForkEaten;
    }

    void OnDishStarted(int index, int length) => typewriter.Play(dishStartLine);
    void OnServed(ServeResult result) => typewriter.Clear();
    void OnWrong(ServeResult result) => typewriter.Play(wrongLine);
    void OnCorrect(ServeResult result) => typewriter.Play(correctLine);
    void OnTimeUp(IReadOnlyList<IngredientData> recipe) => typewriter.Play(timeUpLine);
    void OnFinal() => typewriter.Play(finalLine);
    void OnForkEaten() => typewriter.Play(forkLine);
}
