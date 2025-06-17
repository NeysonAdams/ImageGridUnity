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
    [SerializeField] private GameObject cellPrefab;
    [SerializeField] private FullScreenGridLayout layout;

    private ImagePool m_imagePool;

    private int totalRows;
    private int totalCols;
    private int minX = -1, maxY = 3;
    private GridCell[,] grid;
    private float leftBound, rightBound, topBound, bottomBound;
    private Vector3 nullPosition;

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
                grid[x, y] = cell;
            }
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
        var castGrid = grid.Cast<GridCell>();

        // Find the minimum X and maximum Y among all grid cells
        minX = castGrid.Where(c => c != null).Min(c => c.GridPosition.x);
        maxY = castGrid.Where(c => c != null).Max(c => c.GridPosition.y);

        // Find the target cell that is one step right and one step down from the top-left corner
        GridCell targetCell = castGrid
            .FirstOrDefault(c => c != null &&
                                 c.GridPosition.x == minX + 1 &&
                                 c.GridPosition.y == maxY - 1);

        // Get the current world position of that target cell
        Vector3 targetPosition = targetCell.transform.position;

        return targetPosition - nullPosition;
    }

    #endregion


    #region Collapse Logic

    /// <summary>
    /// Determines the expansion direction of a cell based on its horizontal (X) grid position.
    /// Used to decide whether the cell should collapse left or right.
    /// </summary>
    /// <param name="gridPosition">The grid position of the cell</param>
    /// <returns>True if the cell is on the right side (should collapse left), otherwise false</returns>

    public bool CheckGridPosition(Vector2Int gridPosition)
    {
        return minX + 3 == gridPosition.x;
    }
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
