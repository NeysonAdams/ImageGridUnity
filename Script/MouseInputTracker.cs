using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum Axis { None, XAxis, YAxis }

/// <summary>
/// Tracks mouse input events globally and determines the primary direction of movement (X or Y axis).
/// Sends the determined axis to listeners and handles mouse movement tracking lifecycle.
/// </summary>
public class MouseInputTracker : MonoBehaviour
{
    public static MouseInputTracker Instance;

    public Axis CurrentAxis { get; private set; } = Axis.None;

    private Vector2 initialMousePosition;   // Position where mouse button was first pressed
    private bool isTrackingMove = false;    // Whether mouse movement is currently being tracked
    private bool isFirstClick = true;       // Used to skip delta check on the first frame

    // Event fired when a movement axis has been determined.
    public Action<Axis> SendAxis;

    // Event fired when mouse button is released (tracking ends).
    public Action<Axis> ForceStopMouseTrackingAction;


    private void Awake()
    {
        Instance = this;
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0) && (CurrentAxis == Axis.None))
        {
            initialMousePosition = Input.mousePosition;
            isTrackingMove = true;
        }

        MouseMoveHandler();
        

        if (Input.GetMouseButtonUp(0))
        {
            ForceStopMouseTrackingAction?.Invoke(CurrentAxis);
        }

    }

    /// <summary>
    /// Handles movement detection and axis determination after mouse down.
    /// </summary>
    private void MouseMoveHandler()
    {
        if (!(isTrackingMove && Input.GetMouseButton(0))) return;
        if (isFirstClick)
        {
            isFirstClick = false;
            return;
        }
        isFirstClick = true;

        Vector2 currentMousePosition = Input.mousePosition;
        Vector2 delta = currentMousePosition - initialMousePosition;

        if (CurrentAxis == Axis.None)
        {
            if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y) && Mathf.Abs(delta.x) > 10)
            {
                CurrentAxis = Axis.XAxis;
            }
            else if (Mathf.Abs(delta.y) > Mathf.Abs(delta.x) && Mathf.Abs(delta.y) > 10)
            {
                CurrentAxis = Axis.YAxis;
            }
            else
            {
                CurrentAxis = Axis.None;
            }

            //Debug.Log(delta);

            SendAxis?.Invoke(CurrentAxis);
        }
    }

    /// <summary>
    /// Manually stops mouse tracking and resets the state.
    /// Should be called externally after processing ends.
    /// </summary>
    public void StopMouseTracking()
    {
        isTrackingMove = false;
        CurrentAxis = Axis.None;
        initialMousePosition = Vector2.zero;
    }
}
