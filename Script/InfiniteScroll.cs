using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.EventSystems;
using static UnityEditor.ShaderGraph.Internal.KeywordDependentCollection;
using NestedScroll.Core;

[RequireComponent(typeof(ScrollRect))]
public class InfiniteScroll : MonoBehaviour, IBeginDragHandler, IEndDragHandler
{
    [SerializeField] private float snapThreshold = 250f;
    [SerializeField] private float snapDuration = 0.2f;

    private ScrollRect m_ScrollRect;
    private RectTransform m_contnent;
    private FullScreenGridLayout m_layout;

    private float cellWidth, cellHeight;
    private Vector2 centerPosition;

    private bool m_isUserScrolling;
    private bool m_isSnapping;

    private Vector2 lastVelocity;
    private int snapDirX = 0;
    private int snapDirY = 0;

    private Vector3 m_contentNullPosition;

    private void Awake()
    {
        m_ScrollRect = GetComponent<ScrollRect>();
        m_contnent = m_ScrollRect.content;
        m_layout = m_ScrollRect.content.GetComponent<FullScreenGridLayout>();
        MouseInputTracker.Instance.SendAxis += SetScrollAxis;
        MouseInputTracker.Instance.ForceStopMouseTrackingAction += StopMouseTrackingLowMagnitude;
        m_contentNullPosition = m_contnent.localPosition;
    }

    private void Start()
    {
        cellWidth = m_layout.GetCellWidth();
        cellHeight = m_layout.GetCellHeight();
        centerPosition = Vector2.zero;

        m_ScrollRect.onValueChanged.AddListener(OnScroll);

    }

    private void OnScroll(Vector2 delta)
    {
        GridManager.Instance.GridUpdate();
        m_layout.UpdateLayout();
        lastVelocity = m_ScrollRect.velocity;
    }
    /// <summary>
    /// Called every frame after all Update calls. Checks if conditions are met to perform a snap.
    /// If the user is not interacting with the scroll and the scroll velocity is low but not zero,
    /// a snap step is triggered to align the grid content to the nearest valid cell position.
    /// </summary>
    private void LateUpdate()
    {
        if (!m_isUserScrolling && !m_isSnapping &&
            m_ScrollRect.velocity.magnitude < snapThreshold &&
            m_ScrollRect.velocity.magnitude > 1f)
        {
            PerformSnapStep();
        }
    }
    /// <summary>
    /// Performs a snap animation to align the content to the closest grid alignment.
    /// It calculates the distance between the top-left visible grid cell and the logical origin (null position),
    /// then animates the content to correct that offset.
    /// </summary>
    private void PerformSnapStep()
    {
        m_isSnapping = true;

        // Stop current scroll movement and temporarily remove listener to avoid interference
        m_ScrollRect.velocity = Vector2.zero;
        m_ScrollRect.onValueChanged.RemoveListener(OnScroll);

        // Get the misalignment distance from GridManager
        Vector3 distance = GridManager.Instance.GetSnapOffset();

        // Calculate final corrected position for content
        Vector3 targetPosition = m_contnent.position - distance;

        // Animate the content into aligned position smoothly
        m_contnent.DOMove(targetPosition, snapDuration)
            .SetEase(Ease.OutCubic)
            .SetUpdate(true)
            .OnComplete(() => {
                m_contnent.position = targetPosition;
                m_isSnapping = false;
                m_ScrollRect.onValueChanged.AddListener(OnScroll);
                StartCoroutine( ResetScrollRect());
            });
    }

    /// <summary>
    /// Enables scrolling only along the specified axis (horizontal or vertical).
    /// Disables the other axis to lock scroll direction.
    /// </summary>
    /// <param name="axis">The axis along which scrolling should be allowed (XAxis or YAxis).</param>
    private void SetScrollAxis(Axis axis)
    {
        m_ScrollRect.horizontal = axis == Axis.XAxis;
        m_ScrollRect .vertical = axis == Axis.YAxis;

    }

    /// <summary>
    /// Stops mouse tracking and triggers a snap if the scroll velocity is low
    /// and the user performed a minimal movement gesture.
    /// </summary>
    private void StopMouseTrackingLowMagnitude(Axis axis)
    {
        if (m_ScrollRect.velocity.magnitude > 1 || axis == Axis.None) return;
        PerformSnapStep();
    }

    /// <summary>
    /// Resets the scrollable content position after a row swap is completed.
    /// This ensures the content snaps back to its original position and mouse tracking is stopped.
    /// </summary>
    public IEnumerator ResetScrollRect()
    {
        GridManager.Instance.ResetGreed();
        yield return new WaitForNextFrameUnit();
        m_contnent.localPosition = m_contentNullPosition;
        MouseInputTracker.Instance.StopMouseTracking();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        m_isUserScrolling = true;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        m_isUserScrolling = false;
    }
}
