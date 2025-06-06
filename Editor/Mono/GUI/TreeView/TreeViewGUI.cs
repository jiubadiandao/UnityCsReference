// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor.Experimental;
using UnityEditor.StyleSheets;

namespace UnityEditor.IMGUI.Controls
{
    internal abstract class TreeViewGUI<TIdentifier> : ITreeViewGUI<TIdentifier> where TIdentifier : unmanaged, IEquatable<TIdentifier>
    {
        protected TreeViewController<TIdentifier>  m_TreeView;
        protected PingData m_Ping = new PingData();
        protected Rect m_DraggingInsertionMarkerRect;
        protected Rect m_DraggingAncestorMarkerRect;
        protected bool m_UseHorizontalScroll;

        // Icon overlay
        public float iconLeftPadding { get; set; }
        public float iconRightPadding { get; set; }
        public float iconTotalPadding { get { return iconLeftPadding + iconRightPadding; } }
        public System.Action<TreeViewItem<TIdentifier> , Rect> iconOverlayGUI { get; set; } // Rect includes iconLeftPadding and iconRightPadding
        public System.Action<TreeViewItem<TIdentifier> , Rect> labelOverlayGUI { get; set; }

        private bool m_AnimateScrollBarOnExpandCollapse = true;

        // Layout
        private float m_LineHeight = -1;
        public float k_LineHeight
        {
            get
            {
                if (m_LineHeight < 0)
                    m_LineHeight = new SVC<float>("--treeview-line-height", 16f);

                return m_LineHeight;
            }
            set { m_LineHeight = value; }
        }
        public float k_BaseIndent = 2f;
        public float k_IndentWidth = 14f;
        public float k_IconWidth = 16f;
        public float k_SpaceBetweenIconAndText = 2f;
        public float k_TopRowMargin = 0f;
        public float k_BottomRowMargin = 0f;
        public float indentWidth { get { return k_IndentWidth + iconTotalPadding; } }
        public float k_HalfDropBetweenHeight = 4f;
        public float customFoldoutYOffset = 0f;
        public float extraInsertionMarkerIndent = 0f;
        public float extraSpaceBeforeIconAndLabel { get; set; }
        public bool drawSelection { get; set; } = true;
        public float halfDropBetweenHeight { get { return k_HalfDropBetweenHeight; } }

        public virtual float topRowMargin { get { return k_TopRowMargin; } }
        public virtual float bottomRowMargin { get { return k_BottomRowMargin; } }

        // Styles
        internal static class Styles
        {
            public static GUIStyle foldout = "IN Foldout";
            public static GUIStyle insertion = "TV Insertion";
            public static GUIStyle insertionRelativeToSibling = "TV InsertionRelativeToSibling";
            public static GUIStyle ping = "TV Ping";
            public static GUIStyle toolbarButton = "ToolbarButton";
            public static GUIStyle lineStyle = "TV Line";
            public static GUIStyle lineBoldStyle = "TV LineBold";
            public static GUIStyle selectionStyle = "TV Selection";
            public static GUIContent content = new GUIContent(EditorGUIUtility.FindTexture(EditorResources.folderIconName));
        }

        private GUIStyle m_FoldoutStyle;
        protected GUIStyle foldoutStyle
        {
            get
            {
                return m_FoldoutStyle ?? Styles.foldout;
            }
            set { m_FoldoutStyle = value; }
        }

        private GUIStyle m_InsertionStyle;
        protected GUIStyle insertionStyle
        {
            get
            {
                return m_InsertionStyle ?? Styles.insertion;
            }
            set { m_InsertionStyle = value; }
        }

        private GUIStyle m_InsertionRelativeToSiblingStyle;
        protected GUIStyle insertionRelativeToSiblingStyle
        {
            get
            {
                return m_InsertionRelativeToSiblingStyle ?? Styles.insertionRelativeToSibling;
            }
            set { m_InsertionRelativeToSiblingStyle = value; }
        }

        private GUIStyle m_PingStyle;
        protected GUIStyle pingStyle
        {
            get
            {
                return m_PingStyle ?? Styles.ping;
            }
            set { m_PingStyle = value; }
        }
        private GUIStyle m_ToolbarButtonStyle;
        protected GUIStyle toolbarButtonStyle
        {
            get
            {
                return m_ToolbarButtonStyle ?? Styles.toolbarButton;
            }
            set { m_ToolbarButtonStyle = value; }
        }
        private GUIStyle m_LineStyle;
        protected GUIStyle lineStyle
        {
            get
            {
                return m_LineStyle ?? Styles.lineStyle;
            }
            set { m_LineStyle = value; }
        }
        private GUIStyle m_SelectionStyle;
        protected GUIStyle selectionStyle
        {
            get
            {
                return m_SelectionStyle ?? Styles.selectionStyle;
            }
            set { m_SelectionStyle = value; }
        }

        private GUIStyle renameStyle
        {
            get
            {
                GUIStyle renameStyle = new GUIStyle("PR TextField");
                renameStyle.fontSize = lineStyle.fontSize; // the textField should have the same text size as the line

                return renameStyle;
            }
        }
        protected float foldoutStyleWidth
        {
            get { return foldoutStyle.fixedWidth; }
        }

        public TreeViewGUI(TreeViewController<TIdentifier>  treeView)
        {
            m_TreeView = treeView;
        }

        public TreeViewGUI(TreeViewController<TIdentifier>  treeView, bool useHorizontalScroll)
        {
            m_TreeView = treeView;
            m_UseHorizontalScroll = useHorizontalScroll;
        }

        virtual public void OnInitialize()
        {
            var dragging = m_TreeView.dragging as TreeViewDragging<TIdentifier> ;
            if (dragging != null)
                dragging.getIndentLevelForMouseCursor = GetIndentLevelForMouseCursor;
        }

        int GetIndentLevelForMouseCursor()
        {
            float contentStartX = k_BaseIndent + extraInsertionMarkerIndent + foldoutStyleWidth + lineStyle.margin.left;
            float mousePosX = Event.current.mousePosition.x;
            return Mathf.FloorToInt((mousePosX - contentStartX) / indentWidth);
        }

        internal Texture GetEffectiveIcon(TreeViewItem<TIdentifier>  item, bool selected, bool focused)
        {
            var icon = GetIconForItem(item);

            if (selected && focused)
            {
                var selIcon = GetIconForSelectedItem(item);

                if (selIcon != null)
                    return selIcon;
            }

            return icon;
        }

        protected virtual Texture GetIconForItem(TreeViewItem<TIdentifier>  item) => GetIconForItemInternal(item);
        protected virtual Texture GetIconForItemInternal(TreeViewItem<TIdentifier>  item)
        {
            return item.icon;
        }

        internal virtual Texture GetIconForSelectedItem(TreeViewItem<TIdentifier>  item)
        {
            return EditorUtility.GetIconInActiveState(GetIconForItem(item));
        }

        // ------------------
        // Size section

        // Calc correct width if horizontal scrollbar is wanted return new Vector2(1, height)
        virtual public Vector2 GetTotalSize()
        {
            // Width is 1 to prevent showing horizontal scrollbar
            float width = 1f;
            if (m_UseHorizontalScroll)
            {
                var rows = m_TreeView.data.GetRows();
                width = GetMaxWidth(rows);
            }

            // Height
            float height = m_TreeView.data.rowCount * k_LineHeight + topRowMargin + bottomRowMargin;


            if (m_AnimateScrollBarOnExpandCollapse && m_TreeView.expansionAnimator.isAnimating)
            {
                height -= m_TreeView.expansionAnimator.deltaHeight;
            }

            return new Vector2(width, height);
        }

        protected float GetMaxWidth(IList<TreeViewItem<TIdentifier>> rows)
        {
            float maxWidth = 1f;

            foreach (TreeViewItem<TIdentifier>  item in rows)
            {
                float width = 0f;

                width += GetContentIndent(item);

                if (item.icon != null)
                    width += k_IconWidth;

                float minNameWidth, maxNameWidth;
                lineStyle.CalcMinMaxWidth(GUIContent.Temp(item.displayName), out minNameWidth, out maxNameWidth);
                width += maxNameWidth;

                // Add some padding to the back
                width += k_BaseIndent;

                if (width > maxWidth)
                    maxWidth = width;
            }

            return maxWidth;
        }

        virtual public int GetNumRowsOnPageUpDown(TreeViewItem<TIdentifier>  fromItem, bool pageUp, float heightOfTreeView)
        {
            return (int)Mathf.Floor(heightOfTreeView / k_LineHeight);
        }

        // Should return the row index of the first and last row thats fits in the pixel rect defined by top and height
        virtual public void GetFirstAndLastRowVisible(out int firstRowVisible, out int lastRowVisible)
        {
            if (m_TreeView.data.rowCount == 0 || Mathf.Approximately(m_TreeView.visibleRect.height, 0.0f))
            {
                firstRowVisible = lastRowVisible = -1;
                return;
            }

            float topPixel = m_TreeView.state.scrollPos.y;
            float heightInPixels = m_TreeView.visibleRect.height;
            firstRowVisible = (int)Mathf.Floor((topPixel - topRowMargin) / k_LineHeight);
            lastRowVisible = firstRowVisible + (int)Mathf.Ceil(heightInPixels / k_LineHeight);

            firstRowVisible = Mathf.Max(firstRowVisible, 0);
            lastRowVisible = Mathf.Min(lastRowVisible, m_TreeView.data.rowCount - 1);

            // Validate
            if (firstRowVisible >= m_TreeView.data.rowCount && firstRowVisible > 0)
            {
                // Reset scroll if it was invalid, this can be the case if scroll y value was serialized and loading new tree data
                m_TreeView.state.scrollPos.y = 0f;
                GetFirstAndLastRowVisible(out firstRowVisible, out lastRowVisible);
            }
        }

        // ---------------------
        // OnGUI section

        virtual public void BeginRowGUI()
        {
            // Reset
            m_DraggingInsertionMarkerRect.x = -1;
            m_DraggingAncestorMarkerRect.x = -1;

            SyncFakeItem(); // After domain reload we ensure to reconstruct new Item state

            // Input for rename overlay (repainted in EndRowGUI to ensure rendered on top)
            if (Event.current.type != EventType.Repaint)
                DoRenameOverlay();
        }

        void DrawDraggingInsertionMarkerIfNeeded()
        {
            if (Event.current.type != EventType.Repaint)
                return;

            // If an inheriting class already set the m_DraggingInsertionMarkerRect we don't overwrite it
            var dragging = m_TreeView.dragging as TreeViewDragging<TIdentifier>;
            if (dragging != null && dragging.insertionMarkerYPosition > 0 && m_DraggingInsertionMarkerRect.x == -1)
            {
                float xPos = GetContentIndent(dragging.insertRelativeToSibling) + extraInsertionMarkerIndent;
                float yPos = dragging.insertionMarkerYPosition - insertionStyle.fixedHeight * 0.5f;
                m_DraggingInsertionMarkerRect = new Rect(xPos, yPos, GUIClip.visibleRect.width - xPos, insertionStyle.fixedHeight);
            }

            // Draw row marker when dragging
            if (m_DraggingInsertionMarkerRect.x >= 0)
            {
                insertionStyle.Draw(m_DraggingInsertionMarkerRect, false, false, false, false);
            }

            if (m_DraggingAncestorMarkerRect.x >= 0)
            {
                insertionRelativeToSiblingStyle.Draw(m_DraggingAncestorMarkerRect, GUIContent.none, false, false, false, false);
            }
        }

        virtual public void EndRowGUI()
        {
            DrawDraggingInsertionMarkerIfNeeded();

            // Render rename overlay last (input is handled in BeginRowGUI)
            if (Event.current.type == EventType.Repaint)
                DoRenameOverlay();

            // Ping a Item
            HandlePing();
        }

        virtual public void OnRowGUI(Rect rowRect, TreeViewItem<TIdentifier>  item, int row, bool selected, bool focused) => OnRowGUIInternal(rowRect, item, row, selected, focused);
        virtual public void OnRowGUIInternal(Rect rowRect, TreeViewItem<TIdentifier>  item, int row, bool selected, bool focused)
        {
            DoItemGUI(rowRect, row, item, selected, focused, false);
        }

        // Override this method for custom rendering of background behind selection and drop effect rendering
        protected virtual void DrawItemBackground(Rect rect, int row, TreeViewItem<TIdentifier>  item, bool selected, bool focused)
        {
            if (item == m_TreeView.hoveredItem)
            {
                using (new GUI.BackgroundColorScope(GameObjectTreeViewGUI.GameObjectStyles.hoveredBackgroundColor))
                {
                    GUI.Label(rect, GUIContent.none, GameObjectTreeViewGUI.GameObjectStyles.hoveredItemBackgroundStyle);
                }
            }
        }

        public virtual Rect GetRenameRect(Rect rowRect, int row, TreeViewItem<TIdentifier>  item)
        {
            float offset = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;

            if (GetIconForItem(item) != null)
                offset += k_SpaceBetweenIconAndText + k_IconWidth + iconTotalPadding;

            // By default we top align the rename rect to follow the label style, foldout and controls alignment
            return new Rect(rowRect.x + offset, rowRect.y, rowRect.width - offset,
                rowRect.height);
        }

        virtual protected void DoItemGUI(Rect rect, int row, TreeViewItem<TIdentifier>  item, bool selected, bool focused, bool useBoldFont)
        {
            EditorGUIUtility.SetIconSize(new Vector2(k_IconWidth, k_IconWidth)); // If not set we see icons scaling down if text is being cropped

            int itemControlID = TreeViewController<TIdentifier>.GetItemControlID(item);

            bool isRenamingThisItem = IsRenaming(item.id);
            bool showFoldout = m_TreeView.data.IsExpandable(item);

            // Adjust edit field if needed (on repaint since on layout rect.width is invalid when using GUILayout)
            if (isRenamingThisItem && Event.current.type == EventType.Repaint)
            {
                GetRenameOverlay().editFieldRect = GetRenameRect(rect, row, item);
            }

            string label = item.displayName;
            if (isRenamingThisItem)
            {
                selected = false;
                label = "";
            }

            if (Event.current.type == EventType.Repaint)
            {
                // Draw background (can be overridden)
                DrawItemBackground(rect, row, item, selected, focused);

                // Draw selection
                if (selected && drawSelection)
                    selectionStyle.Draw(rect, false, false, true, focused);

                bool hasDragHandling = m_TreeView.dragging != null;
                if (hasDragHandling)
                {
                    // Draw drop marker
                    if (m_TreeView.dragging.GetDropTargetControlID() == itemControlID && m_TreeView.data.CanBeParent(item))
                    {
                        Styles.lineStyle.Draw(GetDropTargetRect(rect), GUIContent.none, true, true, false, false);
                    }

                    // Ancestor item marker is rendered after all rows in RowEndGUI - extra visual helper marker when previous sibling is far away from the cursor
                    var dragging = m_TreeView.dragging as TreeViewDragging<TIdentifier>;
                    if (dragging != null && dragging.GetAncestorControlID() == itemControlID && dragging.insertRelativeToSibling != null)
                    {
                        m_DraggingAncestorMarkerRect = rect;
                        m_DraggingAncestorMarkerRect.xMin += extraInsertionMarkerIndent + GetContentIndent(item);
                        m_DraggingAncestorMarkerRect.y = rect.yMax - insertionRelativeToSiblingStyle.fixedHeight * 0.5f;
                    }
                }
            }

            // Do additional ui controls (menu button, prefab arrow etc)
            OnAdditionalGUI(rect, row, item, selected, focused);

            // Do row content (icon, label, controls etc)
            OnContentGUI(rect, row, item, label, selected, focused, useBoldFont, false);

            // Do foldout
            if (showFoldout)
            {
                DoFoldout(rect, item, row);
            }

            EditorGUIUtility.SetIconSize(Vector2.zero);
        }

        protected virtual Rect GetDropTargetRect(Rect rect)
        {
            return rect;
        }

        float GetTopPixelOfRow(int row)
        {
            return row * k_LineHeight + topRowMargin;
        }

        public virtual Rect GetRowRect(int row, float rowWidth)
        {
            return new Rect(0, GetTopPixelOfRow(row), rowWidth, k_LineHeight);
        }

        public virtual Rect GetRectForFraming(int row)
        {
            return GetRowRect(row, 1); // We ignore width by default when framing (only y scroll is affected)
        }

        float GetFoldoutYPosition(float rectY)
        {
            // By default the arrow is aligned to the top to match text rendering
            return rectY + customFoldoutYOffset;
        }

        protected virtual Rect DoFoldout(Rect rect, TreeViewItem<TIdentifier>  item, int row)
        {
            float indent = GetFoldoutIndent(item);
            Rect foldoutRect = new Rect(rect.x + indent, GetFoldoutYPosition(rect.y), foldoutStyleWidth, k_LineHeight);
            FoldoutButton(foldoutRect, item, row, foldoutStyle);
            return foldoutRect;
        }

        protected virtual void FoldoutButton(Rect foldoutRect, TreeViewItem<TIdentifier>  item, int row, GUIStyle foldoutStyle)
        {
            var expansionAnimator = m_TreeView.expansionAnimator;

            bool newExpandedValue;
            EditorGUI.BeginChangeCheck();
            {
                bool expandedState = expansionAnimator.IsAnimating(item.id) ? expansionAnimator.isExpanding : m_TreeView.data.IsExpanded(item);
                newExpandedValue = DoFoldoutButton(foldoutRect, expandedState, foldoutStyle);
            }
            if (EditorGUI.EndChangeCheck())
            {
                m_TreeView.UserInputChangedExpandedState(item, row, newExpandedValue);
            }
        }

        protected virtual bool DoFoldoutButton(Rect foldoutRect, bool expandedState, GUIStyle foldoutStyle)
        {
            return GUI.Toggle(foldoutRect, expandedState, GUIContent.none, foldoutStyle);
        }

        protected virtual void OnAdditionalGUI(Rect rect, int row, TreeViewItem<TIdentifier>  item, bool selected, bool focused)
        {
        }

        protected virtual void OnContentGUI(Rect rect, int row, TreeViewItem<TIdentifier>  item, string label, bool selected, bool focused, bool useBoldFont, bool isPinging)
        {
            if (Event.current.rawType != EventType.Repaint)
                return;

            if (!isPinging)
            {
                // The rect is assumed indented and sized after the content when pinging
                float indent = GetContentIndent(item) + extraSpaceBeforeIconAndLabel;
                rect.xMin += indent;
            }

            lineStyle = useBoldFont ? Styles.lineBoldStyle : Styles.lineStyle;

            // Draw icon
            Rect iconRect = rect;
            iconRect.width = k_IconWidth;
            iconRect.x += iconLeftPadding;

            Texture icon = GetEffectiveIcon(item, selected, focused);
            if (icon != null)
            {
                var iconColor = GUI.color.AlphaMultiplied(GUI.enabled ? 1f : 0.5f);
                GUI.DrawTexture(iconRect, icon, ScaleMode.ScaleToFit, true, 0, iconColor, 0, 0);
            }

            if (iconOverlayGUI != null)
            {
                Rect iconOverlayRect = rect;
                iconOverlayRect.width = k_IconWidth + iconTotalPadding;
                iconOverlayGUI(item, iconOverlayRect);
            }

            // Draw text
            if (icon != null)
                rect.xMin += k_IconWidth + iconTotalPadding + k_SpaceBetweenIconAndText;
            lineStyle.Draw(rect, label, false, false, selected, focused);

            if (labelOverlayGUI != null)
            {
                labelOverlayGUI(item, rect);
            }
        }

        // Ping Item
        // -------------

        virtual public void BeginPingItem(TreeViewItem<TIdentifier>  item, float topPixelOfRow, float availableWidth)
        {
            if (item == null)
                return;

            // Setup ping
            if (topPixelOfRow >= 0f)
            {
                m_Ping.isPinging = true;
                m_Ping.m_PingStyle = pingStyle;

                GUIContent cont = GUIContent.Temp(item.displayName);
                Vector2 contentSize = m_Ping.m_PingStyle.CalcSize(cont);

                m_Ping.m_ContentRect = new Rect(GetContentIndent(item) + extraSpaceBeforeIconAndLabel,
                    topPixelOfRow,
                    k_IconWidth + k_SpaceBetweenIconAndText + contentSize.x + iconTotalPadding,
                    contentSize.y);

                m_Ping.m_AvailableWidth = availableWidth;

                int row = m_TreeView.data.GetRow(item.id);

                bool useBoldFont = item.displayName.Equals("Assets");
                m_Ping.m_ContentDraw = (Rect r) =>
                {
                    // get Item parameters from closure
                    OnContentGUI(r, row, item, item.displayName, false, false, useBoldFont, true);
                };

                m_TreeView.Repaint();
            }
        }

        virtual public void EndPingItem()
        {
            m_Ping.isPinging = false;
        }

        void HandlePing()
        {
            m_Ping.HandlePing();

            if (m_Ping.isPinging)
                m_TreeView.Repaint();
        }

        //-------------------
        // Rename section

        protected RenameOverlay<TIdentifier> GetRenameOverlay()
        {
            return m_TreeView.state.renameOverlay;
        }

        virtual protected bool IsRenaming(TIdentifier id)
        {
            return GetRenameOverlay().IsRenaming() && GetRenameOverlay().userData.Equals(id) && !GetRenameOverlay().isWaitingForDelay;
        }

        virtual public bool BeginRename(TreeViewItem<TIdentifier>  item, float delay) => BeginRenameInternal(item, delay);
        virtual public bool BeginRenameInternal(TreeViewItem<TIdentifier>  item, float delay)
        {
            return GetRenameOverlay().BeginRename(item.displayName, item.id, delay);
        }

        virtual public void EndRename()
        {
            // We give keyboard focus back to our tree view because the rename utility stole it (now we give it back)
            if (GetRenameOverlay().HasKeyboardFocus())
                m_TreeView.GrabKeyboardFocus();

            RenameEnded();
            ClearRenameAndNewItemState(); // Ensure clearing if RenameEnden is overrided
        }

        virtual protected void RenameEnded() {}

        virtual public void DoRenameOverlay()
        {
            if (GetRenameOverlay().IsRenaming())
                if (!GetRenameOverlay().OnGUI(renameStyle))
                    EndRename();
        }

        virtual protected void SyncFakeItem() {}


        virtual protected void ClearRenameAndNewItemState()
        {
            m_TreeView.data.RemoveFakeItem();
            GetRenameOverlay().Clear();
        }

        virtual public float GetFoldoutIndent(TreeViewItem<TIdentifier>  item)
        {
            // Ignore depth when showing search results
            if (m_TreeView.isSearching)
                return k_BaseIndent;

            return k_BaseIndent + item.depth * indentWidth;
        }

        virtual public float GetContentIndent(TreeViewItem<TIdentifier>  item)
        {
            return GetFoldoutIndent(item) + foldoutStyleWidth + lineStyle.margin.left;
        }
    }
}
