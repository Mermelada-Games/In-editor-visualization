using UnityEngine;
using System;

public class Grid
{
    public const int HEAT_MAP_MAX_VALUE = 100;
    public const int HEAT_MAP_MIN_VALUE = 0;

    public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;
    public class OnGridValueChangedEventArgs : EventArgs
    {
        public int x;
        public int z;
    }

    private readonly int width;
    private readonly int depth;
    private readonly float cellSize;
    private Vector3 originPosition;
    private readonly int[,] gridArray;

    public Grid(int width, int depth, float cellSize, Vector3 originPosition)
    {
        this.width = width;
        this.depth = depth;
        this.cellSize = cellSize;
        this.originPosition = originPosition;

        gridArray = new int[width, depth];
    }

    public int GetWidth() => width;
    public int GetDepth() => depth;
    public float GetCellSize() => cellSize;

    public Vector3 GetWorldPosition(int x, int z)
    {
        return new Vector3(x, 0, z) * cellSize + originPosition;
    }

    public void GetXZ(Vector3 worldPosition, out int x, out int z)
    {
        x = Mathf.FloorToInt((worldPosition - originPosition).x / cellSize);
        z = Mathf.FloorToInt((worldPosition - originPosition).z / cellSize);
    }

    public void SetValue(int x, int z, int value)
    {
        if (x >= 0 && z >= 0 && x < width && z < depth)
        {
            gridArray[x, z] = Mathf.Clamp(value, HEAT_MAP_MIN_VALUE, HEAT_MAP_MAX_VALUE);
            OnGridValueChanged?.Invoke(this, new OnGridValueChangedEventArgs { x = x, z = z });
        }
    }

    public void SetValue(Vector3 worldPosition, int value)
    {
        GetXZ(worldPosition, out int x, out int z);
        SetValue(x, z, value);
    }

    public void AddValue(int x, int z, int value)
    {
        if (x >= 0 && z >= 0 && x < width && z < depth)
        {
            SetValue(x, z, GetValue(x, z) + value);
        }
    }

    public void AddValue(Vector3 worldPosition, int value, int fullValueRange, int totalRange)
    {
        GetXZ(worldPosition, out int originX, out int originZ);
        
        for (int x = 0; x < totalRange; x++)
        {
            for (int z = 0; z < totalRange - x; z++)
            {
                int radius = x + z;
                int addValueAmount = value;
                if (radius >= fullValueRange)
                {
                    addValueAmount -= Mathf.RoundToInt((float)value * (radius - fullValueRange) / (totalRange - fullValueRange));
                }

                AddValue(originX + x, originZ + z, addValueAmount);

                if (x != 0)
                    AddValue(originX - x, originZ + z, addValueAmount);

                if (z != 0)
                {
                    AddValue(originX + x, originZ - z, addValueAmount);

                    if (x != 0)
                        AddValue(originX - x, originZ - z, addValueAmount);
                }
            }
        }
    }

    public int GetValue(int x, int z)
    {
        if (x >= 0 && z >= 0 && x < width && z < depth)
            return gridArray[x, z];
        else
            return 0;
    }
}
