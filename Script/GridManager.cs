using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using TMPro;
using Unity.VisualScripting;

public class GridManager : MonoBehaviour
{

    private static GridManager instance;

    public static GridManager Instance => instance;

    [Header("Referens")]
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private FullScreenGridLayout staticContent;
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private FullScreenGridLayout layout;

    private ImagePool m_imagePool;

    private int totalRows;
    private int totalCols;
    private int minX = -1, minY = 3;
    private GridCell[,] grid;
    private float leftBound, rightBound, topBound, bottomBound;
    private Vector3 nullPosition;
    private IEnumerable<GridCell> movableCells;

    private void Awake()
    {
        if(instance == null) instance = this;
        m_imagePool = new ImagePool();
    }
    

    private IEnumerator Start()
    {
        totalRows = layout.visibleRows + layout.bufferRows * 2;
        totalCols = layout.visibleColumns + layout.bufferColumns * 2;

        grid = new GridCell[totalCols, totalRows];

        SpawnGrid();
        yield return new WaitForEndOfFrame();
        PutAllCellInStatick();
        CalculateBounds();
    }

    private void SpawnGrid()
    {
        float w = layout.GetCellWidth();
        float h = layout.GetCellHeight();
        for (int y = 0; y < totalRows; y++)
        {
            for (int x = 0; x < totalCols; x++)
            {
                GameObject cellObj = Instantiate(cellPrefab, contentRect);
                GridCell cell = cellObj.GetComponent<GridCell>();

                cell.SetSprite(m_imagePool.Next());

                int gridX = x - layout.bufferColumns;
                int gridY = layout.visibleRows + layout.bufferRows - 1 - y;

                cell.SetGridPosition(new Vector2Int(gridX, gridY));
                cell.SetColliderSize(new Vector2(w,h));
                cell.CallPosition += PutCellsInLine;
                grid[x, y] = cell;
            }
        }
    }

    /// <summary>
    /// Moves all GridCell elements from the grid into the static container,
    /// preserving their world position.
    /// </summary>
    private void PutAllCellInStatick()
    {
        var castGrid = grid.Cast<GridCell>();

        foreach (GridCell cell in castGrid)
            cell.transform.parent = staticContent.transform;
    }

    /// <summary>
    /// Called from a GridCell to move all cells in the same row or column (depending on the given axis)
    /// into the scrollable content container (contentRect).
    /// </summary>
    /// <param name="gridPosition">The grid position of the selected cell.</param>
    /// <param name="axis">The axis along which the selection is made (XAxis or YAxis).</param>
    public void PutCellsInLine(Vector2Int gridPosition, Axis axis)
    {
        if (axis == Axis.None) return;
        var castGrid = grid.Cast<GridCell>();

        movableCells = axis switch
        {
            Axis.XAxis => castGrid.Where(cell => cell.GridPosition.y == gridPosition.y),
            Axis.YAxis => castGrid.Where(cell => cell.GridPosition.x == gridPosition.x),
            _ => Enumerable.Empty<GridCell>()
        };
        foreach (var cell in movableCells)
        {
            cell.transform.parent = contentRect;
        }

        
    }

    public void SetLayoutActive(bool active)
    {
        layout.enabled = active;
    }
    #region Infinite Scroll Logic

    /// <summary>
    /// Calculates the world-space boundaries of the visible grid area.
    /// These bounds are used to detect when grid elements move out of view
    /// so they can be repositioned for infinite scrolling.
    /// </summary>
    private void CalculateBounds()
    {
        // Get the RectTransform of the top-left cell
        RectTransform cellRect = grid[0, 0].GetComponent<RectTransform>();

        // Get the world corners of the cell (bottom-left, top-left, top-right, bottom-right)
        Vector3[] corners = new Vector3[4];
        cellRect.GetWorldCorners(corners);

        // Calculate cell size in world units
        float worldCellWidth = Vector3.Distance(corners[0], corners[3]); 
        float worldCellHeight = Vector3.Distance(corners[0], corners[1]); 

        Vector3 left = grid[0, totalRows / 2].transform.position;
        Vector3 right = grid[totalCols - 1, totalRows / 2].transform.position;
        Vector3 top = grid[totalCols / 2, 0].transform.position;
        Vector3 bottom = grid[totalCols / 2, totalRows - 1].transform.position;

        // Define bounds
        leftBound = left.x - worldCellWidth / 2f;
        rightBound = right.x + worldCellWidth / 2f;
        topBound = top.y + worldCellHeight / 2f;
        bottomBound = bottom.y - worldCellHeight / 2f;

        //This null position we storing for the Snap-A-Step
        nullPosition = grid[1,1].transform.position;

        //Debug.Log($"Bounds (float): Left = {leftBound}, Right = {rightBound}, Top = {topBound}, Bottom = {bottomBound}");
    }

    /// <summary>
    /// Iterates through all grid cells and checks if any have crossed the visible boundaries.
    /// If a cell is out of bounds, it will be repositioned accordingly to maintain infinite scrolling.
    /// </summary>
    public void GridUpdate()
    {
        foreach (var cell in grid)
            cell.CheckOutOfBound(
                leftBound,
                rightBound, 
                topBound, 
                leftBound, 
                layout.GetTotalColumns());
    }

    /// <summary>
    /// Calculates the offset between the current grid position and the reference "null" position.
    /// This is used after the ScrollRect has stopped scrolling to determine how far the grid
    /// has shifted from its expected snap alignment.
    /// </summary>
    public Vector2 GetSnapOffset()
    {
        var castGrid = movableCells;
        var axis = MouseInputTracker.Instance.CurrentAxis;

        // Find the minimum X and maximum Y among all grid cells
        minX = castGrid.Where(c => c != null).Min(c => c.GridPosition.x);
        minY = castGrid.Where(c => c != null).Max(c => c.GridPosition.y);

        // Find the target cell that is one step right and one step down from the top-left corner
        GridCell targetCell = castGrid
            .FirstOrDefault(c => c != null &&
                                 (c.GridPosition.x == minX+1 && axis == Axis.XAxis) ||
                                 (c.GridPosition.y == minY-1 && axis == Axis.YAxis));


        // Get the current world position of that target cell
        Vector3 targetPosition = targetCell.transform.position;
        
        Vector3 extraNullPosition = new Vector3(
            axis == Axis.XAxis ? nullPosition.x : targetPosition.x,
            axis == Axis.YAxis ? nullPosition.y : targetPosition.y,
            nullPosition.z);

        return targetPosition - extraNullPosition;
    }
    /// <summary>
    /// Resets the grid by collecting all movable GridCell components from the scrollable content area,
    /// sorting them along the current mouse movement axis (X or Y),
    /// assigning them new sequential grid positions, and reparenting them to a static container
    /// while preserving their world positions.
    /// </summary>
    public void ResetGreed()
    {
        // Get all active GridCell components from the scrollable content
        movableCells = contentRect.GetComponentsInChildren<GridCell>(includeInactive: false);

        // Get the current axis along which the mouse moved (X or Y)
        var axis = MouseInputTracker.Instance.CurrentAxis;

        // Sort cells by local position.x if X-axis, otherwise by local position.y
        movableCells = movableCells.OrderBy(c => axis == Axis.XAxis ?
                                                        c.transform.localPosition.x :
                                                        c.transform.localPosition.y);

        int i = -1;// Index counter used to assign new grid positions

        foreach (var cell in movableCells)
        {
            // Set a new grid position for the cell along the active axis
            cell.SetGridPosition(new Vector2Int(axis == Axis.XAxis ? i : cell.GridPosition.x,
                                                axis == Axis.YAxis ? i : cell.GridPosition.y));
            i++;
            cell.transform.SetParent(staticContent.transform, worldPositionStays: true);

            cell.ResetCell();
            
        }


    }

    #endregion


    #region Collapse Logic

    
    /// <summary>
    /// Centralized method for handling cell collapse logic. 
    /// Ensures that only one cell is expanded at a time.
    /// If another cell is already expanded, it will be collapsed before expanding the new one.
    /// </summary>
    /// <param name="cell">The cell that was clicked and should be expanded</param>
    public void SendCollapce(GridCell cell)
    {
        float width = layout.GetCellWidth();
        foreach (GridCell c in grid)
            if (c.IsCollapsed && !c.Equals(cell)) c.Collapse(width);

        cell.Collapse(width);
    }
    #endregion
}
