using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] Camera cam;
    [SerializeField] LayerMask clickMask;
    [Tooltip("Optional. Gets the ingredient under the cursor while playing.")]
    [SerializeField] IngredientHoverFeedback hover;
    [Tooltip("Optional. Clicking its head makes it pull away.")]
    [SerializeField] DragonRigAnimator dragon;
    [SerializeField] InputAction point = new("Point", InputActionType.Value, "<Pointer>/position", expectedControlType: "Vector2");
    [SerializeField] InputAction click = new("Click", InputActionType.Button, "<Pointer>/press");
    [SerializeField] InputAction restart = new("Restart", InputActionType.Button, "<Keyboard>/r");

    void OnEnable()
    {
        point.Enable();
        click.Enable();
        restart.Enable();
    }

    void OnDisable()
    {
        if (hover != null) hover.SetHovered(null);
        point.Disable();
        click.Disable();
        restart.Disable();
    }

    void Update()
    {
        if (restart.WasPressedThisFrame()) game.TryRestart();

        var ray = cam.ScreenPointToRay(point.ReadValue<Vector2>());
        bool pointed = Physics.Raycast(ray, out var hit, Mathf.Infinity, clickMask);
        var view = pointed ? hit.collider.GetComponentInParent<IngredientView>() : null;
        if (hover != null) hover.SetHovered(game.State == GameState.Playing ? view : null);

        if (!click.WasPressedThisFrame() || !pointed) return;
        if (view != null)
        {
            if (view.PlateSlot >= 0) game.TryRemove(view.PlateSlot);
            else game.TrySelect(view.Data);
        }
        else if (dragon != null && hit.collider.GetComponentInParent<DragonRigAnimator>() == dragon)
        {
            dragon.Poke(hit.point);
        }
    }
}
