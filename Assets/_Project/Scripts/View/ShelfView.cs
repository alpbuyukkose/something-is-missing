using System.Collections.Generic;
using UnityEngine;

public class ShelfView : MonoBehaviour
{
    [Tooltip("One anchor per board, top to bottom, on the board surface. Items spread along the anchor's local X.")]
    [SerializeField] Transform[] rows;
    [SerializeField] float rowWidth = 0.9f;

    public void Layout(IReadOnlyList<IngredientView> items)
    {
        if (rows.Length == 0) return;
        int index = 0;
        for (int r = 0; r < rows.Length; r++)
        {
            int count = items.Count / rows.Length + (r < items.Count % rows.Length ? 1 : 0);
            for (int i = 0; i < count; i++, index++)
            {
                float x = ((i + 0.5f) / count - 0.5f) * rowWidth;
                items[index].SetHome(rows[r].TransformPoint(x, 0f, 0f), rows[r].rotation);
            }
        }
    }
}
