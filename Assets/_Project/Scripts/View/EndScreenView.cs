using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class EndScreenView : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] CanvasGroup group;
    [SerializeField] TMP_Text message;
    [SerializeField] TMP_Text hint;
    [Tooltip("Wait for the dragon to reach the camera before going dark.")]
    [SerializeField] float showDelay = 2.1f;
    [SerializeField] float fadeDuration = 0.4f;
    [SerializeField] string recipeTitle = "The recipe was:";
    [SerializeField] string finalMessage = "You were the missing ingredient.";
    [SerializeField] string restartHint = "Press R to restart";

    void Awake() => Hide();

    void OnEnable()
    {
        game.TimeUp += OnTimeUp;
        game.Final += OnFinal;
        game.Restarted += Hide;
    }

    void OnDisable()
    {
        game.TimeUp -= OnTimeUp;
        game.Final -= OnFinal;
        game.Restarted -= Hide;
    }

    void OnTimeUp(IReadOnlyList<IngredientData> recipe)
    {
        var text = new StringBuilder(recipeTitle);
        for (int i = 0; i < recipe.Count; i++)
            text.Append($"\n{i + 1}. <color=#{ColorUtility.ToHtmlStringRGB(recipe[i].greyboxColor)}>{recipe[i].displayName}</color>");
        Show(text.ToString());
    }

    void OnFinal() => Show(finalMessage);

    void Show(string text)
    {
        StopAllCoroutines();
        StartCoroutine(FadeIn(text));
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
        group.alpha = 0f;
    }
}
