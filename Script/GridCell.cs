using DG.Tweening;
using NestedScroll.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class GridCell : MonoBehaviour,
    IPointerDownHandler, IPointerUpHandler, IPointerClickHandler,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private Vector2Int gridPosition;
    [SerializeField] private RectTransform interactiveRectTrasform;
    [SerializeField] private Image image;

    private BoxCollider2D m_boxCollider;
    private Canvas m_canvas;
    private RectTransform m_rectTransform;
    private ScrollRect m_parentScroll;

    private bool m_canDrag = false;
    private Coroutine m_holdCheckRoutine;
    private Vector2 m_dragOffset;
    private Vector2 m_holdedPosition;
    private bool m_isColapsed = false;
    private bool m_isMoved = false;

    public Action<Vector2Int, Axis> CallPosition;

    private void Awake()
    {
        m_boxCollider = gameObject.GetComponent<BoxCollider2D>();
        m_rectTransform = GetComponent<RectTransform>();
        m_canvas = GetComponentInParent<Canvas>();
        m_parentScroll = GetComponentInParent<ScrollRect>();
    }

    #region Getters
    public Sprite GetSprite() => image.sprite;
    public Vector2Int GridPosition => gridPosition;
    public bool IsCollapsed => m_isColapsed;
    #endregion

    #region Setters
    public void SetSprite(Sprite sprite) => image.sprite = sprite;

    public void SetGridPosition(Vector2Int position) => this.gridPosition = position;

    public void SetColliderSize(Vector2 size)
    {
        if (m_boxCollider != null) m_boxCollider.size = size;
    }
    #endregion

    #region Infinite Scroll Logic
    /// <summary>
    /// Checks whether this cell has moved outside the defined scrollable grid bounds.
    /// If the cell crosses any boundary (left, right, top, or bottom),
    /// its logical grid position is updated accordingly. This allows the layout system
    /// (FullScreenGridLayout) to reposition it correctly in the infinite scroll.
    /// </summary>
    /// <param name="left">World-space X position of the left boundary</param>
    /// <param name="right">World-space X position of the right boundary</param>
    /// <param name="top">World-space Y position of the top boundary</param>
    /// <param name="bottom">World-space Y position of the bottom boundary</param>
    /// <param name="step">The number of grid units to shift when repositioning</param>
    public void CheckOutOfBound(float left, float right, float top, float bottom, int step)
    {
        Vector2 pos = transform.position;

        if (pos.x < left)
            gridPosition += new Vector2Int(step, 0);
        else if (pos.x > right)
            gridPosition += new Vector2Int(-step, 0);
        else if (pos.y > top)
            gridPosition += new Vector2Int(0, -step);
        else if (pos.y < bottom)
            gridPosition += new Vector2Int(0, step);
    }
    #endregion

    #region Drag and Drop Logic

    /// <summary>
    /// Called when the user presses down on the cell. Starts the hold-check coroutine
    /// to detect if the user is intentionally trying to drag (after 1 second hold without scrolling).
    /// </summary>
    public void OnPointerDown(PointerEventData eventData)
    {
        StartCoroutine(HoldForAxis());
        if (m_holdCheckRoutine != null)
            StopCoroutine(m_holdCheckRoutine);

        m_holdCheckRoutine = StartCoroutine(HoldCheck(eventData));
    }

    /// <summary>
    /// Called when the pointer is released. Stops the hold-check coroutine and re-enables scrolling.
    /// </summary>
    public void OnPointerUp(PointerEventData eventData)
    {
        if (m_holdCheckRoutine != null)
        {
            StopCoroutine(m_holdCheckRoutine);
            m_holdCheckRoutine = null;
        }

        if (m_parentScroll != null)
            m_parentScroll.enabled = true;

    }

    /// <summary>
    /// Coroutine to determine whether the user is holding the cell for drag activation.
    /// If the ScrollRect is being actively moved (velocity > 2), dragging is aborted.
    /// </summary>
    private IEnumerator HoldCheck(PointerEventData eventData)
    {
        float timer = 0f;
        const float holdTime = 1f;

        while (timer < holdTime)
        {
            if (m_parentScroll.velocity.magnitude > 2f)
            {
                m_isMoved = true;
                yield break;
            }// User is scrolling, cancel hold

            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        m_canDrag = true;
        m_holdedPosition = transform.position;
        if (m_parentScroll != null)
            m_parentScroll.enabled = false;

        // Calculate offset between mouse and object center
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint);

        m_dragOffset = m_rectTransform.anchoredPosition - localPoint;
    }

    private IEnumerator HoldForAxis()
    {
        while (MouseInputTracker.Instance.CurrentAxis == Axis.None)
            yield return null;

        CallPosition?.Invoke(gridPosition, MouseInputTracker.Instance.CurrentAxis);
    }

    /// <summary>
    /// Called when dragging begins. If drag is not permitted, forward the event to ScrollRect manually.
    /// Otherwise, start drag visuals.
    /// </summary>
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (!m_canDrag)
        {
            // Forward drag event to ScrollRect manually
            ExecuteEvents.ExecuteHierarchy(
                m_parentScroll.gameObject,
                eventData,
                ExecuteEvents.beginDragHandler);
            return;
        }

        GridManager.Instance.SetLayoutActive(false);
        // Brings the dragged cell to the front of the hierarchy so it's rendered above others.
        // NOTE: This method can be expensive on large hierarchies — used here as a quick solution 
        // due to time constraints. Consider optimizing or caching if performance becomes an issue.
        m_rectTransform.SetAsLastSibling();

        transform.DOScale(1.05f, 0.15f).SetEase(Ease.OutQuad);
    }

    /// <summary>
    /// Handles dragging the cell or forwards the drag event to ScrollRect if not in drag mode.
    /// </summary>
    public void OnDrag(PointerEventData eventData)
    {
        if (!m_canDrag)
        {
            ExecuteEvents.ExecuteHierarchy(
                m_parentScroll.gameObject,
                eventData,
                ExecuteEvents.dragHandler
            );
            return;
        }

        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
            m_canvas.transform as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out Vector2 localPoint))
        {
            m_rectTransform.anchoredPosition = localPoint + m_dragOffset;

            GridCell nearest = GetClosestCollidingCell(100);
            SwapCells(nearest);


        }
    }

    /// <summary>
    /// Called when the user stops dragging. Ends the drag operation and animates the cell back.
    /// If not in drag mode, forwards the event to ScrollRect.
    /// </summary>
    public void OnEndDrag(PointerEventData eventData)
    {
        if (!m_canDrag)
        {
            ExecuteEvents.ExecuteHierarchy(
                m_parentScroll.gameObject,
                eventData,
                ExecuteEvents.endDragHandler
            );
            return;
        }

        m_canDrag = false;

        if (m_parentScroll != null)
            m_parentScroll.enabled = true;

        transform.DOScale(1f, 0.15f).SetEase(Ease.OutQuad);

        transform.DOMove(m_holdedPosition, 0.2f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                GridManager.Instance.SetLayoutActive(true);
                m_isMoved = false;
            });
    }

    #endregion

    #region Swap Cells Logic

    /// <summary>
    /// Swaps the current cell with the specified nearby cell.
    /// The swap occurs by animating the positions and exchanging logical grid coordinates.
    /// This operation is triggered when the dragged cell gets close enough to another.
    /// </summary>
    /// <param name="cell">The nearby GridCell to swap with. Must not be null or in motion.</param>
    private void SwapCells(GridCell cell)
    {
        if (cell == null && cell.m_isMoved) return;

        // Save original data for swapping
        Vector2Int gridPos = cell.GridPosition;
        Vector2 worldPosition = cell.transform.position;
        Vector2 hold = m_holdedPosition;

        // Mark the other cell as moving to prevent it from being swapped again during animation
        cell.m_isMoved = true;

        // Animate the other cell to move into the original position of the dragged one
        cell.transform.DOMove(hold, 0.2f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() =>
            {
                // Once animation completes, swap the logical grid positions
                cell.m_isMoved = false;
                cell.SetGridPosition(GridPosition);
                SetGridPosition(gridPos);
                cell.transform.position = hold;
                m_holdedPosition = worldPosition;
            });
    }

    /// <summary>
    /// Finds the closest GridCell that overlaps with the current cell within a given radius.
    /// This method is used to detect potential swap targets during drag.
    /// </summary>
    /// <param name="radius">The maximum distance to check for collisions.</param>
    /// <returns>The closest GridCell within radius, or null if none found.</returns>
    private GridCell GetClosestCollidingCell(float radius)
    {
        // Use Physics2D overlap to detect all nearby colliders
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, radius);
        GridCell closest = null;
        float minDistance = Mathf.Infinity;

        foreach (var col in colliders)
        {
            // Skip nulls and self - collision
            if (col == null || col.gameObject == gameObject) continue;

            GridCell cell = col.GetComponent<GridCell>();
            if (cell != null && cell != this && !cell.m_isMoved)
            {
                // Compute the distance between centers
                float distance = Vector2.Distance(transform.localPosition, cell.transform.localPosition);

                // If this cell is closer than any found before, and within radius, select it
                if (distance < minDistance && distance <= radius)
                {
                    closest = cell;
                    minDistance = distance;
                }
            }
        }

        return closest;
    }
    #endregion

    #region Collapse Logic
    /// <summary>
    /// Called when the user clicks on the cell.
    /// If the cell is not in the middle of a move animation, it informs the GridManager to handle collapse logic.
    /// The actual collapse operation is delegated to GridManager to ensure only one expanded cell at a time.
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        
        if (m_isMoved) return;// Ignore clicks during animations

        // Delegate collapse logic to the GridManager
        GridManager.Instance.SendCollapce(this);
    }

    public void Collapse(float width)
    {
        // Determine direction of expansion based on whether the cell is on the left or right side of the grid
        int kof = gridPosition.x == 2 ? -1 : 1;

        // Calculate target X position for local shift when expanded
        float targetPosX = m_isColapsed ? 0 : kof * (width/2+10);

        // Determine target scale (2.1x width if expanding, 1x if collapsing)
        Vector2 scale = new Vector2(m_isColapsed ? 1 : 2.1f, 1);

        m_rectTransform.SetAsLastSibling();

        m_isMoved = true;

        // Animate scale change
        interactiveRectTrasform.
            DOScale(scale, 0.1f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .OnComplete(() => {
                m_isColapsed = !m_isColapsed;
                m_isMoved = false;
            });
        // Animate horizontal shift to visually offset the expanded cell
        interactiveRectTrasform.DOLocalMoveX(targetPosX, 0.1f);
    }

    public void ResetCell()
    {
        m_isMoved = false;
    }
    #endregion
}
