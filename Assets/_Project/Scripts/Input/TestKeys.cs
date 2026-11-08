using UnityEngine;
using UnityEngine.InputSystem;

// Test shortcuts, off everywhere. Add TEST_KEYS to Scripting Define Symbols to turn them on.
// N = plate bite, M = player bite, V = rage (the dragon comes back after each), B = time runs out, K = skip the intro poem.
public class TestKeys : MonoBehaviour
{
    [SerializeField] RunTimer timer;
    [SerializeField] DragonRigAnimator dragon;
    [SerializeField] GameMenu menu;

#if TEST_KEYS
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.nKey.wasPressedThisFrame) dragon.PlayPlateBite();
        if (keyboard.mKey.wasPressedThisFrame) dragon.PlayPlayerBite(true);
        if (keyboard.vKey.wasPressedThisFrame) dragon.PlayRage();
        if (keyboard.bKey.wasPressedThisFrame) timer.EndNow();
        if (keyboard.kKey.wasPressedThisFrame) menu.SkipPoem();
    }
#endif
}
