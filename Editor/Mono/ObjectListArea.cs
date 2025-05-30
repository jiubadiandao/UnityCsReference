// Unity C# reference source
// Copyright (c) Unity Technologies. For terms of use, see
// https://unity3d.com/legal/licenses/Unity_Reference_Only_License

using System;
using UnityEngine;
using UnityEditor.VersionControl;
using System.Collections.Generic;
using System.Linq;
using UnityEditorInternal;
using AssetReference = UnityEditorInternal.InternalEditorUtility.AssetReference;
using Object = UnityEngine.Object;
using RenameOverlay = UnityEditor.RenameOverlay<int>;

namespace UnityEditor
{
    [System.Serializable]
    internal class ObjectListAreaState
    {
        // Selection state
        public List<int> m_SelectedInstanceIDs = new List<int>();
        public int m_LastClickedInstanceID;     // Used for navigation
        public bool m_HadKeyboardFocusLastEvent; // Needs to survive domain reloads to prevent setting selection on got keyboard focus

        // Expanded instanceIDs
        public List<int> m_ExpandedInstanceIDs = new List<int>();

        // Rename state
        public RenameOverlay m_RenameOverlay = new RenameOverlay();

        // Create new asset state
        public CreateAssetUtility m_CreateAssetUtility = new CreateAssetUtility();
        public int m_NewAssetIndexInList = -1;

        // Misc state
        public Vector2 m_ScrollPosition;
        public int m_GridSize = 64;

        public void OnAwake()
        {
            // Clear state that should not survive closing/starting Unity
            m_NewAssetIndexInList = -1;
            m_RenameOverlay.Clear();
            m_CreateAssetUtility = new CreateAssetUtility();
        }
    }


    internal partial class ObjectListArea
    {
        static class Styles
        {
            public static readonly GUIStyle resultsLabel = new GUIStyle("OL ResultLabel");
            public static readonly GUIStyle resultsGridLabel = new GUIStyle("ProjectBrowserGridLabel");
            public static readonly GUIStyle resultsGrid = new GUIStyle("ObjectPickerResultsGrid");
            public static readonly GUIStyle groupHeaderMiddle = new GUIStyle("ProjectBrowserHeaderBgMiddle");
            public static readonly GUIStyle groupHeaderTop = new GUIStyle("ProjectBrowserHeaderBgTop");
            public static readonly GUIStyle groupHeaderLabel = new GUIStyle("Label");
            public static readonly GUIStyle groupHeaderLabelCount = new GUIStyle("MiniLabel");
            public static readonly GUIStyle groupFoldout = new GUIStyle("IN Foldout");
            public static readonly GUIStyle miniRenameField = new GUIStyle("OL MiniRenameField");
            public static readonly GUIStyle ping = new GUIStyle("OL Ping");
            public static readonly GUIStyle miniPing = new GUIStyle("OL MiniPing");
            public static readonly GUIStyle iconDropShadow = new GUIStyle("ProjectBrowserIconDropShadow");
            public static readonly GUIStyle textureIconDropShadow = new GUIStyle("ProjectBrowserTextureIconDropShadow");
            public static readonly GUIStyle iconAreaBg = new GUIStyle("ProjectBrowserIconAreaBg");
            public static readonly GUIStyle previewBg = new GUIStyle("ProjectBrowserPreviewBg");
            public static readonly GUIStyle subAssetBg = new GUIStyle("ProjectBrowserSubAssetBg");
            public static readonly GUIStyle subAssetBgOpenEnded = new GUIStyle("ProjectBrowserSubAssetBgOpenEnded");
            public static readonly GUIStyle subAssetBgCloseEnded = new GUIStyle("ProjectBrowserSubAssetBgCloseEnded");
            public static readonly GUIStyle subAssetBgMiddle = new GUIStyle("ProjectBrowserSubAssetBgMiddle");
            public static readonly GUIStyle subAssetExpandButton = new GUIStyle("ProjectBrowserSubAssetExpandBtn");
            public static readonly GUIStyle subAssetExpandButtonMedium = new GUIStyle("ProjectBrowserSubAssetExpandBtnMedium");
            public static readonly GUIStyle subAssetExpandButtonSmall = new GUIStyle("ProjectBrowserSubAssetExpandBtnSmall");

            public static readonly GUIContent maxmumItemCountInfo = EditorGUIUtility.TrTextContent("NOTE: Maximum item count is reached, not all items are shown. Narrow your search.");
        }

        // State persisted across assembly reloads
        ObjectListAreaState m_State;

        // Key navigation
        const int kHome = int.MinValue;
        const int kEnd = int.MaxValue;
        const int kPageDown = int.MaxValue - 1;
        const int kPageUp = int.MinValue + 1;
        int m_SelectionOffset = 0;

        const float k_ListModeVersionControlOverlayPadding = 14f;
        static bool s_VCEnabled = false;

        PingData m_Ping = new PingData();

        EditorWindow m_Owner;

        public bool allowDragging { get; set; }
        public bool allowRenaming { get; set; }
        public bool allowMultiSelect { get; set; }
        public bool allowDeselection { get; set; }
        public bool allowFocusRendering { get; set; }
        public bool allowBuiltinResources { get; set; }
        public bool allowUserRenderingHook { get; set; }
        public bool allowFindNextShortcut { get; set; }
        public bool foldersFirst { get; set; }
        int m_KeyboardControlID;

        bool m_AllowRenameOnMouseUp = true;


        Vector2 m_LastScrollPosition = new Vector2(0, 0);
        double LastScrollTime = 0;

        internal Texture m_SelectedObjectIcon = null;

        protected LocalGroup m_LocalAssets;

        // List of all available groups
        List<Group> m_Groups;

        // Layout
        Rect m_TotalRect;
        Rect m_VisibleRect;
        const int kListLineHeight = 16;
        private int m_pingIndex;

        int m_MinIconSize = 32;
        int m_MinGridSize = 16;
        int m_MaxGridSize = 96;
        bool m_AllowThumbnails = true;
        const int kSpaceForScrollBar = 13;
        int m_LeftPaddingForPinging = 0;
        bool m_FrameLastClickedItem = false;

        bool m_ShowLocalAssetsOnly = true;

        double m_NextDirtyCheck = 0;

        readonly SearchService.ProjectSearchSessionHandler m_SearchSessionHandler = new SearchService.ProjectSearchSessionHandler();

        // Callbacks
        System.Action m_RepaintWantedCallback;
        System.Action<bool> m_ItemSelectedCallback;
        System.Action m_KeyboardInputCallback;
        System.Action m_GotKeyboardFocus;
        System.Func<Rect, float> m_DrawLocalAssetHeader;

        public System.Action repaintCallback                { get {return m_RepaintWantedCallback; } set {m_RepaintWantedCallback = value; }}
        public System.Action<bool> itemSelectedCallback     { get {return m_ItemSelectedCallback; }  set {m_ItemSelectedCallback = value; }}
        public System.Action keyboardCallback               { get {return m_KeyboardInputCallback; } set {m_KeyboardInputCallback = value; }}
        public System.Action gotKeyboardFocus               { get {return m_GotKeyboardFocus; }      set {m_GotKeyboardFocus = value; }}
        public System.Func<Rect, float> drawLocalAssetHeader { get {return m_DrawLocalAssetHeader; }  set {m_DrawLocalAssetHeader = value; }}

        // Debug
        static internal bool s_Debug = false;

        public ObjectListArea(ObjectListAreaState state, EditorWindow owner, bool showNoneItem)
        {
            m_State = state;
            m_Owner = owner;

            m_LocalAssets = new LocalGroup(this, "", showNoneItem);

            m_Groups = new List<Group>();
            m_Groups.Add(m_LocalAssets);
        }

        public void ShowObjectsInList(int[] instanceIDs)
        {
            // Clear search, etc.
            Init(m_TotalRect, HierarchyType.Assets, new SearchFilter(), false);

            // Set list manually
            m_LocalAssets.ShowObjectsInList(instanceIDs);
        }

        internal void ShowObjectsInList(int[] instanceIDs, string[] rootPaths)
        {
            // Clear search, etc.
            Init(m_TotalRect, HierarchyType.Assets, new SearchFilter(), false);

            // Set list manually
            m_LocalAssets.ShowObjectsInList(instanceIDs, rootPaths);
        }

        // This method is being used by the EditorTests/Searching tests
        public string[] GetCurrentVisibleNames()
        {
            var list = m_LocalAssets.GetVisibleNameAndInstanceIDs();
            return list.Select(x => x.Key).ToArray();
        }

        public void Init(Rect rect, HierarchyType hierarchyType, SearchFilter searchFilter, bool checkThumbnails)
        {
            Init(rect, hierarchyType, searchFilter, checkThumbnails, SearchService.SearchSessionOptions.Default);
        }

        public void Init(Rect rect, HierarchyType hierarchyType, SearchFilter searchFilter, bool checkThumbnails, SearchService.SearchSessionOptions searchSessionOptions)
        {
            // Keep for debugging
            //Debug.Log ("Init ObjectListArea: " + searchFilter);

            m_TotalRect = m_VisibleRect = rect;

            m_LocalAssets.UpdateFilter(hierarchyType, searchFilter, foldersFirst, searchSessionOptions);
            m_LocalAssets.UpdateAssets();

            if (checkThumbnails)
                m_AllowThumbnails = ObjectsHaveThumbnails(hierarchyType, searchFilter, searchSessionOptions);
            else
                m_AllowThumbnails = true;

            Repaint();

            // Prepare data
            SetupData(true);
        }

        internal void InitForSearch(Rect rect, HierarchyType hierarchyType, SearchFilter searchFilter, bool checkThumbnails, Func<string, int> assetToInstanceId)
        {
            InitForSearch(rect, hierarchyType, searchFilter, checkThumbnails, assetToInstanceId, SearchService.SearchSessionOptions.Default);
        }

        internal void InitForSearch(Rect rect, HierarchyType hierarchyType, SearchFilter searchFilter, bool checkThumbnails, Func<string, int> assetToInstanceId, SearchService.SearchSessionOptions searchSessionOptions)
        {
            var searchQuery = searchFilter.originalText;
            if (string.IsNullOrEmpty(searchQuery))
                searchQuery = searchFilter.FilterToSearchFieldString();

            // Override Asset search here. For GameObjects, it is done in CachedFilteredHierarchy.cs
            if (hierarchyType == HierarchyType.GameObjects)
            {
                Init(rect, hierarchyType, searchFilter, checkThumbnails, searchSessionOptions);
                return;
            }

            var allResults = new List<string>();
            if (searchFilter.IsSearching())
            {
                m_SearchSessionHandler.BeginSession(() =>
                {
                    return new SearchService.ProjectSearchContext
                    {
                        requiredTypeNames = searchFilter.classNames,
                        requiredTypes = searchFilter.classNames.Select(name =>
                            TypeCache.GetTypesDerivedFrom<Object>()
                                .FirstOrDefault(t => name == t.FullName || name == t.Name)),
                        searchFilter = searchFilter
                    };
                }, searchSessionOptions);
                m_SearchSessionHandler.BeginSearch(searchQuery);
                // Asynchronous searches return new results. Accumulate those results when using ShowObjectsInList.
                var results = m_SearchSessionHandler.Search(searchQuery, newResults =>
                {
                    if (newResults == null || !searchFilter.IsSearching())
                        return;
                    allResults.AddRange(newResults);
                    InitListAreaWithItems(rect, hierarchyType, searchFilter, checkThumbnails, allResults, assetToInstanceId, searchSessionOptions);
                });
                InitListAreaWithItems(rect, hierarchyType, searchFilter, checkThumbnails, results, assetToInstanceId, searchSessionOptions);
                if (results != null)
                    allResults.AddRange(results);
                m_SearchSessionHandler.EndSearch();
            }
            else
            {
                m_SearchSessionHandler.EndSession();
                // Call default implementation when not searching
                Init(rect, hierarchyType, searchFilter, checkThumbnails, searchSessionOptions);
            }
        }

        void InitListAreaWithItems(Rect rect, HierarchyType hierarchyType, SearchFilter searchFilter, bool checkThumbnails, IEnumerable<string> items, Func<string, int> assetToInstanceId, SearchService.SearchSessionOptions searchSessionOptions)
        {
            // When items is null, we fallback to default implementation. Current default search engine returns null.
            Init(rect, hierarchyType, items == null ? searchFilter : new SearchFilter(), checkThumbnails, searchSessionOptions);
            if (items != null && hierarchyType == HierarchyType.Assets)
            {
                // We only support assets under "Assets" and "Packages"
                var instanceIdSet = new HashSet<int>();
                var uniqueInstanceIds = new List<int>();
                var rootPaths = new List<string>();
                var itemsTaken = 0;
                foreach (var path in items)
                {
                    if (string.IsNullOrEmpty(path))
                        continue;

                    var reformattedPath = path.Replace('\\', '/');

                    var rootPath = "";
                    if (reformattedPath.StartsWith("Assets"))
                        rootPath = "Assets";
                    else if (reformattedPath.StartsWith("Packages"))
                    {
                        var secondSlashIndex = reformattedPath.IndexOf('/', "Packages".Length + 1);
                        rootPath = secondSlashIndex == -1 ? reformattedPath : reformattedPath.Substring(0, secondSlashIndex);
                    }
                    else
                        continue;

                    // We don't support showing root folders
                    if (reformattedPath == rootPath)
                        continue;

                    var instanceId = assetToInstanceId(reformattedPath);
                    if (instanceId == 0)
                        continue;

                    if (instanceIdSet.Add(instanceId))
                    {
                        uniqueInstanceIds.Add(instanceId);
                        rootPaths.Add(rootPath);
                        ++itemsTaken;
                    }

                    if (itemsTaken >= FilteredHierarchy.maxSearchAddCount)
                        break;
                }
                ShowObjectsInList(uniqueInstanceIds.ToArray(), rootPaths.ToArray());
            }
        }

        bool HasFocus()
        {
            if (!allowFocusRendering)
                return true;
            return m_KeyboardControlID == GUIUtility.keyboardControl && m_Owner.m_Parent.hasFocus;
        }

        internal float GetVisibleWidth()
        {
            return m_VisibleRect.width;
        }

        public float m_SpaceBetween = 6f;
        public float m_TopMargin = 10f;
        public float m_BottomMargin = 10f;
        public float m_RightMargin = 10f;
        public float m_LeftMargin = 10f;

        public void OnGUI(Rect position, int keyboardControlID)
        {
            s_VCEnabled = VersionControlUtils.isVersionControlConnected;

            Event evt = Event.current;

            m_TotalRect = position;

            FrameLastClickedItemIfWanted();

            // Background
            GUI.Label(m_TotalRect, GUIContent.none, Styles.iconAreaBg);

            // For keyboard focus handling (for Tab support and rendering of keyboard focus state)
            m_KeyboardControlID = keyboardControlID;

            // Grab keyboard focus on mousedown in entire rect
            if (evt.type == EventType.MouseDown && position.Contains(Event.current.mousePosition))
            {
                GUIUtility.keyboardControl = m_KeyboardControlID;
                m_AllowRenameOnMouseUp = true; // Reset on mouse down

                Repaint(); // Ensure repaint so we can show we have keyboard focus
            }

            bool hasKeyboardFocus = m_KeyboardControlID == GUIUtility.keyboardControl;
            if (hasKeyboardFocus != m_State.m_HadKeyboardFocusLastEvent)
            {
                m_State.m_HadKeyboardFocusLastEvent = hasKeyboardFocus;

                // We got focus
                if (hasKeyboardFocus)
                {
                    if (evt.type == EventType.MouseDown)
                        m_AllowRenameOnMouseUp = false; // If we got focus by mouse down then we do not want to begin renaming if clicking on an already selected item

                    if (m_GotKeyboardFocus != null)
                        m_GotKeyboardFocus();
                }
            }

            // For key navigation: Auto set selection to first element if selection is not shown currently when tabbing
            if (evt.keyCode == KeyCode.Tab && evt.type == EventType.KeyDown && !hasKeyboardFocus && !IsShowingAny(GetSelection()))
            {
                AssetReference firstAssetReference;
                if (m_LocalAssets.AssetReferenceAtIndex(0, out firstAssetReference))
                {
                    m_LocalAssets.GetNewSelection(ref firstAssetReference, false, false);
                    Selection.activeInstanceID = firstAssetReference.instanceID;
                }
            }


            HandleKeyboard(true);
            HandleZoomScrolling();
            HandleListArea();
            DoOffsetSelection();
            HandleUnusedEvents();
        }

        void FrameLastClickedItemIfWanted()
        {
            if (m_FrameLastClickedItem && Event.current.type == EventType.Repaint)
            {
                m_FrameLastClickedItem = false;
                double timeSinceLastDraw = EditorApplication.timeSinceStartup - m_LocalAssets.m_LastClickedDrawTime;
                if (m_State.m_SelectedInstanceIDs.Count > 0 && timeSinceLastDraw < 0.2)
                    Frame(m_State.m_LastClickedInstanceID, true, false);
            }
        }

        void HandleUnusedEvents()
        {
            if (allowDeselection && Event.current.type == EventType.MouseDown && Event.current.button == 0 && m_TotalRect.Contains(Event.current.mousePosition))
                SetSelection(new int[0], false);
        }

        internal Vector2 sizeUsedForCroppingName;

        public bool CanShowThumbnails()
        {
            //
            //      return m_AllowThumbnails;
            // #else
            return m_AllowThumbnails;
            //
        }

        public int gridSize
        {
            get { return m_State.m_GridSize; }
            set
            {
                if (m_State.m_GridSize != value)
                {
                    m_State.m_GridSize = value;
                    m_FrameLastClickedItem = true;
                }
            }
        }

        public int minGridSize
        {
            get { return m_MinGridSize; }
        }
        public int maxGridSize
        {
            get { return m_MaxGridSize; }
        }


        public int numItemsDisplayed
        {
            get { return m_LocalAssets.ItemCount; }
        }

        bool ObjectsHaveThumbnails(HierarchyType type, SearchFilter searchFilter, SearchService.SearchSessionOptions searchSessionOptions)
        {
            // Check if we have any built-ins, if so we have thumbs since all builtins have thumbs
            if (m_LocalAssets.HasBuiltinResources)
                return true;

            // Check if current hierarchy have thumbs
            FilteredHierarchy hierarchy = new FilteredHierarchy(type, searchSessionOptions);
            hierarchy.searchFilter = searchFilter;
            IHierarchyProperty assetProperty = FilteredHierarchyProperty.CreateHierarchyPropertyForFilter(hierarchy);
            int[] empty = new int[0];
            if (assetProperty.CountRemaining(empty) == 0)
                return true;

            assetProperty.Reset();
            while (assetProperty.Next(empty))
            {
                if (assetProperty.hasFullPreviewImage)
                    return true;
            }

            return false;
        }

        internal void OnDestroy()
        {
            AssetPreview.DeletePreviewTextureManagerByID(GetAssetPreviewManagerID());
        }

        public void Repaint()
        {
            if (m_RepaintWantedCallback != null)
                m_RepaintWantedCallback();
        }

        public void OnEvent()
        {
            GetRenameOverlay().OnEvent();
        }

        CreateAssetUtility GetCreateAssetUtility()
        {
            return m_State.m_CreateAssetUtility;
        }

        internal RenameOverlay GetRenameOverlay()
        {
            return m_State.m_RenameOverlay;
        }

        internal void BeginNamingNewAsset(string newAssetName, int instanceID, bool isCreatingNewFolder)
        {
            m_State.m_NewAssetIndexInList = m_LocalAssets.IndexOfNewText(newAssetName, isCreatingNewFolder, foldersFirst);
            if (m_State.m_NewAssetIndexInList != -1)
            {
                Frame(instanceID, true, false);
                GetRenameOverlay().BeginRename(newAssetName, instanceID, 0f);
            }
            else
            {
                Debug.LogError("Failed to insert new asset into list");
            }

            Repaint();
        }

        public bool BeginRename(float delay)
        {
            if (!allowRenaming)
                return false;

            // Only allow renaming when one item is selected
            if (m_State.m_SelectedInstanceIDs.Count != 1)
                return false;
            int instanceID = m_State.m_SelectedInstanceIDs[0];
            if (!InternalEditorUtility.CanRenameAsset(instanceID))
                return false;

            string name = m_LocalAssets.GetNameOfLocalAsset(instanceID);

            return GetRenameOverlay().BeginRename(name, instanceID, delay);
        }

        public void EndRename(bool acceptChanges)
        {
            if (GetRenameOverlay().IsRenaming())
            {
                GetRenameOverlay().EndRename(acceptChanges);
                RenameEnded();
            }
        }

        void RenameEnded()
        {
            // We are done renaming (user accepted/rejected, we lost focus etc, other grabbed renameOverlay etc.)
            string name = string.IsNullOrEmpty(GetRenameOverlay().name) ? GetRenameOverlay().originalName : GetRenameOverlay().name;
            int instanceID = GetRenameOverlay().userData; // we passed in an instanceID as userData

            try
            {
                // Are we creating new asset?
                if (GetCreateAssetUtility().IsCreatingNewAsset())
                {
                    if (GetRenameOverlay().userAcceptedRename)
                        GetCreateAssetUtility().EndNewAssetCreation(name);
                    else
                        GetCreateAssetUtility().EndNewAssetCreationCanceled(name);
                }
                else // renaming existing asset
                {
                    if (GetRenameOverlay().userAcceptedRename)
                    {
                        ObjectNames.SetNameSmartWithInstanceID(instanceID, name);
                    }
                }

                if (GetRenameOverlay().HasKeyboardFocus())
                    GUIUtility.keyboardControl = m_KeyboardControlID;

                if (GetRenameOverlay().userAcceptedRename)
                {
                    Frame(instanceID, true, false); // frames existing assets (new ones could have instanceID 0)
                }
            }
            catch (UnityException)
            {
                // Any UnityException reported by the asset database is already printed in the console.
                GUIUtility.keyboardControl = m_KeyboardControlID;
            }
            finally
            {
                ClearRenameState();
            }
        }

        void ClearRenameState()
        {
            // Cleanup
            GetRenameOverlay().Clear();
            GetCreateAssetUtility().Clear();
            m_State.m_NewAssetIndexInList = -1;
        }

        internal void HandleRenameOverlay()
        {
            if (GetRenameOverlay().IsRenaming())
            {
                GUIStyle renameStyle = (IsListMode() ? null : Styles.miniRenameField);
                if (!GetRenameOverlay().OnGUI(renameStyle))
                {
                    RenameEnded();
                    GUIUtility.ExitGUI(); // We exit gui because we are iterating items and when we end naming a new asset this will change the order of items we are iterating.
                }
            }
        }

        public bool IsSelected(int instanceID)
        {
            return m_State.m_SelectedInstanceIDs.Contains(instanceID);
        }

        public int[] GetSelection()
        {
            return m_State.m_SelectedInstanceIDs.ToArray();
        }

        public bool IsLastClickedItemVisible()
        {
            return GetSelectedAssetIdx() >= 0;
        }

        public void SelectAll()
        {
            List<int> instanceIDs;
            List<string> guids;
            m_LocalAssets.GetAssetReferences(out instanceIDs, out guids);
            if (instanceIDs.Count != 0)
            {
                var selectedInstanceIDs = InternalEditorUtility.TryGetInstanceIds(instanceIDs, guids, 0, instanceIDs.Count - 1);
                if (selectedInstanceIDs == null)
                {
                    Debug.Log("Cannot select all because some assets being selected are in progress of being imported");
                    return;
                }
            }
            SetSelection(instanceIDs.ToArray(), false);
        }

        public void SetSelection(int[] selectedInstanceIDs, bool doubleClicked)
        {
            InitSelection(selectedInstanceIDs);

            if (m_ItemSelectedCallback != null)
            {
                Repaint();
                m_ItemSelectedCallback(doubleClicked);
            }
        }

        public void InitSelection(int[] selectedInstanceIDs)
        {
            // Note that selectedInstanceIDs can be gameObjects
            m_State.m_SelectedInstanceIDs = new List<int>(selectedInstanceIDs);

            // Keep for debugging
            //Debug.Log ("InitSelection (ObjectListArea): new selection " + DebugUtils.ListToString(m_State.m_SelectedInstanceIDs));

            if (m_State.m_SelectedInstanceIDs.Count > 0)
            {
                // Only init last clicked instance if it is NOT part of our selection (we need it for navigation)
                if (!m_State.m_SelectedInstanceIDs.Contains(m_State.m_LastClickedInstanceID))
                    m_State.m_LastClickedInstanceID = m_State.m_SelectedInstanceIDs[m_State.m_SelectedInstanceIDs.Count - 1];
            }
            else
            {
                m_State.m_LastClickedInstanceID = 0;
            }
        }

        void HandleZoomScrolling()
        {
            if (EditorGUI.actionKey && Event.current.type == EventType.ScrollWheel && m_TotalRect.Contains(Event.current.mousePosition))
            {
                int sign = Event.current.delta.y > 0 ? -1 : 1;
                gridSize = Mathf.Clamp(gridSize + sign * 7, minGridSize, maxGridSize);

                if (sign < 0 && gridSize < m_MinIconSize)
                    gridSize = m_MinGridSize;
                if (sign > 0 && gridSize < m_MinIconSize)
                    gridSize = m_MinIconSize;

                Event.current.Use();
                GUI.changed = true;
            }
        }

        bool IsPreviewIconExpansionModifierPressed()
        {
            return Event.current.alt;
        }

        bool AllowLeftRightArrowNavigation()
        {
            bool gridMode = !m_LocalAssets.ListMode && !IsPreviewIconExpansionModifierPressed();
            bool validItemCount = !m_ShowLocalAssetsOnly || (m_LocalAssets.ItemCount > 1);
            return gridMode && validItemCount;
        }

        public void HandleKeyboard(bool checkKeyboardControl)
        {
            // Are we allowed to handle keyboard events?
            if (checkKeyboardControl && GUIUtility.keyboardControl != m_KeyboardControlID || !GUI.enabled)
                return;

            // Let client handle keyboard first
            if (m_KeyboardInputCallback != null)
                m_KeyboardInputCallback();

            // Now default list area handling
            if (Event.current.type == EventType.KeyDown)
            {
                int offset = 0;

                if (IsLastClickedItemVisible())
                {
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.UpArrow:
                            offset = -m_LocalAssets.m_Grid.columns; // we assume that all groups have same number of columns
                            break;
                        case KeyCode.DownArrow:
                            offset = m_LocalAssets.m_Grid.columns;
                            break;
                        case KeyCode.LeftArrow:
                            if (AllowLeftRightArrowNavigation())
                                offset = -1;
                            break;
                        case KeyCode.RightArrow:
                            if (AllowLeftRightArrowNavigation())
                                offset = 1;
                            break;
                        case KeyCode.Home:
                            offset = kHome;
                            break;
                        case KeyCode.End:
                            offset = kEnd;
                            break;
                        case KeyCode.PageUp:
                            offset = kPageUp;
                            break;
                        case KeyCode.PageDown:
                            offset = kPageDown;
                            break;
                    }
                }
                else
                {
                    // Select first on any key navigation events if not selection is present
                    bool validNavigationKey = false;
                    switch (Event.current.keyCode)
                    {
                        case KeyCode.LeftArrow:
                        case KeyCode.RightArrow:
                            validNavigationKey = AllowLeftRightArrowNavigation();
                            break;

                        case KeyCode.UpArrow:
                        case KeyCode.DownArrow:
                        case KeyCode.Home:
                        case KeyCode.End:
                        case KeyCode.PageUp:
                        case KeyCode.PageDown:
                            validNavigationKey = true;
                            break;
                    }

                    if (validNavigationKey)
                    {
                        SelectFirst();
                        Event.current.Use();
                    }
                }

                if (offset != 0)
                {
                    // If nothing is selected then select first object and ignore the offset (when showing none GetSelectedAssetIdx return -1)
                    if (GetSelectedAssetIdx() < 0 && !m_LocalAssets.ShowNone)
                        SetSelectedAssetByIdx(0);
                    else
                        m_SelectionOffset = offset;

                    Event.current.Use();
                    GUI.changed = true;
                }
                else
                {
                    if (allowFindNextShortcut && m_LocalAssets.DoCharacterOffsetSelection())
                        Event.current.Use();
                }
            }
        }

        void DoOffsetSelectionSpecialKeys(int idx, int maxIndex)
        {
            float itemHeight = m_LocalAssets.m_Grid.itemSize.y + m_LocalAssets.m_Grid.verticalSpacing;
            int columns = m_LocalAssets.m_Grid.columns;

            switch (m_SelectionOffset)
            {
                case kPageUp:
                    // on OSX paging only scrolls the scrollbar
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        m_State.m_ScrollPosition.y -= m_TotalRect.height;
                        m_SelectionOffset = 0;
                    }
                    else
                    {
                        m_SelectionOffset = -Mathf.RoundToInt(m_TotalRect.height / itemHeight) * columns;
                        // we want it to go to the very top row, but stay on same column
                        m_SelectionOffset = Mathf.Max(-Mathf.FloorToInt(idx / (float)columns) * columns, m_SelectionOffset);
                    }
                    break;
                case kPageDown:
                    // on OSX paging only scrolls the scrollbar
                    if (Application.platform == RuntimePlatform.OSXEditor)
                    {
                        m_State.m_ScrollPosition.y += m_TotalRect.height;
                        m_SelectionOffset = 0;
                    }
                    else
                    {
                        m_SelectionOffset = Mathf.RoundToInt(m_TotalRect.height / itemHeight) * columns;
                        // we want it to go to the very bottom row, but stay on same column
                        int remainingItems = maxIndex - idx;
                        m_SelectionOffset = Mathf.Min(Mathf.FloorToInt(remainingItems / (float)columns) * columns, m_SelectionOffset);
                    }
                    break;
                case kHome:
                    m_SelectionOffset = 0;
                    SetSelectedAssetByIdx(0); // assumes that 'none' is the first item
                    return;
                case kEnd:
                    m_SelectionOffset = maxIndex - idx;
                    break;
            }
        }

        void DoOffsetSelection()
        {
            if (m_SelectionOffset == 0)
                return;

            int maxGridIndex = GetMaxIdx();
            if (maxGridSize == -1)
                return; // no items

            int selectedAssetIdx = GetSelectedAssetIdx();
            selectedAssetIdx = selectedAssetIdx < 0 ? 0 : selectedAssetIdx; // default to first item

            DoOffsetSelectionSpecialKeys(selectedAssetIdx, maxGridIndex);

            // Special keys on some OSs will simply scroll and not change selection
            if (m_SelectionOffset == 0)
                return;

            int newGridIdx = selectedAssetIdx + m_SelectionOffset;
            m_SelectionOffset = 0;

            // We ignore the offset if newIdx is less than 0 or clamp to item list length.
            // This ensures that we stay on the same column at top or jump to the last item when navigating with keys.
            if (newGridIdx < 0)
                newGridIdx = selectedAssetIdx;
            else
                newGridIdx = Mathf.Min(newGridIdx, maxGridIndex);

            // If newIdx is on one of the two bottom rows we scroll to the bottom row because we might jump to another row
            // when navigating (because the last row is half empty). This is a usability decision and can be removed.
            int scrollGridIdx = newGridIdx;
            //if (newGridIdx >= m_Columns * m_Rows - m_Columns * 2)
            //  scrollGridIdx = maxGridIndex + 2 * m_Columns;

            SetSelectedAssetByIdx(scrollGridIdx);
        }

        public void OffsetSelection(int selectionOffset)
        {
            m_SelectionOffset = selectionOffset;
        }

        public void SelectFirst()
        {
            int startIndex = 0;
            if (m_ShowLocalAssetsOnly && m_LocalAssets.ShowNone && m_LocalAssets.ItemCount > 1)
                startIndex = 1;
            SetSelectedAssetByIdx(startIndex);
        }

        void SetSelectedAssetByIdx(int selectedIdx)
        {
            // instanceID can be 0 if 'None' item is at index
            AssetReference assetReference;
            if (m_LocalAssets.AssetReferenceAtIndex(selectedIdx, out assetReference))
            {
                Rect r = m_LocalAssets.m_Grid.CalcRect(selectedIdx, 0f);
                ScrollToPosition(AdjustRectForFraming(r));
                Repaint();

                int[] newSelection;
                if (IsLocalAssetsCurrentlySelected())
                    newSelection = m_LocalAssets.GetNewSelection(ref assetReference, false, true).ToArray(); // Handle multi selection
                else
                    newSelection = m_LocalAssets.GetNewSelection(ref assetReference, false, false).ToArray();

                SetSelection(newSelection, false);
                m_State.m_LastClickedInstanceID = assetReference.instanceID;
            }
        }

        void Reveal(int instanceID)
        {
            if (!AssetDatabase.Contains(instanceID))
                return;

            // We only show one level of subassets so just expand parent asset
            int mainAssetInstanceID = AssetDatabase.GetMainAssetInstanceID(AssetDatabase.GetAssetPath(instanceID));
            bool isSubAsset = mainAssetInstanceID != instanceID;
            if (isSubAsset)
                m_LocalAssets.ChangeExpandedState(mainAssetInstanceID, true);
        }

        // Frames only local assets
        public bool Frame(int instanceID, bool frame, bool ping)
        {
            int index = -1;

            // Check if it is an asset we are creating
            if (GetCreateAssetUtility().IsCreatingNewAsset() && m_State.m_NewAssetIndexInList != -1)
                if (GetCreateAssetUtility().instanceID == instanceID)
                    index = m_State.m_NewAssetIndexInList;

            // Ensure instanceID is visible
            if (frame)
                Reveal(instanceID);

            // Check local assets
            if (index == -1)
                index = m_LocalAssets.IndexOf(instanceID);

            if (index != -1)
            {
                if (frame)
                {
                    float yOffset = 0f;
                    Rect r = m_LocalAssets.m_Grid.CalcRect(index, yOffset);
                    CenterRect(AdjustRectForFraming(r));
                    Repaint();
                }

                if (ping)
                    BeginPing(instanceID);
                return true;
            }

            return false;
        }

        int GetSelectedAssetIdx()
        {
            // Find index of selection
            int offsetIdx = m_LocalAssets.IndexOf(m_State.m_LastClickedInstanceID);
            if (offsetIdx != -1)
                return offsetIdx;
            return -1;
        }

        bool SkipGroup(Group group)
        {
            // We show local assets here
            if (!m_ShowLocalAssetsOnly)
            {
                if (group is LocalGroup)
                    return true;
            }

            return false;
        }

        int GetMaxIdx()
        {
            int groupLastIdx = 0;
            int groupSizesAccumulated = 0;
            int lastGroupSize = 0;

            foreach (Group g in m_Groups)
            {
                if (SkipGroup(g))
                    continue;

                if (!g.Visible)
                    continue;

                groupSizesAccumulated += lastGroupSize;
                lastGroupSize = g.m_Grid.rows * g.m_Grid.columns;
                groupLastIdx = g.ItemCount - 1;
            }
            int max = groupSizesAccumulated + groupLastIdx;
            return (lastGroupSize + max) == 0 ? -1 : max;
        }

        bool IsLocalAssetsCurrentlySelected()
        {
            int currentSelectedInstanceID = m_State.m_SelectedInstanceIDs.FirstOrDefault();
            if (currentSelectedInstanceID != 0)
            {
                int index = m_LocalAssets.IndexOf(currentSelectedInstanceID);
                return index != -1;
            }

            return false;
        }

        private void SetupData(bool forceReflow)
        {
            // Make sure the groups contains the correct assets to show
            foreach (Group g in m_Groups)
            {
                if (SkipGroup(g))
                    continue;
                g.UpdateAssets();
            }

            if (forceReflow || Event.current.type == EventType.Repaint)
            {
                // Reflow according to number of items, scrollbar presence, item dims etc.
                Reflow();
            }
        }

        bool IsObjectSelector()
        {
            // ShowNone is only used in object select window
            return m_LocalAssets.ShowNone;
        }

        void HandleListArea()
        {
            SetupData(false);

            // Figure out height needed to contain all assets
            float totalContentsHeight = 0f;
            foreach (Group g in m_Groups)
            {
                if (SkipGroup(g))
                    continue;

                totalContentsHeight += g.Height;

                // ShowNone is only used in object select window
                if (m_LocalAssets.ShowNone)
                    break;
            }

            bool maxItemsReached = m_LocalAssets.projectItemCount >= FilteredHierarchy.maxSearchAddCount;
            if (maxItemsReached)
                totalContentsHeight += 45;

            Rect scrollRect = m_TotalRect;
            Rect contentRect = new Rect(0, 0, 1, totalContentsHeight);
            bool scrollBarVisible = totalContentsHeight > m_TotalRect.height;

            m_VisibleRect = m_TotalRect;
            if (scrollBarVisible)
                m_VisibleRect.width -= kSpaceForScrollBar;


            double timeNow = EditorApplication.timeSinceStartup;
            m_LastScrollPosition = m_State.m_ScrollPosition;

            bool needRepaint = false;
            m_State.m_ScrollPosition = GUI.BeginScrollView(scrollRect, m_State.m_ScrollPosition, contentRect);
            {
                Vector2 scrollPos = m_State.m_ScrollPosition; // Copy scroll pos since the draw calls may change it

                if (m_LastScrollPosition != m_State.m_ScrollPosition)
                    LastScrollTime = timeNow;

                float yOffset = 0f;
                int rowsBeingUsed = 0;
                foreach (Group g in m_Groups)
                {
                    if (SkipGroup(g))
                        continue;

                    // rect contains the offset rect where the group should draw
                    g.Draw(yOffset, scrollPos, ref rowsBeingUsed);
                    needRepaint = needRepaint || g.NeedsRepaint;
                    yOffset += g.Height;

                    // ShowNone is only used in object select window
                    if (m_LocalAssets.ShowNone)
                        break;
                }

                if (maxItemsReached)
                {
                    GUI.Label(new Rect(0, yOffset + 20, scrollRect.width, 16), Styles.maxmumItemCountInfo, EditorStyles.centeredGreyMiniLabel);
                }

                HandlePing();
                if (needRepaint)
                    Repaint();
            } GUI.EndScrollView();
        }

        bool IsListMode()
        {
            if (allowMultiSelect)
                return (gridSize == kListLineHeight); // ProjectBrowser (should auto change layout on content but entirely user controlled)
            else
                return (gridSize == kListLineHeight) || !CanShowThumbnails(); // ObjectSelector
        }

        void Reflow()
        {
            if (gridSize < 20)
                gridSize = m_MinGridSize;
            else if (gridSize < m_MinIconSize)
                gridSize = m_MinIconSize;

            // We're in list mode.
            if (IsListMode())
            {
                foreach (Group g in m_Groups)
                {
                    if (SkipGroup(g))
                        continue;

                    g.ListMode = true;
                    UpdateGroupSizes(g);

                    // ShowNone is only used in object select window
                    if (m_LocalAssets.ShowNone)
                        break;
                }
            }
            // we're in thumbnail mode
            else
            {
                // Grid without scrollbar
                float totalHeight = 0;
                foreach (Group g in m_Groups)
                {
                    if (SkipGroup(g))
                        continue;

                    g.ListMode = false;
                    UpdateGroupSizes(g);

                    totalHeight += g.Height;

                    // ShowNone is only used in object select window
                    if (m_LocalAssets.ShowNone)
                        break;
                }

                // Grid with scrollbar
                bool scrollbarVisible = m_TotalRect.height < totalHeight;
                if (scrollbarVisible)
                {
                    // Make room for the scrollbar
                    foreach (Group g in m_Groups)
                    {
                        if (SkipGroup(g))
                            continue;

                        g.m_Grid.fixedWidth = m_TotalRect.width - kSpaceForScrollBar;
                        g.m_Grid.InitNumRowsAndColumns(g.ItemCount, g.m_Grid.CalcRows(g.ItemsWantedShown));
                        g.UpdateHeight();

                        // ShowNone is only used in object select window
                        if (m_LocalAssets.ShowNone)
                            break;
                    }
                }

                int maxVisibleItems = GetMaxNumVisibleItems();

                AssetPreview.SetPreviewTextureCacheSize(maxVisibleItems * 2 + 30, GetAssetPreviewManagerID());
            }
        }

        void UpdateGroupSizes(Group g)
        {
            if (g.ListMode)
            {
                g.m_Grid.fixedWidth = m_VisibleRect.width;
                g.m_Grid.itemSize = new Vector2(m_VisibleRect.width, kListLineHeight);
                g.m_Grid.topMargin = 0f;
                g.m_Grid.bottomMargin = 0f;
                g.m_Grid.leftMargin = 0f;
                g.m_Grid.rightMargin = 0f;
                g.m_Grid.verticalSpacing = 0f;
                g.m_Grid.minHorizontalSpacing = 0f;
                g.m_Grid.InitNumRowsAndColumns(g.ItemCount, g.ItemsWantedShown);

                g.UpdateHeight();
            }
            else
            {
                g.m_Grid.fixedWidth = m_TotalRect.width;
                g.m_Grid.itemSize = new Vector2(gridSize, gridSize + 14);
                g.m_Grid.topMargin = 10f;
                g.m_Grid.bottomMargin = 10f;
                g.m_Grid.leftMargin = 10f;
                g.m_Grid.rightMargin = 10f;
                g.m_Grid.verticalSpacing = 15f;
                g.m_Grid.minHorizontalSpacing = 12f;
                g.m_Grid.InitNumRowsAndColumns(g.ItemCount, g.m_Grid.CalcRows(g.ItemsWantedShown));

                g.UpdateHeight();
            }
        }

        int GetMaxNumVisibleItems()
        {
            foreach (Group g in m_Groups)
            {
                if (SkipGroup(g))
                    continue;

                return g.m_Grid.GetMaxVisibleItems(m_TotalRect.height);
            }

            return 0;
        }

        static Rect AdjustRectForFraming(Rect r)
        {
            r.height += (Styles.resultsGridLabel.fixedHeight * 2);
            r.y -= Styles.resultsGridLabel.fixedHeight;
            return r;
        }

        void CenterRect(Rect r)
        {
            float middle = (r.yMax + r.yMin) / 2;
            float middleVisibleRect = m_TotalRect.height / 2;

            m_State.m_ScrollPosition.y = middle - middleVisibleRect;

            // Ensure clamped
            ScrollToPosition(r);
        }

        void ScrollToPosition(Rect r)
        {
            float top = r.y;
            float bottom = r.yMax;
            float viewHeight = m_TotalRect.height;

            if (bottom > viewHeight + m_State.m_ScrollPosition.y)
            {
                m_State.m_ScrollPosition.y = bottom - viewHeight;
            }
            if (top < m_State.m_ScrollPosition.y)
            {
                m_State.m_ScrollPosition.y = top;
            }

            m_State.m_ScrollPosition.y = Mathf.Max(m_State.m_ScrollPosition.y, 0f);
        }

        public void OnInspectorUpdate()
        {
            if (EditorApplication.timeSinceStartup > m_NextDirtyCheck && m_LocalAssets.IsAnyLastRenderedAssetsDirty())
            {
                // If an asset is dirty we ensure to get a updated preview by clearing cache of temporary previews
                AssetPreview.ClearTemporaryAssetPreviews();
                Repaint();
                m_NextDirtyCheck = EditorApplication.timeSinceStartup + 0.77;
            }
        }

        public bool IsShowing(int instanceID)
        {
            return m_LocalAssets.IndexOf(instanceID) >= 0;
        }

        public bool IsShowingAny(int[] instanceIDs)
        {
            if (instanceIDs.Length == 0)
                return false;

            foreach (int instanceID in instanceIDs)
                if (IsShowing(instanceID))
                    return true;

            return false;
        }

        protected Texture GetIconByInstanceID(int instanceID)
        {
            Texture icon = null;
            if (instanceID != 0)
            {
                string path = AssetDatabase.GetAssetPath(instanceID);
                icon = AssetDatabase.GetCachedIcon(path);
            }
            return icon;
        }

        internal int GetAssetPreviewManagerID()
        {
            return m_Owner.GetInstanceID();
        }

        // Pings only local assets
        public void BeginPing(int instanceID)
        {
            // Check local assets
            int index =  m_LocalAssets.IndexOf(instanceID);

            if (index != -1)
            {
                string name = null;
                HierarchyProperty hierarchyProperty = new HierarchyProperty(HierarchyType.Assets);
                if (hierarchyProperty.Find(instanceID, null))
                {
                    name = hierarchyProperty.name;
                }
                var path = AssetDatabase.GetAssetPath(instanceID);
                if (string.IsNullOrEmpty(path))
                    return;

                var packageInfo = PackageManager.PackageInfo.FindForAssetPath(path);
                if (packageInfo != null)
                {
                    hierarchyProperty = new HierarchyProperty(packageInfo.assetPath);
                    if (hierarchyProperty.Find(instanceID, null))
                    {
                        name = hierarchyProperty.name;
                    }
                }
                if (name == null)
                    return;

                m_Ping.isPinging = true;
                m_Ping.m_AvailableWidth = m_VisibleRect.width;
                m_pingIndex = index;

                float vcPadding = s_VCEnabled ? k_ListModeVersionControlOverlayPadding : 0f;
                var assetReference = new AssetReference() { instanceID = instanceID };
                var textClipping = m_LocalAssets.ListMode ? TextClipping.Overflow : TextClipping.Ellipsis;
                GUIContent cont = new GUIContent(name);
                string label = cont.text;

                if (m_LocalAssets.ListMode)
                {
                    const float iconWidth = 16;
                    m_Ping.m_PingStyle = Styles.ping;
                    Vector2 pingLabelSize = m_Ping.m_PingStyle.CalcSize(cont);
                    m_Ping.m_ContentRect.width = pingLabelSize.x + vcPadding + iconWidth;
                    m_Ping.m_ContentRect.height = pingLabelSize.y;
                    m_LeftPaddingForPinging = hierarchyProperty.isMainRepresentation ? LocalGroup.k_ListModeLeftPadding : LocalGroup.k_ListModeLeftPaddingForSubAssets;
                    FilteredHierarchy.FilterResult res = m_LocalAssets.LookupByInstanceID(instanceID);
                    var icon = hierarchyProperty.icon;
                    m_Ping.m_ContentDraw = (Rect r) =>
                    {
                        if (icon)
                        {
                            m_LocalAssets.DrawIconAndLabel(r, res, label, icon, false, false);
                        }
                    };
                }
                else
                {
                    m_Ping.m_PingStyle = Styles.miniPing;
                    var oldClipping = m_Ping.m_PingStyle.clipping;
                    m_Ping.m_PingStyle.clipping = textClipping;
                    Vector2 pingLabelSize = Styles.resultsGridLabel.CalcSizeWithConstraints(cont, sizeUsedForCroppingName);
                    m_Ping.m_ContentRect.width = pingLabelSize.x + Styles.resultsGridLabel.padding.horizontal;
                    m_Ping.m_ContentRect.height = pingLabelSize.y;
                    m_Ping.m_ContentDraw = (Rect r) =>
                    {
                        // We need to temporary adjust style to render into content rect (org anchor is middle-centered)
                        var orgAnchor = Styles.resultsGridLabel.alignment;
                        var orgClipping = Styles.resultsGridLabel.clipping;
                        // Shift the rect to match the original text position
                        r.position -= new Vector2(5, 1);
                        Styles.resultsGridLabel.alignment = TextAnchor.MiddleCenter;
                        Styles.resultsGridLabel.clipping = TextClipping.Ellipsis;
                        Styles.resultsGridLabel.Draw(r, label, false, false, false, false);
                        Styles.resultsGridLabel.alignment = orgAnchor;
                        Styles.resultsGridLabel.clipping = orgClipping;
                    };
                    m_Ping.m_PingStyle.clipping = oldClipping;
                }
                Vector2 pos = CalculatePingPosition();
                m_Ping.m_ContentRect.x = pos.x;
                m_Ping.m_ContentRect.y = pos.y;

                Repaint();
            }
        }

        public void EndPing()
        {
            m_Ping.isPinging = false;
        }

        void HandlePing()
        {
            // We need to update m_Ping.m_ContentTopLeft in icon mode. The position might change if user resizes the window while pinging
            if (m_Ping.isPinging && !m_LocalAssets.ListMode)
            {
                Vector2 pos = CalculatePingPosition();
                m_Ping.m_ContentRect.x = pos.x;
                m_Ping.m_ContentRect.y = pos.y;
            }
            m_Ping.HandlePing();

            if (m_Ping.isPinging)
                Repaint();
        }

        Vector2 CalculatePingPosition()
        {
            Rect gridRect = m_LocalAssets.m_Grid.CalcRect(m_pingIndex, 0f);

            if (m_LocalAssets.ListMode)
            {
                return new Vector2(m_LeftPaddingForPinging, gridRect.y);
            }
            else
            {
                Vector2 adjustLabel = new Vector2(-3, 2); // adjust so ping label matches grid label exactly
                float width = m_Ping.m_ContentRect.width;
                return new Vector2(gridRect.center.x - width / 2f + m_Ping.m_PingStyle.padding.left + adjustLabel.x, gridRect.yMax - Styles.resultsGridLabel.fixedHeight + adjustLabel.y);
            }
        }
    }
}  // namespace UnityEditor
