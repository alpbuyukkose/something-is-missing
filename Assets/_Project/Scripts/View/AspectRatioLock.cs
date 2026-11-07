using UnityEngine;

// Keeps this camera's image at a fixed aspect ratio. The leftover screen area becomes bars,
// cleared by a separate background camera (URP does not clear outside a camera's viewport).
[RequireComponent(typeof(Camera))]
public class AspectRatioLock : MonoBehaviour
{
    [Tooltip("Target width : height.")]
    [SerializeField] Vector2 aspect = new(16f, 9f);

    Camera cam;
    Vector2Int lastScreen;

    void Awake() => cam = GetComponent<Camera>();

    void OnDisable()
    {
        if (cam != null) cam.rect = new Rect(0f, 0f, 1f, 1f);
        lastScreen = default;
    }

    // Checked every frame because the window can be resized.
    void Update()
    {
        var screen = new Vector2Int(Screen.width, Screen.height);
        if (screen == lastScreen || screen.y == 0) return;
        lastScreen = screen;

        float target = aspect.x / aspect.y;
        float current = (float)screen.x / screen.y;
        cam.rect = current > target
            ? new Rect((1f - target / current) * 0.5f, 0f, target / current, 1f)  // wider screen: bars left and right
            : new Rect(0f, (1f - current / target) * 0.5f, 1f, current / target); // taller screen: bars top and bottom
    }
}
