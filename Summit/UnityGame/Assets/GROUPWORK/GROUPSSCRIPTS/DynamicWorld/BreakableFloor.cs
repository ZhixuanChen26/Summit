using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;
using System.Collections.Generic;

public class BreakableFloor : MonoBehaviour
{
    public Tilemap tilemap; // Reference to the tilemap
    public float breakDelay = 0.5f; // Delay between breaking each tile
    public float restoreDelay = 10f; // Time before tiles reappear

    private Dictionary<Vector3Int, TileBase> originalTiles = new Dictionary<Vector3Int, TileBase>();

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.tag == "Player")
        {
            Vector2 contact = collision.GetContact(0).point;
            Vector3Int startTile = tilemap.WorldToCell(contact);
            StartCoroutine(DropPlat(startTile));
        }
    }

    private IEnumerator DropPlat(Vector3Int startTile)
    {
        List<Vector3Int> platformTiles = GetConnectedTiles(startTile);

        foreach (Vector3Int tile in platformTiles)
        {
            if (tilemap.HasTile(tile))
            {
                // Store the original tile before breaking
                originalTiles[tile] = tilemap.GetTile(tile);

                StartCoroutine(AnimateTileBreak(tile)); // Play animation before breaking
                yield return new WaitForSeconds(breakDelay); // Wait before breaking the next tile
            }
        }

        // Start coroutine to restore tiles after a delay
        StartCoroutine(RestoreTiles(platformTiles));
    }

    private IEnumerator AnimateTileBreak(Vector3Int tilePosition)
    {
        tilemap.SetColor(tilePosition, Color.red); // Change color to indicate breaking
        yield return new WaitForSeconds(0.3f); // Short delay before breaking
        tilemap.SetTile(tilePosition, null); // Remove the tile
    }

    private IEnumerator RestoreTiles(List<Vector3Int> tilesToRestore)
    {
        yield return new WaitForSeconds(restoreDelay); // Wait before restoring

        foreach (Vector3Int tile in tilesToRestore)
        {
            if (originalTiles.ContainsKey(tile))
            {
                tilemap.SetTile(tile, originalTiles[tile]); // Restore tile
            }
        }
    }

    private List<Vector3Int> GetConnectedTiles(Vector3Int startTile)
    {
        List<Vector3Int> tilesToBreak = new List<Vector3Int>();
        Queue<Vector3Int> queue = new Queue<Vector3Int>();
        HashSet<Vector3Int> visited = new HashSet<Vector3Int>();

        queue.Enqueue(startTile);
        visited.Add(startTile);

        while (queue.Count > 0)
        {
            Vector3Int currentTile = queue.Dequeue();
            tilesToBreak.Add(currentTile);

            Vector3Int[] neighbors = new Vector3Int[]
            {
                currentTile + Vector3Int.right,
                currentTile + Vector3Int.left,
                currentTile + Vector3Int.up,
                currentTile + Vector3Int.down
            };

            foreach (Vector3Int neighbor in neighbors)
            {
                if (!visited.Contains(neighbor) && tilemap.HasTile(neighbor))
                {
                    queue.Enqueue(neighbor);
                    visited.Add(neighbor);
                }
            }
        }

        return tilesToBreak;
    }
}
