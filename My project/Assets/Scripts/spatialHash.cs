using System.Collections.Generic;
using UnityEngine;

public class spatialHash
{
    private Dictionary<Vector3Int, List<Vector3>> hashGrid;
    private float cellSize;

    public spatialHash(float cellSize)
    {
        this.cellSize = cellSize;
        hashGrid = new Dictionary<Vector3Int, List<Vector3>>();
    }

    // Compute cell index for a position.
    private Vector3Int Hash(Vector3 position)
    {
        int x = Mathf.FloorToInt(position.x / cellSize);
        int y = Mathf.FloorToInt(position.y / cellSize);
        int z = Mathf.FloorToInt(position.z / cellSize);
        return new Vector3Int(x, y, z);
    }

    // Insert an array of elements.
    public void Insert(Vector3[] elements)
    {
        foreach (var elem in elements)
        {
            Vector3Int cell = Hash(elem);
            if (!hashGrid.TryGetValue(cell, out List<Vector3> list))
            {
                list = new List<Vector3>();
                hashGrid[cell] = list;
            }
            list.Add(elem);
        }
    }

    // Update the hash using current positions.
    public void UpdateHash(IEnumerable<Vector3> elements)
    {
        hashGrid.Clear();
        foreach (var elem in elements)
        {
            Vector3Int cell = Hash(elem);
            if (!hashGrid.TryGetValue(cell, out List<Vector3> list))
            {
                list = new List<Vector3>();
                hashGrid[cell] = list;
            }
            list.Add(elem);
        }
    }

    // Query nearby elements.
    public void Query(Vector3 position, float radius, List<Vector3> result)
    {
        result.Clear();
        Vector3Int centerCell = Hash(position);
        int cellRadius = Mathf.CeilToInt(radius / cellSize);
        for (int x = -cellRadius; x <= cellRadius; x++)
        {
            for (int y = -cellRadius; y <= cellRadius; y++)
            {
                for (int z = -cellRadius; z <= cellRadius; z++)
                {
                    Vector3Int cell = new Vector3Int(centerCell.x + x, centerCell.y + y, centerCell.z + z);
                    if (hashGrid.TryGetValue(cell, out List<Vector3> list))
                    {
                        foreach (var elem in list)
                        {
                            if (Vector3.Distance(elem, position) <= radius)
                                result.Add(elem);
                        }
                    }
                }
            }
        }
    }

    // For debugging: number of cells.
    //public int GetCellCount()
    //{
    //    return hashGrid.Count;
    //}
}
