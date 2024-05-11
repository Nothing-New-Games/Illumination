using NNG.CustomUnityInspector;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GroundBase : MonoBehaviour
{
    [BoxGroup("Debugging Data"), ShowInInspector, ReadOnly]
    private Dictionary<int, List<Tile>> Tiles = new();

    public List<Tile> XTiles(int X) => Tiles[X];
    public List<Tile> XTiles(float X) => Tiles[Mathf.FloorToInt(X)];

    [BoxGroup("Customization"), Required]
    public GameObject TilePrefab;

    private bool previousSizeToggle;
    private void MaintainSizeSliders()
    {
        if (this.previousSizeToggle && !this.UseWholeNumberSizes)
        {
            this.TileSizeFloat = this.TileSizeWholeNumber;
        }
        else if (!this.previousSizeToggle && this.UseWholeNumberSizes)
        {
            if (this.TileSizeFloat > 1)
                this.TileSizeWholeNumber = Mathf.FloorToInt(this.TileSizeFloat);
            else
                this.TileSizeWholeNumber = Mathf.CeilToInt(this.TileSizeFloat);
        }

        this.previousSizeToggle = this.UseWholeNumberSizes;
    }
    private static IEnumerable TileSizeToggle = new ValueDropdownList<bool>()
    {
        { "Whole Numbers", true },
        { "Floats", false }
    };
    [BoxGroup("Customization"), ValueDropdown("TileSizeToggle"), LabelText("Tile Size Type: "), OnValueChanged("MaintainSizeSliders")]
    public bool UseWholeNumberSizes = true;
    [BoxGroup("Customization"), Range(0.1f, 50), HideIf("@UseWholeNumberSizes"), LabelText("Tile Size")]
    public float TileSizeFloat;
    [BoxGroup("Customization"), Range(1, 50), HideIf("@!UseWholeNumberSizes"), LabelText("Tile Size")]
    public int TileSizeWholeNumber;
    [BoxGroup("Customization"), Range(0.1f, 100f)]
    public float TileHeight;

    [BoxGroup("Debugging Data"), ReadOnly, DisplayAsString]
    [Tooltip("This should always be readonly, however, until we figure out the math to calculate this, it will be manually inputed.")]
    public float MaxFloorYPos;

    public Tile GetCurrentTile(int X, int Z) => Tiles[X].First(tile => tile.Z == Z);
    public Tile GetCurrentTile(float X, float Z) => Tiles[Mathf.FloorToInt(X)].First(tile => tile.Z == Mathf.FloorToInt(Z));
    public Tile GetCurrentTile(Vector3 pos) => Tiles[Mathf.FloorToInt(pos.x)].First(tile => tile.Z == Mathf.FloorToInt(pos.z));
    public Tile GetCurrentTile(Vector2 pos) => Tiles[Mathf.FloorToInt(pos.x)].First(tile => tile.Z == Mathf.FloorToInt(pos.y));

    private IEnumerable<bool> GetBooleanOptions()
    {
        yield return true;
        yield return false;
    }
    [BoxGroup("Customization"), ValueDropdown("GetBooleanOptions")]
    public bool AllowOutOfBoundsGeneration = true;
    [BoxGroup("Customization")]
    [ShowInInspector, SerializeField]
    private protected GroundType _Difficulty_ = GroundType.Normal;
    [BoxGroup("Debugging Data"), ShowInInspector, ReadOnly, DisplayAsString]
    public GroundType GetGroundType => _Difficulty_;


    [BoxGroup("Debugging Data"), ReadOnly, DisplayAsString]
    public int _PassableTilesGenerated;
    [BoxGroup("Debugging Data"), ReadOnly, DisplayAsString]
    public int _ImpassableTilesGenerated;


    [Button]
    public void GenerateTiles()
    {
        if (_Collider_ == null)
            _Collider_ = GetGroundCollider();

        Bounds bounds = _Collider_.bounds;

        MaxFloorYPos = bounds.max.y;

        if (DebuggingModeIsActive && DebuggingConsolePrints)
            Debug.Log($"Bounds: {bounds}\nSize: {bounds.size}\nMin: {bounds.min}\nMax: {bounds.max}");

        int totalNeededNodes = Mathf.FloorToInt((bounds.size.x * bounds.size.z) / TileSizeFloat);

        if (DebuggingModeIsActive && DebuggingConsolePrints && DebuggingTileCount)
            Debug.Log($"Number of Nodes Required: {totalNeededNodes}");

        float y = 0f;

        float tileSize = 0f;
        if (UseWholeNumberSizes) tileSize = TileSizeWholeNumber;
        else tileSize = TileSizeFloat;

        // Loop through each tile within the terrain bounds
        for (float x = bounds.min.x; x < bounds.max.x; x += tileSize)
        {
            for (float z = bounds.min.z; z < bounds.max.z; z += tileSize)
            {
                y = this.SampleGroundHeight(new Vector3(x, bounds.max.y + TileHeight, z));

                var newTile = Instantiate(TilePrefab, new (), transform.rotation).GetComponent<Tile>();
                var newTilePos = new Vector3(x + (tileSize / 2), y + (TileHeight / 2), z + (tileSize / 2));

                newTile.SetUpNewTile(tileSize, TileHeight, transform, newTilePos);


                Collider tileCollider = newTile.GetComponent<Collider>();
                if ((!bounds.Contains(newTilePos - tileCollider.bounds.extents - new Vector3(0, TileHeight, 0)) || !bounds.Contains(newTilePos + tileCollider.bounds.extents - new Vector3(0, TileHeight, 0))) && !AllowOutOfBoundsGeneration)
                {
                    Debug.LogWarning($"Destroying tile {newTile.name} for violating bounds!");
                    //Kill it, assuming it's out of bounds.
                    DestroyImmediate(newTile.gameObject);
                    continue;
                }


                newTile.CheckForColliders();
                AddNewTile(newTile);
            }
        }


        //Debugging logs for tile count
        if (DebuggingModeIsActive && DebuggingConsolePrints && DebuggingTileCount)
        {
            _PassableTilesGenerated = Tiles.SelectMany(keyValuePair => keyValuePair.Value).Count(node => node.Contested);
            Debug.Log($"Number of Passable Nodes Created: {_PassableTilesGenerated}");
            _ImpassableTilesGenerated = Tiles.SelectMany(keyValuePair => keyValuePair.Value).Count(node => !node.Contested);
            Debug.Log($"Number of Impassable Nodes Created: {_ImpassableTilesGenerated}");
            int totalNodesCreated = _PassableTilesGenerated + _ImpassableTilesGenerated;
            Debug.Log($"Total nodes created: {totalNodesCreated}");
        }
    }
    [Button(ButtonStyle.FoldoutButton)]
    public void CheckForTiles(bool generateIfNoneFound = false)
    {
        if (Tiles.Count == 0)
        {
            var found = GetComponentsInChildren<Tile>();

            if (found.Length == 0 && generateIfNoneFound)
            {
                GenerateTiles();
                return;
            }

            //This needs to be adjusted for tile sizes... Maybe? It might not, because the coordinates of the tile doesn't change. It's just the size which changes where you have to be to be within that tile.
            //Do we need tile size... this adds a lot to think about....
            foreach (Tile tile in found)
                AddNewTile(tile);
        }
    }
    [Button]
    public void ClearTiles()
    {
        if (Tiles.Count == 0)
            CheckForTiles();
        foreach (var keys in Tiles)
        {
            for (int i = 0; i < keys.Value.Count; i++)
            {
                Tile tile = keys.Value[i];
                DestroyImmediate(tile.gameObject);
            }
        }

        Tiles.Clear();
    }

    private void AddNewTile(Tile tile)
    {
        if (!Tiles.ContainsKey(tile.X))
            Tiles.Add(tile.X, new List<Tile>());

        Tiles[tile.X].Add(tile);
    }

    private Collider _Collider_ { get; set; }
    private Collider GetGroundCollider()
    {
        TerrainCollider terrainCollider = GetComponent<TerrainCollider>();
        Collider collider = GetComponent<Collider>();

        
        if (terrainCollider != null) 
            return terrainCollider;
        else if (collider != null)
            return collider;
        else if (DebuggingErrors && DebuggingConsolePrints && DebuggingModeIsActive)
        {
            Debug.LogError("Unable to get collider!");
        }
        else Debug.LogWarning("Error message was suppressed!");

        return null;
    }

    [BoxGroup("Debugging Toggles")]
    public bool DebuggingModeIsActive = false;
    [BoxGroup("Debugging Toggles"), Indent(1), ShowIf("@DebuggingModeIsActive")]
    public bool DebuggingConsolePrints = false;
    [BoxGroup("Debugging Toggles"), Indent(2), ShowIf("@DebuggingModeIsActive && DebuggingConsolePrints")]
    public bool DebuggingLogs = false;
    [BoxGroup("Debugging Toggles"), Indent(2), ShowIf("@DebuggingModeIsActive && DebuggingConsolePrints")]
    public bool DebuggingWarnings = false;
    [BoxGroup("Debugging Toggles"), Indent(2), ShowIf("@DebuggingModeIsActive && DebuggingConsolePrints")]
    public bool DebuggingErrors = false;
    [BoxGroup("Debugging Toggles"), Indent(2), ShowIf("@DebuggingModeIsActive && DebuggingConsolePrints")]
    public bool DebuggingTileCount = false;

    [BoxGroup("Debugging Toggles"), Indent(1), ShowIf("@DebuggingModeIsActive")]
    public bool DebuggingGizmosIsActive = false;
    [BoxGroup("Debugging Toggles"), Indent(2), ShowIf("@DebuggingModeIsActive && DebuggingGizmosIsActive")]
    public bool DrawContestedTiles = false;
    [BoxGroup("Debugging Toggles"), Indent(2), ShowIf("@DebuggingModeIsActive && DebuggingGizmosIsActive")]
    public bool DrawOpenTiles = false;
    [BoxGroup("Debugging Toggles"), Indent(2), ShowIf("@DebuggingModeIsActive && DebuggingGizmosIsActive")]
    public bool DrawFloorHeight = false;
    [BoxGroup("Debugging Toggles"), Indent(2), ShowIf("@DebuggingModeIsActive && DebuggingGizmosIsActive")]
    public bool DrawTileHeight = false;

    [BoxGroup("Customization"), ShowIf("@DebuggingModeIsActive && DebuggingGizmosIsActive && DrawContestedTiles")]
    public Color ContestedTileGizmoColor = Color.red;
    [BoxGroup("Customization"), ShowIf("@DebuggingModeIsActive && DebuggingGizmosIsActive && DrawOpenTiles")]
    public Color OpenTileGizmoColor = Color.green;
    
    [BoxGroup("Customization"), ShowIf("@DebuggingModeIsActive && DebuggingGizmosIsActive && DrawFloorHeight")]
    public Color FloorHeightGizmoColor = Color.blue;
    [BoxGroup("Customization"), ShowIf("@DebuggingModeIsActive && DebuggingGizmosIsActive && DrawTileHeight")]
    public Color TileHeightGizmoColor = Color.yellow;


    public Collider Collider { get { return _Collider_; } }

    private void Awake()
    {
        _Collider_ = GetGroundCollider();
    }

    public void OnDrawGizmos()
    {
        if (DebuggingGizmosIsActive)
        {
            if (_Collider_ == null)
                _Collider_ = GetComponent<Collider>();
            if (DrawFloorHeight)
            {
                Gizmos.color = FloorHeightGizmoColor;
                Gizmos.DrawWireCube(new Vector3(_Collider_.bounds.center.x, _Collider_.bounds.center.y, _Collider_.bounds.center.z), _Collider_.bounds.size);
            }
            if (DrawTileHeight)
            {
                Gizmos.color = TileHeightGizmoColor;
                Gizmos.DrawCube(new Vector3(_Collider_.bounds.center.x, _Collider_.bounds.max.y + (TileHeight / 2), _Collider_.bounds.center.z), new Vector3(_Collider_.bounds.size.x, TileHeight, _Collider_.bounds.size.z));
            }
        }
    }
}


public enum GroundType
{
    Normal, Difficult, Water
}
