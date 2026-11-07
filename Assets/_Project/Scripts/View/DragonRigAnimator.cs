using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Higher values win: a state can only be replaced by one of the same or higher priority.
public enum DragonState { Idle, Chew, PlateBite, Rage, PlayerBite }

// Procedural motion for the rigged dragon, no animation clips. Idle breathing and sway always run underneath;
// bites bend the neck toward a target with a small CCD IK and snap the jaw shut; chewing follows the eating
// sound; rage rears up and roars. A poke (click on the head) is a short layer on top of idle and chewing.
public class DragonRigAnimator : MonoBehaviour
{
    [System.Serializable]
    class Bite
    {
        public Transform target;
        [Tooltip("Pull back before the lunge, seconds.")]
        public float anticipation = 0.15f;
        public float lunge = 0.2f;
        [Tooltip("Degrees around the jaw bone's local Z. Flip the sign if it opens the wrong way.")]
        public float jawOpen = 30f;
        [Tooltip("Fired when the jaws close.")]
        public UnityEvent onBite = new();
    }

    [SerializeField] GameController game;
    [Tooltip("Endings wait until its line is fully shown.")]
    [SerializeField] DialogTypewriter dialog;

    [Header("Rig")]
    [Tooltip("Neck bones from the base toward the head.")]
    [SerializeField] Transform[] neckBones;
    [SerializeField] Transform head;
    [SerializeField] Transform jaw;
    [Tooltip("Point between the jaws. This is what reaches the target.")]
    [SerializeField] Transform mouth;
    [Tooltip("Moved toward the target when the neck alone cannot reach it.")]
    [SerializeField] Transform body;

    [Header("Idle")]
    [Tooltip("Breathing: neck rises and falls, degrees.")]
    [SerializeField] float breathAngle = 2f;
    [Tooltip("Breaths per second.")]
    [SerializeField] float breathSpeed = 0.3f;
    [Tooltip("Slow side-to-side look, degrees.")]
    [SerializeField] float swayAngle = 4f;
    [SerializeField] float swaySpeed = 0.12f;

    [Header("Bites")]
    [SerializeField] Bite plateBite = new();
    [SerializeField] Bite playerBite = new() { anticipation = 0.25f, lunge = 0.3f, jawOpen = 45f };
    [Tooltip("Seconds between serving and the plate bite.")]
    [SerializeField] float plateBiteDelay = 0.3f;
    [Tooltip("Seconds between the end of the dialog line and the player bite.")]
    [SerializeField] float afterLineDelay = 0.3f;
    [SerializeField] float anticipationDistance = 0.3f;
    [SerializeField] float jawCloseDuration = 0.06f;
    [Tooltip("Pause on the target before going back.")]
    [SerializeField] float biteHold = 0.15f;
    [SerializeField] float returnDuration = 0.45f;

    [Header("Chew (after the plate bite)")]
    [Tooltip("Jaw opening in degrees (y) over seconds after the bite (x). Shaped to the eating sound: big chomps at about 0.35 and 1.3 s.")]
    [SerializeField] AnimationCurve chewJaw = DefaultChew();
    [Tooltip("The head lifts this many degrees when the jaw is fully open (20°).")]
    [SerializeField] float chewNod = 4f;
    [Tooltip("Chewing (and its sounds) runs this much faster after eating the fork.")]
    [SerializeField] float forkChewSpeed = 1.5f;

    [Header("Rage (fork)")]
    [Tooltip("The head rises and pulls away from the player before roaring, world units.")]
    [SerializeField] float rearHeight = 0.5f;
    [SerializeField] float rearBack = 0.4f;
    [SerializeField] float rearDuration = 0.25f;
    [SerializeField] float roarJawOpen = 50f;
    [SerializeField] float roarOpenDuration = 0.1f;
    [SerializeField] float roarCloseDuration = 0.15f;
    [Tooltip("Head tremble while roaring, degrees.")]
    [SerializeField] float roarShakeAngle = 5f;
    [SerializeField] float roarShakeFrequency = 25f;
    [Tooltip("The roar lasts at least this long, then until the dialog line ends. Set it to the scream's length.")]
    [SerializeField] float minRoarDuration = 0.6f;

    [Header("Poke (click on the head)")]
    [Tooltip("How far the head swings away from the clicked side, degrees.")]
    [SerializeField] float pokeAngle = 20f;
    [SerializeField] float pokeDuration = 0.45f;
    [Tooltip("Swing (0-1) over the poke: quick away, slower back.")]
    [SerializeField] AnimationCurve pokeCurve = new(new Keyframe(0f, 0f, 0f, 10f), new Keyframe(0.18f, 1f), new Keyframe(1f, 0f));
    [Tooltip("Clicks closer together than this are ignored.")]
    [SerializeField] float pokeCooldown = 0.2f;

    [Header("IK")]
    [SerializeField] int ikIterations = 8;
    [Tooltip("How much the head itself turns toward the target. Lower keeps the face toward the player and bends the neck more.")]
    [Range(0f, 1f)]
    [SerializeField] float headTurn = 0.4f;
    [Tooltip("How much of the neck's length is used before the body moves forward.")]
    [Range(0.5f, 1f)]
    [SerializeField] float reachFraction = 0.9f;

    public DragonState State { get; private set; }

    // Code-side moments, e.g. for sounds.
    public event System.Action PlayerLunged, PlateBitten, PlayerBitten, Roared;
    // Chewing starts; the value is its speed (1 = normal).
    public event System.Action<float> ChewStarted;
    // A state was cut short by a higher one (its sounds should stop).
    public event System.Action<DragonState> Interrupted;

    Transform[] chain;
    Quaternion[] chainRest, idlePose;
    Quaternion jawRest;
    Vector3 bodyRest;
    float chainLength;

    Vector3 ikTarget, bodyOffset;
    float ikWeight, jawAngle, roarShake, nod;
    float pokeStart = float.NegativeInfinity, pokeSign;

    void Awake()
    {
        chain = new Transform[neckBones.Length + 1];
        neckBones.CopyTo(chain, 0);
        chain[^1] = head;

        chainRest = new Quaternion[chain.Length];
        idlePose = new Quaternion[chain.Length];
        for (int i = 0; i < chain.Length; i++) chainRest[i] = chain[i].localRotation;
        jawRest = jaw.localRotation;
        bodyRest = body.localPosition;

        for (int i = 0; i < chain.Length - 1; i++) chainLength += Vector3.Distance(chain[i].position, chain[i + 1].position);
        chainLength += Vector3.Distance(head.position, mouth.position);
    }

    void OnEnable()
    {
        game.Served += OnServed;
        game.TimeUp += OnTimeUp;
        game.Final += OnFinal;
        game.Restarted += OnRestarted;
    }

    void OnDisable()
    {
        game.Served -= OnServed;
        game.TimeUp -= OnTimeUp;
        game.Final -= OnFinal;
        game.Restarted -= OnRestarted;
    }

    // Without delays or dialog. Used by TestKeys.
    public void PlayPlateBite() => Play(DragonState.PlateBite, PlateBiteAndChew(0f, 1f));
    public void PlayPlayerBite(bool returnAfter) => Play(DragonState.PlayerBite, PlayerBiteTest(returnAfter));
    public void PlayRage() => Play(DragonState.Rage, RageAndReturn());

    // Swings the head away from the clicked point. Only while idle or chewing.
    public void Poke(Vector3 clickPoint)
    {
        if (State is not (DragonState.Idle or DragonState.Chew) || Time.time - pokeStart < pokeCooldown) return;
        // Pick the swing direction that moves the head away from the click, seen from the camera.
        var cam = Camera.main;
        Vector3 away = cam != null ? -cam.transform.right * Mathf.Sign(Vector3.Dot(clickPoint - head.position, cam.transform.right)) : Vector3.right;
        Vector3 headMove = Vector3.Cross(Vector3.up, head.position - chain[0].position); // head motion for a +yaw at the neck base
        pokeSign = Mathf.Sign(Vector3.Dot(headMove, away));
        pokeStart = Time.time;
    }

    // The fork is eaten like any dish (chewed faster), then the dragon rages and bites the player.
    void OnServed(ServeResult result)
    {
        bool fork = game.Fork != null && System.Array.IndexOf(result.Guess, game.Fork) >= 0;
        Play(DragonState.PlateBite, fork ? ForkEnding() : PlateBiteAndChew(plateBiteDelay, 1f));
    }

    void OnTimeUp(IReadOnlyList<IngredientData> recipe) => OnFinal();
    void OnFinal() => Play(DragonState.PlayerBite, PlayerBiteAfterLine());

    void OnRestarted()
    {
        StopAllCoroutines();
        State = DragonState.Idle;
        ikWeight = jawAngle = roarShake = nod = 0f;
        bodyOffset = Vector3.zero;
        pokeStart = float.NegativeInfinity;
    }

    void Play(DragonState state, IEnumerator routine)
    {
        if (state < State) return;
        if (State != DragonState.Idle) Interrupted?.Invoke(State);
        StopAllCoroutines();
        State = state;
        StartCoroutine(RunThenIdle(routine));
    }

    IEnumerator RunThenIdle(IEnumerator routine)
    {
        yield return routine;
        State = DragonState.Idle;
    }

    IEnumerator PlateBiteAndChew(float delay, float chewSpeed)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return Attack(plateBite);
        yield return Chew(chewSpeed);
    }

    IEnumerator ForkEnding()
    {
        yield return PlateBiteAndChew(plateBiteDelay, forkChewSpeed);
        yield return Rage();
        yield return new WaitForSeconds(afterLineDelay);
        yield return Attack(playerBite);
        yield return StayUntilRestart();
    }

    IEnumerator PlayerBiteAfterLine()
    {
        yield return null; // the line starts on the same game event
        yield return WaitForLine();
        yield return new WaitForSeconds(afterLineDelay);
        yield return Attack(playerBite);
        yield return StayUntilRestart();
    }

    // Holds the player bite (no pokes, no idle) until the run restarts.
    static IEnumerator StayUntilRestart()
    {
        while (true) yield return null;
    }

    IEnumerator PlayerBiteTest(bool returnAfter)
    {
        yield return Attack(playerBite);
        if (!returnAfter) yield break;
        yield return new WaitForSeconds(biteHold);
        yield return Return();
    }

    IEnumerator RageAndReturn()
    {
        yield return Rage();
        yield return new WaitForSeconds(biteHold);
        yield return Return();
    }

    IEnumerator WaitForLine()
    {
        while (dialog != null && dialog.IsTyping) yield return null;
    }

    // Starts from the current pose, so a new bite can interrupt the last one without a jump.
    IEnumerator Attack(Bite bite)
    {
        State = bite == plateBite ? DragonState.PlateBite : DragonState.PlayerBite;
        Vector3 start = mouth.position;
        Vector3 goal = bite.target.position;
        Vector3 pulled = start + (start - goal).normalized * anticipationDistance;
        Vector3 push = BodyPush(goal);
        Vector3 startOffset = bodyOffset;
        float startWeight = ikWeight, startJaw = jawAngle, halfOpen = bite.jawOpen * 0.3f;

        yield return Tween(bite.anticipation, t =>
        {
            float e = Ease(t);
            ikTarget = Vector3.Lerp(start, pulled, e);
            ikWeight = Mathf.Lerp(startWeight, 1f, e);
            jawAngle = Mathf.Lerp(startJaw, halfOpen, t);
            nod = Mathf.Lerp(nod, 0f, e);
        });
        if (bite == playerBite) PlayerLunged?.Invoke();
        yield return Tween(bite.lunge, t =>
        {
            float e = t * t; // accelerate into the bite
            ikTarget = Vector3.Lerp(pulled, goal, e);
            bodyOffset = Vector3.Lerp(startOffset, push, e);
            jawAngle = Mathf.Lerp(halfOpen, bite.jawOpen, t);
        });
        yield return Tween(jawCloseDuration, t => jawAngle = Mathf.Lerp(bite.jawOpen, 0f, t));
        bite.onBite.Invoke();
        (bite == plateBite ? PlateBitten : PlayerBitten)?.Invoke();
    }

    // Chews along the jaw curve while the head goes back from the plate.
    IEnumerator Chew(float speed)
    {
        State = DragonState.Chew;
        ChewStarted?.Invoke(speed);
        Vector3 startOffset = bodyOffset;
        float startWeight = ikWeight;
        float duration = Mathf.Max(chewJaw.keys[^1].time, biteHold + returnDuration);
        for (float t = 0f; t < duration; t += Time.deltaTime * speed)
        {
            float back = Ease(Mathf.Clamp01((t - biteHold) / returnDuration));
            ikWeight = startWeight * (1f - back);
            bodyOffset = startOffset * (1f - back);
            jawAngle = chewJaw.Evaluate(t);
            nod = chewNod * jawAngle / 20f;
            yield return null;
        }
        ikWeight = jawAngle = nod = 0f;
        bodyOffset = Vector3.zero;
    }

    // Rears up away from the player, then roars until the dialog line ends.
    IEnumerator Rage()
    {
        State = DragonState.Rage;
        Vector3 start = mouth.position;
        Vector3 away = Vector3.ProjectOnPlane(start - playerBite.target.position, Vector3.up).normalized;
        Vector3 reared = start + Vector3.up * rearHeight + away * rearBack;
        Vector3 startOffset = bodyOffset;
        float startWeight = ikWeight, startJaw = jawAngle;

        yield return Tween(rearDuration, t =>
        {
            float e = Ease(t);
            ikTarget = Vector3.Lerp(start, reared, e);
            ikWeight = Mathf.Lerp(startWeight, 1f, e);
            jawAngle = Mathf.Lerp(startJaw, 0f, e);
            bodyOffset = Vector3.Lerp(startOffset, Vector3.zero, e);
        });

        float roarStart = Time.time;
        Roared?.Invoke();
        yield return Tween(roarOpenDuration, t =>
        {
            jawAngle = Mathf.Lerp(0f, roarJawOpen, Ease(t));
            roarShake = t;
        });
        while (Time.time - roarStart < minRoarDuration) yield return null;
        yield return WaitForLine();
        yield return Tween(roarCloseDuration, t =>
        {
            jawAngle = Mathf.Lerp(roarJawOpen, 0f, t);
            roarShake = 1f - t;
        });
    }

    // Blends from the current IK pose back to idle.
    IEnumerator Return()
    {
        Vector3 startOffset = bodyOffset;
        float startWeight = ikWeight;
        yield return Tween(returnDuration, t =>
        {
            float e = 1f - Ease(t);
            ikWeight = startWeight * e;
            bodyOffset = startOffset * e;
        });
    }

    // Horizontal shift of the body needed for the mouth to reach the goal.
    Vector3 BodyPush(Vector3 goal)
    {
        Vector3 toGoal = goal - chain[0].position;
        float missing = toGoal.magnitude - chainLength * reachFraction;
        if (missing <= 0f) return Vector3.zero;
        Vector3 shift = Vector3.ProjectOnPlane(toGoal, Vector3.up).normalized * missing;
        return body.parent != null ? body.parent.InverseTransformVector(shift) : shift;
    }

    static IEnumerator Tween(float duration, System.Action<float> step)
    {
        for (float t = 0f; t < 1f; t += Time.deltaTime / Mathf.Max(duration, 0.0001f))
        {
            step(t);
            yield return null;
        }
        step(1f);
    }

    static float Ease(float t) => Mathf.SmoothStep(0f, 1f, t);

    // Two big chomps (about 0.35 and 1.3 s, matching the eating sound) with small chews between and a swallow.
    static AnimationCurve DefaultChew() => new(
        new Keyframe(0f, 0f), new Keyframe(0.24f, 18f), new Keyframe(0.35f, 0f),
        new Keyframe(0.5f, 8f), new Keyframe(0.62f, 0f),
        new Keyframe(0.78f, 8f), new Keyframe(0.9f, 0f),
        new Keyframe(1.18f, 18f), new Keyframe(1.3f, 0f),
        new Keyframe(1.45f, 5f), new Keyframe(1.55f, 0f), new Keyframe(1.7f, 0f));

    void LateUpdate()
    {
        for (int i = 0; i < chain.Length; i++) chain[i].localRotation = chainRest[i];
        body.localPosition = bodyRest + bodyOffset;

        // Idle: breathing pitch on the neck base and a slow look around.
        float breath = Mathf.Sin(Time.time * breathSpeed * 2f * Mathf.PI) * breathAngle;
        float sway = Mathf.Sin(Time.time * swaySpeed * 2f * Mathf.PI) * swayAngle;
        Vector3 side = chain[0].forward; // bone Z is the dragon's side axis in this rig
        chain[0].rotation = Quaternion.AngleAxis(sway, Vector3.up) * Quaternion.AngleAxis(breath, side) * chain[0].rotation;

        if (ikWeight > 0f) BlendIK();

        float poke = (Time.time - pokeStart) / pokeDuration;
        if (poke < 1f) chain[0].rotation = Quaternion.AngleAxis(pokeCurve.Evaluate(poke) * pokeAngle * pokeSign, Vector3.up) * chain[0].rotation;

        if (nod != 0f) head.localRotation *= Quaternion.AngleAxis(-nod, Vector3.forward); // -Z lifts the snout in this rig
        if (roarShake > 0f) head.localRotation *= Quaternion.Euler(Noise(Time.time * roarShakeFrequency) * (roarShakeAngle * roarShake));
        jaw.localRotation = jawRest * Quaternion.AngleAxis(jawAngle, Vector3.forward);
    }

    // Smooth noise in -1..1 per axis.
    static Vector3 Noise(float t) => new(
        Mathf.PerlinNoise(t, 0f) * 2f - 1f,
        Mathf.PerlinNoise(t, 17.3f) * 2f - 1f,
        Mathf.PerlinNoise(t, 31.7f) * 2f - 1f);

    // Solves the IK on top of the idle pose and mixes the two by ikWeight.
    void BlendIK()
    {
        for (int i = 0; i < chain.Length; i++) idlePose[i] = chain[i].localRotation;
        SolveIK(ikTarget);
        if (ikWeight >= 1f) return;
        for (int i = 0; i < chain.Length; i++) chain[i].localRotation = Quaternion.Slerp(idlePose[i], chain[i].localRotation, ikWeight);
    }

    // Cyclic coordinate descent: turn each bone, tip to base, so the mouth points at the target.
    void SolveIK(Vector3 target)
    {
        for (int iteration = 0; iteration < ikIterations; iteration++)
        {
            for (int i = chain.Length - 1; i >= 0; i--)
            {
                Vector3 toMouth = mouth.position - chain[i].position;
                Vector3 toTarget = target - chain[i].position;
                if (toMouth.sqrMagnitude < 1e-6f || toTarget.sqrMagnitude < 1e-6f) continue;
                var turn = Quaternion.FromToRotation(toMouth, toTarget);
                if (chain[i] == head) turn = Quaternion.Slerp(Quaternion.identity, turn, headTurn);
                chain[i].rotation = turn * chain[i].rotation;
            }
            if ((mouth.position - target).sqrMagnitude < 1e-4f) break;
        }
    }
}
