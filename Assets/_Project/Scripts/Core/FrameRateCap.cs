using UnityEngine;

// Caps the frame rate so the GPU does not render as fast as it can (heat, uneven frame pacing).
public class FrameRateCap : MonoBehaviour
{
    [Tooltip("Frames per second. VSync is turned off so this value is used as is.")]
    [SerializeField] int targetFrameRate = 60;

    void Awake()
    {
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = targetFrameRate;
    }
}
