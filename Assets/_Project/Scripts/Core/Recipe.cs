using System.Collections.Generic;
using UnityEngine;

public enum FeedbackType { Correct, Present, Absent }

public class ServeResult
{
    public readonly IngredientData[] Guess;
    public readonly FeedbackType[] Feedback;
    public readonly bool IsCorrect;

    public ServeResult(IngredientData[] guess, FeedbackType[] feedback)
    {
        Guess = guess;
        Feedback = feedback;
        IsCorrect = System.Array.TrueForAll(feedback, f => f == FeedbackType.Correct);
    }
}

public static class Recipe
{
    public static List<IngredientData> Generate(IReadOnlyList<IngredientData> pool, int length)
    {
        var picks = new List<IngredientData>();
        foreach (var ingredient in pool)
            if (ingredient != null && !picks.Contains(ingredient)) picks.Add(ingredient);

        length = Mathf.Clamp(length, 0, picks.Count);
        for (int i = 0; i < length; i++)
        {
            int j = Random.Range(i, picks.Count);
            (picks[i], picks[j]) = (picks[j], picks[i]);
        }
        picks.RemoveRange(length, picks.Count - length);
        return picks;
    }

    // Wordle rules: exact matches first, then each unmatched recipe item can mark one misplaced guess.
    public static ServeResult Evaluate(IReadOnlyList<IngredientData> recipe, IReadOnlyList<IngredientData> guess)
    {
        var copy = new IngredientData[guess.Count];
        var feedback = new FeedbackType[guess.Count];
        var unmatched = new List<IngredientData>();
        for (int i = 0; i < guess.Count; i++)
        {
            copy[i] = guess[i];
            bool exact = i < recipe.Count && guess[i] == recipe[i];
            feedback[i] = exact ? FeedbackType.Correct : FeedbackType.Absent;
            if (!exact && i < recipe.Count) unmatched.Add(recipe[i]);
        }
        for (int i = 0; i < guess.Count; i++)
            if (feedback[i] != FeedbackType.Correct && unmatched.Remove(guess[i]))
                feedback[i] = FeedbackType.Present;
        return new ServeResult(copy, feedback);
    }
}
