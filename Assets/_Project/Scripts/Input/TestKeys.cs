using UnityEngine;
using UnityEngine.InputSystem;

// Test shortcuts, editor and development builds only.
// N = plate bite, M = player bite, V = rage (the dragon comes back after each), B = time runs out.
public class TestKeys : MonoBehaviour
{
    [SerializeField] RunTimer timer;
    [SerializeField] DragonRigAnimator dragon;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    void Update()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null) return;
        if (keyboard.nKey.wasPressedThisFrame) dragon.PlayPlateBite();
        if (keyboard.mKey.wasPressedThisFrame) dragon.PlayPlayerBite(true);
        if (keyboard.vKey.wasPressedThisFrame) dragon.PlayRage();
        if (keyboard.bKey.wasPressedThisFrame) timer.EndNow();
    }
#endif
}
