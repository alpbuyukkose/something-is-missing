using System;
using UnityEngine;

// The run's single clock: starts with the first dish, never pauses, stops at an ending.
// The hourglass (and later the music) read RemainingNormalized.
public class RunTimer : MonoBehaviour
{
    [Tooltip("Seconds for the whole run, all dishes together.")]
    [Min(1f)]
    [SerializeField] float runDuration = 200f;

    public event Action Expired;

    public bool Running { get; private set; }
    public float Remaining { get; private set; }
    // 1 at the start, 0 when time is up.
    public float RemainingNormalized => Remaining / runDuration;

    void Awake() => Remaining = runDuration;

    public void Restart()
    {
        Remaining = runDuration;
        Running = true;
    }

    public void Stop() => Running = false;

    // Test shortcut: time runs out on the next frame.
    public void EndNow()
    {
        if (Running) Remaining = 0f;
    }

    void Update()
    {
        if (!Running) return;
        Remaining = Mathf.Max(0f, Remaining - Time.deltaTime);
        if (Remaining > 0f) return;
        Running = false;
        Expired?.Invoke();
    }
}
