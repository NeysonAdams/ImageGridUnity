using UnityEngine;
using UnityEngine.UI;
/// <summary>
/// A custom LayoutGroup that arranges child GridCells based on their GridPosition values.
/// The layout uses a virtual grid with both visible and buffer rows/columns to enable smooth
/// infinite scrolling logic. Positioning is calculated based on the cell's assigned grid coordinates.
/// Updating a GridCell's GridPosition will automatically reposition it in the correct layout slot.
/// </summary>
[ExecuteAlways]
public class FullScreenGridLayout : LayoutGroup
{
    public int visibleRows = 3;
    public int visibleColumns = 3;
    public int bufferRows = 1;
    public int bufferColumns = 1;
    public Vector2 spacing = Vector2.zero;

    private float cellWidth, cellHeight;
    private int totalRows, totalCols;

    #region Compomemt Logic
    public override void CalculateLayoutInputHorizontal()
    {
        base.CalculateLayoutInputHorizontal();
        LayoutChildren();
    }

    public override void CalculateLayoutInputVertical() { }
    public override void SetLayoutHorizontal() { CalculateLayoutInputHorizontal(); }
    public override void SetLayoutVertical() { CalculateLayoutInputHorizontal(); }

    /// <summary>
    /// Arranges all child GridCells based on their GridPosition and current layout settings.
    /// </summary>
    private void LayoutChildren()
    {
        totalRows = visibleRows + bufferRows * 2;
        totalCols = visibleColumns + bufferColumns * 2;

        if (totalRows <= 0 || totalCols <= 0) return;

        RectTransform viewport = rectTransform.parent as RectTransform;

        // Calculate cell dimensions based on visible grid size
        cellWidth = (viewport.rect.width - padding.left - padding.right - spacing.x * (visibleColumns - 1)) / visibleColumns;
        cellHeight = (viewport.rect.height - padding.top - padding.bottom - spacing.y * (visibleRows - 1)) / visibleRows;

        float gridWidth = totalCols * cellWidth + (totalCols - 1) * spacing.x;
        float gridHeight = totalRows * cellHeight + (totalRows - 1) * spacing.y;

        // Offset used to center the grid within the parent
        float offsetX = (rectTransform.rect.width - gridWidth) / 2f;
        float offsetY = (rectTransform.rect.height - gridHeight) / 2f;

        int childIndex = 0;
        for (int y = 0; y < totalRows; y++)
        {
            for (int x = 0; x < totalCols; x++)
            {
                if (childIndex >= rectChildren.Count) return;

                RectTransform item = rectChildren[childIndex];

                GridCell cell = item.GetComponent<GridCell>();
                if (cell == null)
                {
                    childIndex++;
                    continue;
                }

                int gridX = cell.GridPosition.x;
                int gridY = cell.GridPosition.y;

                // Compute final position in layout based on GridPosition
                float xPos = offsetX + (gridX + bufferColumns) * (cellWidth + spacing.x);
                float yPos = offsetY + (totalRows - 1 - (gridY + bufferRows)) * (cellHeight + spacing.y);

                SetChildAlongAxis(item, 0, xPos, cellWidth);
                SetChildAlongAxis(item, 1, yPos, cellHeight);

                childIndex++;
            }
        }
    }
    /// <summary>
    /// Forces Unity to re-layout the children in the next frame.
    /// Should be called after GridPosition changes.
    /// </summary>
    public void UpdateLayout()
    {
        SetDirty();
    }
    #endregion

    #region Getters
    public float GetCellWidth()
    {
        RectTransform viewport = rectTransform.parent as RectTransform;
        return (viewport.rect.width - padding.left - padding.right - spacing.x * (visibleColumns - 1)) / visibleColumns;
    }

    public float GetCellHeight()
    {
        RectTransform viewport = rectTransform.parent as RectTransform;
        return (viewport.rect.height - padding.top - padding.bottom - spacing.y * (visibleRows - 1)) / visibleRows;
    }

    public int GetTotalRows() => visibleRows + bufferRows * 2;
    public int GetTotalColumns() => visibleColumns + bufferColumns * 2;
    #endregion

    
}
