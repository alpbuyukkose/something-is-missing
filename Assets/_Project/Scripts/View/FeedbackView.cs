using System.Collections.Generic;
using UnityEngine;

// Shows the last serve as a row of colored tiles with the served ingredient on each.
// Move, rotate and scale this object to place the row; the Scene view outlines it while not playing.
public class FeedbackView : MonoBehaviour
{
    [SerializeField] GameController game;
    [Tooltip("Colored base under each mini ingredient, pivot on its top surface.")]
    [SerializeField] GameObject tilePrefab;
    [SerializeField] GameObject greyboxPrefab;

    [Header("Layout (this row's local units)")]
    [Tooltip("Distance between tile centers.")]
    [SerializeField] float spacing = 0.3f;
    [Tooltip("Tile size multiplier. 1 = the prefab's size.")]
    [SerializeField] float tileScale = 1f;
    [Tooltip("Largest side of the mini ingredient on its tile. Any model is scaled to fit.")]
    [SerializeField] float miniSize = 0.16f;

    [Header("Colors")]
    [SerializeField] Color correctColor = new(0.25f, 0.68f, 0.29f);
    [SerializeField] Color presentColor = new(0.95f, 0.77f, 0.06f);
    [SerializeField] Color absentColor = new(0.3f, 0.3f, 0.3f);

    [Header("Editor Preview")]
    [Tooltip("Outlines this many sample tiles in the Scene view while not playing. 0 = off.")]
#pragma warning disable CS0414 // only read in the editor
    [SerializeField] int previewTiles = 5;
#pragma warning restore CS0414

    readonly List<GameObject> tiles = new();

    void OnEnable()
    {
        game.DishStarted += OnDishStarted;
        game.DishWrong += Show;
        game.DishCorrect += Show;
        game.Restarted += Clear;
    }

    void OnDisable()
    {
        game.DishStarted -= OnDishStarted;
        game.DishWrong -= Show;
        game.DishCorrect -= Show;
        game.Restarted -= Clear;
    }

    void OnDishStarted(int index, int length) => Clear();

    void Show(ServeResult result)
    {
        Clear();
        int count = result.Guess.Length;
        for (int i = 0; i < count; i++)
        {
            var tile = Instantiate(tilePrefab, transform, false);
            tile.transform.localPosition = TilePosition(i, count);
            tile.transform.localScale = Vector3.one * tileScale;
            IngredientVisual.Tint(tile, ColorOf(result.Feedback[i]));

            var mini = IngredientVisual.Create(result.Guess[i], greyboxPrefab, tile.transform);
            foreach (var c in mini.GetComponentsInChildren<Collider>()) Destroy(c);
            FitOnTile(mini.transform, tile.transform);
            tiles.Add(tile);
        }
    }

    Vector3 TilePosition(int index, int count) => new((index - (count - 1) * 0.5f) * spacing, 0f, 0f);

    // Scales the mini so its largest side is miniSize and sets its bottom center on the tile's pivot.
    void FitOnTile(Transform mini, Transform tile)
    {
        var bounds = WorldBounds(mini);
        float largest = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
        if (largest <= 0f) return;
        mini.localScale *= miniSize * transform.lossyScale.x / largest;
        bounds = WorldBounds(mini);
        mini.position += tile.position - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    static Bounds WorldBounds(Transform root)
    {
        var renderers = root.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return default;
        var bounds = renderers[0].bounds;
        foreach (var r in renderers) bounds.Encapsulate(r.bounds);
        return bounds;
    }

    Color ColorOf(FeedbackType type) => type switch
    {
        FeedbackType.Correct => correctColor,
        FeedbackType.Present => presentColor,
        _ => absentColor,
    };

    void Clear()
    {
        foreach (var tile in tiles) Destroy(tile);
        tiles.Clear();
    }

#if UNITY_EDITOR
    // Outlines sample tiles (and the space each mini ingredient takes) so the row can be placed without playing.
    void OnDrawGizmos()
    {
        if (Application.isPlaying || previewTiles <= 0 || tilePrefab == null) return;
        var prefabRoot = tilePrefab.transform;
        var meshes = tilePrefab.GetComponentsInChildren<MeshFilter>();
        for (int i = 0; i < previewTiles; i++)
        {
            Vector3 position = TilePosition(i, previewTiles);
            var tileMatrix = transform.localToWorldMatrix * Matrix4x4.TRS(position, Quaternion.identity, Vector3.one * tileScale);
            Gizmos.color = new Color(1f, 0.85f, 0.3f);
            foreach (var mf in meshes)
            {
                Gizmos.matrix = tileMatrix * prefabRoot.worldToLocalMatrix * mf.transform.localToWorldMatrix;
                Gizmos.DrawWireMesh(mf.sharedMesh);
            }
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = new Color(1f, 0.85f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(position + Vector3.up * (miniSize * 0.5f), Vector3.one * miniSize);
        }
        Gizmos.matrix = Matrix4x4.identity;
    }
#endif
}
