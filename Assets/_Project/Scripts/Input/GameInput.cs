using UnityEngine;
using UnityEngine.InputSystem;

public class GameInput : MonoBehaviour
{
    [SerializeField] GameController game;
    [SerializeField] Camera cam;
    [SerializeField] LayerMask clickMask;
    [Tooltip("Optional. Gets the ingredient under the cursor while playing.")]
    [SerializeField] IngredientHoverFeedback hover;
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
        point.Disable();
        click.Disable();
        restart.Disable();
    }

    void Update()
    {
        if (restart.WasPressedThisFrame()) game.TryRestart();

        var view = PointedIngredient();
        if (hover != null) hover.SetHovered(game.State == GameState.Playing ? view : null);

        if (!click.WasPressedThisFrame() || view == null) return;
        if (view.PlateSlot >= 0) game.TryRemove(view.PlateSlot);
        else game.TrySelect(view.Data);
    }

    IngredientView PointedIngredient()
    {
        var ray = cam.ScreenPointToRay(point.ReadValue<Vector2>());
        return Physics.Raycast(ray, out var hit, Mathf.Infinity, clickMask) ? hit.collider.GetComponentInParent<IngredientView>() : null;
    }
}
