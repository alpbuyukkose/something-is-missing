using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlateView : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] Pantry pantry;
    [Tooltip("Slots are laid out left to right along its local X, on the plate surface.")]
    [SerializeField] Transform slotsRoot;
    [SerializeField] float slotSpacing = 0.19f;
    [Tooltip("Optional marker spawned at each slot.")]
    [SerializeField] GameObject slotMarkerPrefab;
    [SerializeField] float moveDuration = 0.35f;
    [SerializeField] float arcHeight = 0.2f;
    [Tooltip("Seconds after serving until the food disappears (the bite).")]
    [SerializeField] float eatDelay = 0.65f;
    [SerializeField] float popDuration = 0.2f;

    readonly List<Transform> slots = new();

    void OnEnable()
    {
        game.DishStarted += OnDishStarted;
        game.IngredientSelected += OnSelected;
        game.IngredientRemoved += OnRemoved;
        game.Served += OnServed;
        game.DishWrong += OnResolved;
        game.DishCorrect += OnResolved;
        game.Restarted += OnRestarted;
    }

    void OnDisable()
    {
        game.DishStarted -= OnDishStarted;
        game.IngredientSelected -= OnSelected;
        game.IngredientRemoved -= OnRemoved;
        game.Served -= OnServed;
        game.DishWrong -= OnResolved;
        game.DishCorrect -= OnResolved;
        game.Restarted -= OnRestarted;
    }

    void OnDishStarted(int index, int length)
    {
        foreach (var slot in slots) Destroy(slot.gameObject);
        slots.Clear();
        for (int i = 0; i < length; i++)
        {
            var slot = slotMarkerPrefab ? Instantiate(slotMarkerPrefab).transform : new GameObject("Slot").transform;
            slot.SetParent(slotsRoot, false);
            slot.SetLocalPositionAndRotation(new Vector3((i - (length - 1) * 0.5f) * slotSpacing, 0f, 0f), Quaternion.identity);
            slots.Add(slot);
        }
    }

    void OnSelected(IngredientData data, int slot) =>
        pantry.Get(data).MoveToPlate(slot, slots[slot].position, slots[slot].rotation, moveDuration, arcHeight);

    void OnRemoved(IngredientData data, int slot) => pantry.Get(data).ReturnHome(moveDuration, arcHeight);

    void OnServed(ServeResult result) => StartCoroutine(Eat(result));

    IEnumerator Eat(ServeResult result)
    {
        yield return new WaitForSeconds(eatDelay);
        foreach (var data in result.Guess) pantry.Get(data).Consume(popDuration);
    }

    void OnResolved(ServeResult result)
    {
        StopAllCoroutines();
        foreach (var data in result.Guess) pantry.Get(data).Respawn(popDuration);
    }

    void OnRestarted()
    {
        StopAllCoroutines();
        foreach (var view in pantry.All) view.ResetHome();
    }
}
