using UnityEngine;

// A color palette for RetroEffect's Palette mode. Create > Something Is Missing > Screen Palette.
[CreateAssetMenu(menuName = "Something Is Missing/Screen Palette")]
public class ScreenPalette : ScriptableObject
{
    [Tooltip("Up to 32 together with the effect's Keep Colors. Order does not matter.")]
    public Color[] colors = { Color.black, Color.white };
}
