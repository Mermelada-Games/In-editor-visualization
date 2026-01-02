using UnityEngine;
using System.Collections.Generic;

public class HeatmapVisualizer : MonoBehaviour
{
    [Header("Grid Settings")]
    public int width = 50;
    public int depth = 50;
    public float cellSize = 2f;
    public bool showGrid = true;

    [Header("Visualization")]
    public Gradient colorGradient;
    [Range(0, 1)] public float transparency = 0.8f;

    [HideInInspector]
    public List<Vector3> dataPoints = new();

    private Grid grid;

    public void UpdateHeatmap()
    {
        grid = new Grid(width, depth, cellSize, transform.position);

        foreach (Vector3 point in dataPoints)
        {
            grid.AddValue(point, 15, 1, 3);
        }
    }

    public void AddSinglePoint(Vector3 point)
    {
        if (grid == null) UpdateHeatmap();
        
        dataPoints.Add(point);
        grid.AddValue(point, 15, 1, 3);
    }

    private void OnDrawGizmos()
    {
        if (grid == null || !showGrid) return;

        for (int x = 0; x < grid.GetWidth(); x++)
        {
            for (int z = 0; z < grid.GetDepth(); z++)
            {
                int value = grid.GetValue(x, z);

                if (value <= 0) continue;

                float normalizedValue = (float)value / Grid.HEAT_MAP_MAX_VALUE;
                Color cellColor = colorGradient.Evaluate(normalizedValue);
                cellColor.a = transparency;

                Gizmos.color = cellColor;

                Vector3 cellCenter = grid.GetWorldPosition(x, z) + new Vector3(cellSize, 0, cellSize) * 0.5f;

                Gizmos.DrawCube(cellCenter, new Vector3(cellSize, 0.1f, cellSize));
            }
        }
    }
}
