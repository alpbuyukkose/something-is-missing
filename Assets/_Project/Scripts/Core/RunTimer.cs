using System;
using UnityEngine;

// The run's single clock: starts with the first dish, never pauses, stops at an ending or when the last dish is served right.
// The hourglass and the run loops read RemainingNormalized.
public class RunTimer : MonoBehaviour
{
    public event Action Expired;

    public bool Running { get; private set; }
    public float Duration { get; private set; } = 1f;
    public float Remaining { get; private set; } = 1f;
    // 1 at the start, 0 when time is up.
    public float RemainingNormalized => Remaining / Duration;

    public void Restart(float duration)
    {
        Duration = Mathf.Max(1f, duration);
        Remaining = Duration;
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
