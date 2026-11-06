using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Greybox-only motion. Remove it when a rigged model + Animator (wired via GameEventHooks) takes over.
public class DragonGreyboxAnimator : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] Transform head;
    [Tooltip("Cylinder stretched from Neck Base to Head every frame.")]
    [SerializeField] Transform neck;
    [SerializeField] Transform neckBase;
    [SerializeField] Transform plateTarget;
    [SerializeField] Transform playerTarget;
    [SerializeField] float biteDelay = 0.35f;
    [SerializeField] float lungeDuration = 0.3f;
    [SerializeField] float biteHold = 0.15f;
    [SerializeField] float returnDuration = 0.45f;
    [SerializeField] float eatPlayerDelay = 1.8f;
    [SerializeField] float eatPlayerDuration = 0.35f;
    [Tooltip("Fired when the head reaches the plate.")]
    [SerializeField] UnityEvent onPlateBite = new();
    [Tooltip("Fired when the head reaches the player.")]
    [SerializeField] UnityEvent onPlayerBite = new();

    Vector3 idlePosition;
    Quaternion idleRotation;

    void Awake()
    {
        idlePosition = head.position;
        idleRotation = head.rotation;
    }

    void OnEnable()
    {
        game.Served += OnServed;
        game.TimeUp += OnTimeUp;
        game.Final += EatPlayer;
        game.Restarted += OnRestarted;
    }

    void OnDisable()
    {
        game.Served -= OnServed;
        game.TimeUp -= OnTimeUp;
        game.Final -= EatPlayer;
        game.Restarted -= OnRestarted;
    }

    void OnServed(ServeResult result) => Play(EatPlate());

    void OnTimeUp(IReadOnlyList<IngredientData> recipe) => EatPlayer();

    void EatPlayer() => Play(EatPlayerRoutine());

    void OnRestarted()
    {
        StopAllCoroutines();
        head.SetPositionAndRotation(idlePosition, idleRotation);
    }

    void Play(IEnumerator routine)
    {
        StopAllCoroutines();
        StartCoroutine(routine);
    }

    IEnumerator EatPlate()
    {
        yield return new WaitForSeconds(biteDelay);
        yield return MoveHead(plateTarget.position, plateTarget.rotation, lungeDuration);
        onPlateBite.Invoke();
        yield return new WaitForSeconds(biteHold);
        yield return MoveHead(idlePosition, idleRotation, returnDuration);
    }

    IEnumerator EatPlayerRoutine()
    {
        yield return new WaitForSeconds(eatPlayerDelay);
        yield return MoveHead(playerTarget.position, playerTarget.rotation, eatPlayerDuration);
        onPlayerBite.Invoke();
    }

    IEnumerator MoveHead(Vector3 to, Quaternion toRotation, float duration)
    {
        Vector3 from = head.position;
        Quaternion fromRotation = head.rotation;
        for (float t = 0f; t < 1f; t += Time.deltaTime / duration)
        {
            float e = Mathf.SmoothStep(0f, 1f, t);
            head.SetPositionAndRotation(Vector3.Lerp(from, to, e), Quaternion.Slerp(fromRotation, toRotation, e));
            yield return null;
        }
        head.SetPositionAndRotation(to, toRotation);
    }

    void LateUpdate()
    {
        Vector3 from = neckBase.position, to = head.position;
        neck.SetPositionAndRotation((from + to) * 0.5f, Quaternion.FromToRotation(Vector3.up, to - from));
        var scale = neck.localScale;
        scale.y = Vector3.Distance(from, to) * 0.5f; // Unity's cylinder is 2 units tall
        neck.localScale = scale;
    }
}
