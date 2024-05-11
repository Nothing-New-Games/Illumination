using Sirenix.OdinInspector;
using System.ComponentModel;
using System.Linq;
using UnityEngine;

public class Tile : MonoBehaviour
{
    public int X => Mathf.FloorToInt(transform.position.x);
    public int Y => Mathf.FloorToInt(transform.position.y);
    public int Z => Mathf.FloorToInt(transform.position.z);

    public float Size { get; private set; }
    public float Height { get; private set; }

    [BoxGroup("Debugging Data"), DisplayName, DisplayAsString, ShowInInspector]
    public GameObject Contester { get; private set; }
    public bool Contested => Contester != null && !Contester.CheckForPassableTag();

    public static GameObject TilePrefab;

    public GroundBase Parent {  get; private set; }

    public void SetUpNewTile(float size, float height, Transform parent, Vector3 tilePos)
    {
        //Set the parent
        transform.parent = parent;
        //Get the parent's GroundBase component
        Parent = GetComponentInParent<GroundBase>();
        //Set the position
        transform.position = tilePos;

        //Assign the size and height
        Size = size;
        Height = height;

        if (Size != 0 && Height != 0 && Parent != null)
        {
            //Set the scale based on the size and height
            transform.localScale = new Vector3(Size, Height, Size);
            //Assign the name
            name = GenerateTileName;
            //Check if the tile is contested
            CheckForColliders();
        }
        //Error log handling
        else if (Parent == null) Debug.LogError($"No Ground found for tile {name}!");
        else if (Parent.DebuggingWarnings && Parent.DebuggingConsolePrints && Parent.DebuggingModeIsActive) Debug.LogWarning("Attempted changing of tile values was prevented!");
    }

    public override string ToString()
    {
        return $"{name} Contested by: {Contester.name}\nSize: {Size}\nHeight: {Height}";
    }

    [BoxGroup("Debugging Data"), ShowInInspector]
    private string GenerateTileName => $"Tile ({X},{Y},{Z})";


    public GroundType GetGroundType => Parent.GetGroundType;


    private void OnTriggerEnter(Collider other)
    {
        if (other == Parent.Collider || other.GetComponent<Tile>()) return;

        Contester = other.gameObject;
    }
    private void OnTriggerExit(Collider other)
    {
        Contester = null;
        CheckForColliders();
    }


    [Button]
    public void CheckForColliders()
    {
        if (Parent == null)
            Parent = GetComponentInParent<GroundBase>();

        Collider[] colliders = Physics.OverlapBox(transform.position, new Vector3(1, Parent.MaxFloorYPos + Height, 1) / 2)
            .Where(collider => (collider != Parent.Collider || collider != Parent.Collider) && collider.GetComponent<Tile>() == null).ToArray();

        if (colliders.Length > 0)
        {
            Contester = colliders.First(obj => !obj.gameObject.CheckForPassableTag()).gameObject;
        }
    }

    public void OnDrawGizmos()
    {
        if (Parent == null)
            Parent = GetComponentInParent<GroundBase>();


        if (Parent.DebuggingGizmosIsActive)
        {
            if (Parent.DrawContestedTiles && Contester != null)
            {
                Gizmos.color = Parent.ContestedTileGizmoColor;
                Gizmos.DrawWireCube(transform.position, new Vector3(Size, Height, Size));
            }
            else if (Parent.DrawOpenTiles && Contester == null)
            {
                Gizmos.color = Parent.OpenTileGizmoColor;
                Gizmos.DrawWireCube(transform.position, new Vector3(Size, Height, Size));
            }
        }
    }
}
