// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Experimental.GraphView
{
    public class RectangleSelector : MouseManipulator
    {
        private readonly RectangleSelect m_Rectangle;
        bool m_Active;

        public RectangleSelector()
        {
            activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse });
            if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Command });
            }
            else
            {
                activators.Add(new ManipulatorActivationFilter { button = MouseButton.LeftMouse, modifiers = EventModifiers.Control });
            }
            m_Rectangle = new RectangleSelect();
            m_Rectangle.style.position = Position.Absolute;
            m_Rectangle.style.top = 0f;
            m_Rectangle.style.left = 0f;
            m_Rectangle.style.bottom = 0f;
            m_Rectangle.style.right = 0f;
            m_Active = false;
        }

        // get the axis aligned bound
        public Rect ComputeAxisAlignedBound(Rect position, Matrix4x4 transform)
        {
            Vector3 min = transform.MultiplyPoint3x4(position.min);
            Vector3 max = transform.MultiplyPoint3x4(position.max);
            return Rect.MinMaxRect(Math.Min(min.x, max.x), Math.Min(min.y, max.y), Math.Max(min.x, max.x), Math.Max(min.y, max.y));
        }

        protected override void RegisterCallbacksOnTarget()
        {
            var graphView = target as GraphView;
            if (graphView == null)
            {
                throw new InvalidOperationException("Manipulator can only be added to a GraphView");
            }

            target.RegisterCallback<MouseDownEvent>(OnMouseDown);
            target.RegisterCallback<MouseUpEvent>(OnMouseUp);
            target.RegisterCallback<MouseMoveEvent>(OnMouseMove);
            target.RegisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        protected override void UnregisterCallbacksFromTarget()
        {
            target.UnregisterCallback<MouseDownEvent>(OnMouseDown);
            target.UnregisterCallback<MouseUpEvent>(OnMouseUp);
            target.UnregisterCallback<MouseMoveEvent>(OnMouseMove);
            target.UnregisterCallback<MouseCaptureOutEvent>(OnMouseCaptureOutEvent);
        }

        void OnMouseCaptureOutEvent(MouseCaptureOutEvent e)
        {
            if (m_Active)
            {
                m_Rectangle.RemoveFromHierarchy();
                m_Active = false;
            }
        }

        private void OnMouseDown(MouseDownEvent e)
        {
            if (m_Active)
            {
                e.StopImmediatePropagation();
                return;
            }

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            // SGB-549: Prevent stealing capture from a child element. This shouldn't be necessary if children
            // elements call StopPropagation when they capture the mouse, but we can't be sure of that and thus
            // we are being a bit overprotective here.
            if (target.panel?.GetCapturingElement(PointerId.mousePointerId) != null)
            {
                return;
            }

            if (CanStartManipulation(e))
            {
                if (!e.actionKey)
                {
                    graphView.ClearSelection();
                }

                graphView.Add(m_Rectangle);

                m_Rectangle.start = e.localMousePosition;
                m_Rectangle.end = m_Rectangle.start;

                m_Active = true;
                target.CaptureMouse(); // We want to receive events even when mouse is not over ourself.
                e.StopImmediatePropagation();
            }
        }

        private void OnMouseUp(MouseUpEvent e)
        {
            if (!m_Active)
                return;

            var graphView = target as GraphView;
            if (graphView == null)
                return;

            if (!CanStopManipulation(e))
                return;

            graphView.Remove(m_Rectangle);

            m_Rectangle.end = e.localMousePosition;

            var selectionRect = new Rect()
            {
                min = new Vector2(Math.Min(m_Rectangle.start.x, m_Rectangle.end.x), Math.Min(m_Rectangle.start.y, m_Rectangle.end.y)),
                max = new Vector2(Math.Max(m_Rectangle.start.x, m_Rectangle.end.x), Math.Max(m_Rectangle.start.y, m_Rectangle.end.y))
            };

            selectionRect = ComputeAxisAlignedBound(selectionRect, graphView.viewTransform.matrix.inverse);

            List<ISelectable> selection = graphView.selection;

            // If a stacknode child already exists in the selection, adding more to the selection via drag-select is not supported.
            bool hasStackChild = selection.Any(ge => (ge is GraphElement) && ((GraphElement)ge).IsStackable());
            if (!hasStackChild)
            {
                // a copy is necessary because Add To selection might cause a SendElementToFront which will change the order.
                List<ISelectable> newSelection = new List<ISelectable>();
                graphView.graphElements.ForEach(child =>
                {
                    var localSelRect = graphView.contentViewContainer.ChangeCoordinatesTo(child, selectionRect);
                    if (child.IsSelectable() && child.Overlaps(localSelRect) && !child.IsStackable()) // Exclude StackNode children
                    {
                        newSelection.Add(child);
                    }
                });

                foreach (var selectable in newSelection)
                {
                    if (selection.Contains(selectable))
                    {
                        if (e.actionKey) // invert selection on shift only
                            graphView.RemoveFromSelection(selectable);
                    }
                    else
                        graphView.AddToSelection(selectable);
                }
            }
            m_Active = false;
            target.ReleaseMouse();
            e.StopPropagation();
        }

        private void OnMouseMove(MouseMoveEvent e)
        {
            if (!m_Active)
                return;

            m_Rectangle.end = e.localMousePosition;
            e.StopPropagation();
        }

        private class RectangleSelect : ImmediateModeElement
        {
            public Vector2 start { get; set; }
            public Vector2 end { get; set; }

            protected override void ImmediateRepaint()
            {
                VisualElement t = parent;
                Vector2 screenStart = start;
                Vector2 screenEnd = end;

                // Avoid drawing useless information
                if (start == end)
                    return;

                var r = new Rect
                {
                    min = new Vector2(Math.Min(screenStart.x, screenEnd.x), Math.Min(screenStart.y, screenEnd.y)),
                    max = new Vector2(Math.Max(screenStart.x, screenEnd.x), Math.Max(screenStart.y, screenEnd.y))
                };

                var lineColor = new Color(1.0f, 0.6f, 0.0f, 1.0f);
                var segmentSize = 5f;

                Vector3[] points =
                {
                    new Vector3(r.xMin, r.yMin, 0.0f),
                    new Vector3(r.xMax, r.yMin, 0.0f),
                    new Vector3(r.xMax, r.yMax, 0.0f),
                    new Vector3(r.xMin, r.yMax, 0.0f)
                };

                DrawDottedLine(points[0], points[1], segmentSize, lineColor);
                DrawDottedLine(points[1], points[2], segmentSize, lineColor);
                DrawDottedLine(points[2], points[3], segmentSize, lineColor);
                DrawDottedLine(points[3], points[0], segmentSize, lineColor);
            }

            private void DrawDottedLine(Vector3 p1, Vector3 p2, float segmentsLength, Color col)
            {
                HandleUtility.ApplyWireMaterial();

                GL.Begin(GL.LINES);
                GL.Color(col);

                float length = Vector3.Distance(p1, p2); // ignore z component
                int count = Mathf.CeilToInt(length / segmentsLength);
                for (int i = 0; i < count; i += 2)
                {
                    GL.Vertex((Vector3.Lerp(p1, p2, i * segmentsLength / length)));
                    GL.Vertex((Vector3.Lerp(p1, p2, (i + 1) * segmentsLength / length)));
                }

                GL.End();
            }
        }
    }
}
