using System;
using UnityEngine;
using UnityEngine.EventSystems;

// Clickable menu text. Hovering highlights it (bigger, more jitter).
[RequireComponent(typeof(DialogTypewriter))]
public class MenuButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Tooltip("Text shown when the menu page opens. Write *word* to emphasize.")]
    [SerializeField] string label;
    [Tooltip("How fast the highlight follows the cursor.")]
    [SerializeField] float highlightSpeed = 8f;

    public event Action Clicked;

    DialogTypewriter typewriter;
    bool hovered;

    public string Label { get => label; set => label = value; }

    void Awake() => typewriter = GetComponent<DialogTypewriter>();

    // Fades the label in from nothing.
    public void Show() => typewriter.Play(label);

    void OnDisable()
    {
        hovered = false;
        if (typewriter != null) typewriter.Highlight = 0f;
    }

    public void OnPointerEnter(PointerEventData eventData) => hovered = true;
    public void OnPointerExit(PointerEventData eventData) => hovered = false;
    public void OnPointerClick(PointerEventData eventData) => Clicked?.Invoke();

    void Update() => typewriter.Highlight = Mathf.MoveTowards(typewriter.Highlight, hovered ? 1f : 0f, Time.unscaledDeltaTime * highlightSpeed);
}
