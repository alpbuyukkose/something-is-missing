using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

// The run's end text. The game event picks the text; Reveal (the dragon's player bite) fades it in.
public class EndScreenView : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text message;
    [SerializeField] TMP_Text hint;
    [Tooltip("Seconds between the bite and the fade.")]
    [SerializeField] float showDelay = 0.1f;
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] string recipeTitle = "The recipe was:";
    [SerializeField] string finalMessage = "You were the missing ingredient.";
    [SerializeField] string forkMessage = "You fed the dragon a fork.";
    [SerializeField] string restartHint = "Press R to restart";

    string pending;

    void Awake() => Hide();

    void OnEnable()
    {
        game.TimeUp += OnTimeUp;
        game.Final += OnFinal;
        game.ForkEaten += OnForkEaten;
        game.Restarted += Hide;
    }

    void OnDisable()
    {
        game.TimeUp -= OnTimeUp;
        game.Final -= OnFinal;
        game.ForkEaten -= OnForkEaten;
        game.Restarted -= Hide;
    }

    void OnTimeUp(IReadOnlyList<IngredientData> recipe)
    {
        var text = new StringBuilder(recipeTitle);
        for (int i = 0; i < recipe.Count; i++)
            text.Append($"\n{i + 1}. <color=#{ColorUtility.ToHtmlStringRGB(recipe[i].greyboxColor)}>{recipe[i].displayName}</color>");
        pending = text.ToString();
    }

    void OnFinal() => pending = finalMessage;
    void OnForkEaten() => pending = forkMessage;

    // Called when the dragon bites the player. Does nothing outside an ending (e.g. test keys).
    public void Reveal()
    {
        if (pending == null) return;
        StopAllCoroutines();
        StartCoroutine(FadeIn(pending));
        pending = null;
    }

    IEnumerator FadeIn(string text)
    {
        yield return new WaitForSeconds(showDelay);
        message.text = text;
        hint.text = restartHint;
        for (float t = 0f; t < 1f; t += Time.deltaTime / fadeDuration)
        {
            group.alpha = t;
            yield return null;
        }
        group.alpha = 1f;
    }

    void Hide()
    {
        StopAllCoroutines();
        pending = null;
        group.alpha = 0f;
    }
}
