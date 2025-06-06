// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System.Collections.Generic;
using System.Linq;


namespace UnityEditor.IMGUI.Controls
{
    // LazyTreeViewDataSource assumes that the Item tree only contains visible items, optimal for large data sets.
    // Usage:
    //    - Override FetchData () and build the tree with visible items with m_RootItem as root  (and and populate the m_VisibleRows List)
    //    - FetchData () is called every time the expanded state changes.
    //    - Configure showRootItem and rootIsCollapsable as wanted
    //
    // Note: if dealing with small trees consider using TreeViewDataSource instead: it assumes that the tree contains all items.

    internal abstract class LazyTreeViewDataSource<TIdentifier> : TreeViewDataSource<TIdentifier> where TIdentifier : unmanaged, System.IEquatable<TIdentifier>
    {
        static readonly List<TreeViewItem<TIdentifier>> s_ChildListForCollapsedParent = new List<TreeViewItem<TIdentifier>>();

        public LazyTreeViewDataSource(TreeViewController<TIdentifier> treeView)
            : base(treeView)
        {
        }

        public static List<TreeViewItem<TIdentifier>> CreateChildListForCollapsedParent()
        {
            // To mark a collapsed parent we use a list with one element that is null.
            // The null element in the children list ensures we show the collapse arrow.
            // Reuse read-only list to prevent allocations.
            if (s_ChildListForCollapsedParent.Count != 1 || s_ChildListForCollapsedParent[0] != null)
            {
                s_ChildListForCollapsedParent.Clear();
                s_ChildListForCollapsedParent.Add(null);
            }
            return s_ChildListForCollapsedParent;
        }

        public static bool IsChildListForACollapsedParent(IList<TreeViewItem<TIdentifier>> childList)
        {
            return (childList != null && childList.Count == 1 && childList[0] == null); // see CreateChildListForCollapsedParent
        }

        // Return all ancestor items of the Item with 'id'
        protected abstract void GetParentsAbove(TIdentifier id, HashSet<TIdentifier> parentsAbove);

        // Return all descendant items that have children from the Item with 'id'
        protected abstract void GetParentsBelow(TIdentifier id, HashSet<TIdentifier> parentsBelow);

        override public void RevealItem(TIdentifier itemID)
        {
            // Get existing expanded in hashset
            HashSet<TIdentifier> expandedSet = new HashSet<TIdentifier>(expandedIDs);
            int orgSize = expandedSet.Count;

            // Add all parents above id
            GetParentsAbove(itemID, expandedSet);

            if (orgSize != expandedSet.Count)
            {
                // Bulk set expanded ids (is sorted in SetExpandedIDs)
                SetExpandedIDs(expandedSet.ToArray());

                // Refresh immediately if any Item was expanded
                if (m_NeedRefreshRows)
                    FetchData();
            }
        }

        override public void RevealItems(TIdentifier[] itemIDs)
        {
            // Get existing expanded in hashset
            HashSet<TIdentifier> expandedSet = new HashSet<TIdentifier>(expandedIDs);
            int orgSize = expandedSet.Count;

            foreach (var itemID in itemIDs)
                // Add all parents above id
                GetParentsAbove(itemID, expandedSet);

            if (orgSize != expandedSet.Count)
            {
                // Bulk set expanded ids (is sorted in SetExpandedIDs)
                SetExpandedIDs(expandedSet.ToArray());

                // Refresh immediately if any Item was expanded
                if (m_NeedRefreshRows)
                    FetchData();
            }
        }

        override public TreeViewItem<TIdentifier> FindItem(TIdentifier itemID)
        {
            // Since this is a LazyTreeViewDataSource that only knows about expanded items
            // we need to reveal the item before searching for it (expand its ancestors)
            RevealItem(itemID);

            // Now find the item after we have expanded and created parent items
            return base.FindItem(itemID);
        }

        override public void SetExpandedWithChildren(TreeViewItem<TIdentifier> item, bool expand)
        {
            SetExpandedWithChildren(item.id, expand);
        }

        // Override for special handling of recursion
        // We cannot recurse normally to tree Item children because we have not loaded children of collapsed items
        // therefore let client implement GetParentsBelow to fetch ids instead
        override public void SetExpandedWithChildren(TIdentifier id, bool expand)
        {
            // Get existing expanded in hashset
            HashSet<TIdentifier> oldExpandedSet = new HashSet<TIdentifier>(expandedIDs);

            // Add all children expanded ids to hashset
            HashSet<TIdentifier> candidates = new HashSet<TIdentifier>();
            GetParentsBelow(id, candidates);

            if (expand)     oldExpandedSet.UnionWith(candidates);
            else            oldExpandedSet.ExceptWith(candidates);

            // Bulk set expanded ids (is sorted in SetExpandedIDs)
            SetExpandedIDs(oldExpandedSet.ToArray());

            // Keep for debugging
            // Debug.Log ("New expanded state (bulk): " + DebugUtils.ListToString(new List<int>(expandedIDs)));
        }

        public override void InitIfNeeded()
        {
            // Cached for large trees...
            if (m_Rows == null || m_NeedRefreshRows)
            {
                FetchData(); // Only need to fetch visible data..

                m_NeedRefreshRows = false;

                if (onVisibleRowsChanged != null)
                    onVisibleRowsChanged();

                m_TreeView.Repaint();
            }
        }

        // Get the flattened tree of visible items. Use GetFirstAndLastRowVisible to cull invisible items
        override public IList<TreeViewItem<TIdentifier>> GetRows()
        {
            InitIfNeeded();
            return m_Rows;
        }
    }
}
