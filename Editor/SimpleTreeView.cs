#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEssentials
{
    public class SimpleTreeViewItem : TreeViewItem
    {
        public bool SupportsChildren = true;
        public bool SupportsRenaming = true;

        public SimpleTreeViewItem Support(bool children = true, bool renaming = true)
        {
            SupportsChildren = children;
            SupportsRenaming = renaming;
            return this;
        }

        public string Name => displayName;
        public Texture Icon => icon;
        public string UserTag { get; set; }
        public SimpleTreeViewItem SetUserTag(string userTag)
        {
            UserTag = userTag;
            return this;
        }
        public object UserData { get; set; }
        public SimpleTreeViewItem SetUserData(object userData)
        {
            UserData = userData;
            return this;
        }

        public SimpleTreeViewItem Parent
        {
            get => parent as SimpleTreeViewItem;
            set
            {
                if (Parent == value)
                    return;

                if (!value.SupportsChildren)
                    return;

                if (Parent != null && Parent.children != null)
                    Parent.children.Remove(this);

                parent = value;
                if (value != null)
                {
                    value.children ??= new();
                    if (!value.children.Contains(this))
                        value.children.Add(this);
                }
            }
        }

        public int ChildCount => children?.Count ?? 0;
        public SimpleTreeViewItem GetChild(int index) => children?[index] as SimpleTreeViewItem;
        public IReadOnlyList<SimpleTreeViewItem> Children =>
            children?.Cast<SimpleTreeViewItem>().ToList() ?? new List<SimpleTreeViewItem>();

        public SimpleTreeViewItem() : base(GenerateUniqueId(), 1, GetDefaultDisplayName()) { }
        public SimpleTreeViewItem(Texture2D icon) : base(GenerateUniqueId(), 1, GetDefaultDisplayName()) { base.icon = icon; }
        public SimpleTreeViewItem(string displayName) : base(GenerateUniqueId(), 1, displayName) { }
        public SimpleTreeViewItem(string displayName, Texture2D icon) : base(GenerateUniqueId(), 1, displayName) { base.icon = icon; }

        private static string GetDefaultDisplayName(string displayName = null) =>
            string.IsNullOrEmpty(displayName) ? "TreeViewItem" : displayName;

        private static int GenerateUniqueId() =>
            Guid.NewGuid().GetHashCode();
    }

    public class SimpleTreeView : TreeView
    {
        public TreeViewState TreeViewState;
        public GenericMenu ContextMenu;
        public bool ContextMenuEnabled = false;
        private bool _contextMenuRequested = false;

        public SimpleTreeViewItem RootItem { get; private set; }

        public SimpleTreeView(SimpleTreeViewItem[] rootChildren = null,
                              string rootName = "Root",
                              int rowHeight = 15,
                              bool showBorder = false,
                              bool showAlternatingRowBackgrounds = false)
            : base(new TreeViewState())
        {
            TreeViewState = state;

            var icon = EditorGUIUtility.IconContent("GUISkin Icon").image as Texture2D;
            RootItem = new SimpleTreeViewItem(rootName, icon) { id = 0, depth = 0 };
            foreach (var child in rootChildren)
                child.Parent = RootItem;

            base.rowHeight = rowHeight;
            base.showBorder = showBorder;
            base.showAlternatingRowBackgrounds = showAlternatingRowBackgrounds;
            base.foldoutOverride = (rect, item, expandedState) => false;

            Reload();
            SetExpandedRecursive(RootItem.id, true);
        }

        public void AddItem(SimpleTreeViewItem child, int? parent = null)
        {
            var parentItem = FindItem(parent ??= RootItem.id, rootItem) as SimpleTreeViewItem;
            if (!parentItem.SupportsChildren)
                return;

            child.Parent = parentItem;

            Reload();
            SetExpanded(child.parent.id, true);
            SetSelection(new List<int> { child.id }, TreeViewSelectionOptions.RevealAndFrame);
        }

        public void ClearAllSelections()
        {
            if (GetSelection().Count > 0)
                SetSelection(new List<int>(), TreeViewSelectionOptions.RevealAndFrame);
        }

        public SimpleTreeViewItem GetSelectedItem() =>
            GetSelectedItems().FirstOrDefault();

        public SimpleTreeViewItem[] GetSelectedItems()
        {
            var selectedIds = GetSelection();
            if (selectedIds.Count == 0)
                return Array.Empty<SimpleTreeViewItem>();
            var allItems = GetAllItems();
            return allItems.Where(i => selectedIds.Contains(i.id)).ToArray();
        }

        public void OnGUI()
        {
            var rect = GUILayoutUtility.GetRect(
                0, float.MaxValue,
                0, float.MaxValue,
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true));

            if (Event.current.type == EventType.MouseDown)
            {
                if (_contextMenuRequested)
                {
                    ContextMenuEnabled = false;
                    _contextMenuRequested = false;
                }

                if (Event.current.button == 1)
                    if (!ContextMenuEnabled)
                        if ((!GetSelectedItem()?.SupportsChildren) ?? false)
                            ClearAllSelections();
            }

            OnGUI(rect);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var isRoot = args.item.depth == 0;
            var item = args.item;
            var rect = args.rowRect;
            var padding = 8;
            rect.x += padding;
            rect.width++;

            if (isRoot)
            {
                var lineRect = new Rect(rect.x - padding, rect.y + rect.height - 1, rect.width, 1);
                EditorGUI.DrawRect(lineRect, new Color(0.8f, 0.8f, 0.8f, 0.08f));

                Color backgroundColor = IsSelected(item.id)
                    ? EditorGUIUtility.isProSkin
                        ? new Color(0.17f, 0.36f, 0.53f, 1f)
                        : new Color(0.243f, 0.490f, 0.905f, 1.0f)
                    : EditorGUIUtility.isProSkin
                        ? new Color(0.18f, 0.18f, 0.18f, 1.0f)
                        : new Color(0.85f, 0.85f, 0.85f, 1.0f);
                var backgroundRect = new Rect(rect.x - padding, rect.y, rect.width, rect.height - 1);
                EditorGUI.DrawRect(backgroundRect, backgroundColor);
            }

            const float indentWidth = 16;
            float indent = item.depth * indentWidth + 16;

            if (item.hasChildren)
            {
                var foldoutRect = new Rect(rect.x + indent - 16, rect.y, 16, rect.height);
                bool expanded = IsExpanded(item.id);
                bool newExpanded = EditorGUI.Foldout(foldoutRect, expanded, GUIContent.none, true);
                if (newExpanded != expanded)
                    SetExpanded(item.id, newExpanded);
            }

            var iconRootXOffset = isRoot ? 0 : -2;
            var iconRect = new Rect(rect.x + indent + iconRootXOffset, rect.y, rect.height, rect.height);
            if (item.icon != null)
                GUI.DrawTexture(iconRect, item.icon, ScaleMode.StretchToFill);

            var label = item.displayName;
            var labelIconXOffset = item.icon != null ? 17 : 0;
            var labelRootXOffset = isRoot ? 2 : 0;
            var labelRect = new Rect(rect.x + indent + labelIconXOffset + labelRootXOffset, rect.y, rect.width, rect.height);
            GUI.Label(labelRect, label, isRoot ? EditorStyles.boldLabel : EditorStyles.label);
        }

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            const float indentWidth = 16;
            float indent = item.depth * indentWidth + 16;
            float iconWidth = item.icon != null ? rowRect.height : 0;
            float labelIconXOffset = item.icon != null ? 17 : 0;
            float labelRootXOffset = item.depth == 0 ? 2 : 0;
            float padding = 8;

            float x = rowRect.x + indent + labelIconXOffset + labelRootXOffset + padding;
            float width = rowRect.width - (x - rowRect.x) - padding;

            return new Rect(x + 1, rowRect.y, width + 8, rowRect.height + 1);
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem) as SimpleTreeViewItem;
            if (item == null)
                return;

            ContextMenuEnabled = true;
            _contextMenuRequested = true;

            var menu = new GenericMenu();
            menu.AddItem(new GUIContent("Rename"), false, () => OnBeginRename(item));
            if (item != RootItem)
                menu.AddItem(new GUIContent("Delete"), false, () => OnDeleteItem(item));

            if (ContextMenu != null && item.SupportsChildren)
            {
                menu.AddSeparator("");

                var bindingFlags = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
                var field = menu.GetType().GetField("m_MenuItems", bindingFlags);
                var menuItems = field.GetValue(menu) as System.Collections.IList;
                var customMenuItems = field.GetValue(ContextMenu) as System.Collections.IList;

                foreach (var customMenuItem in customMenuItems)
                    menuItems.Add(customMenuItem);
            }

            menu.ShowAsContext();
        }

        private void OnBeginRename(SimpleTreeViewItem item)
        {
            if (item.SupportsRenaming)
                BeginRename(item);
        }

        private void OnDeleteItem(SimpleTreeViewItem item)
        {
            if (item.Parent != null && item.Parent.children != null)
            {
                item.Parent.children.Remove(item);
                Reload();
            }
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = -1, depth = -1, displayName = string.Empty };
            root.children = new List<TreeViewItem>() { RootItem };
            SetupDepthsFromParentsAndChildren(root);
            return root;
        }

        protected override bool CanStartDrag(CanStartDragArgs args) =>
            !args.draggedItemIDs.Contains(RootItem.id);

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            if (hasSearch)
                return;

            DragAndDrop.PrepareStartDrag();
            var draggedRows = GetRows()
                .Where(item => args.draggedItemIDs.Contains(item.id))
                .OfType<SimpleTreeViewItem>()
                .ToList();

            DragAndDrop.objectReferences = new UnityEngine.Object[] { };
            DragAndDrop.SetGenericData("TreeViewDragging", draggedRows);
            DragAndDrop.StartDrag("Dragging TreeView");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var draggedRows = DragAndDrop.GetGenericData("TreeViewDragging") as List<SimpleTreeViewItem>;
            if (draggedRows == null || draggedRows.Count == 0)
                return DragAndDropVisualMode.None;

            if (args.performDrop)
            {
                var newParent = args.parentItem as SimpleTreeViewItem;

                foreach (var draggedItem in draggedRows)
                {
                    if (draggedItem == RootItem)
                        continue;

                    if (newParent == null)
                    {
                        newParent = RootItem;
                        args.insertAtIndex = int.MaxValue;
                    }

                    if (!newParent.SupportsChildren)
                        continue;

                    if (IsAncestor(draggedItem, newParent))
                        continue;

                    draggedItem.Parent = newParent;

                    if (newParent.children != null)
                    {
                        newParent.children.Remove(draggedItem);

                        int insertIndex = Mathf.Clamp(args.insertAtIndex, 0, newParent.children.Count);
                        newParent.children.Insert(insertIndex, draggedItem);
                    }
                }

                Reload();
                SetExpanded(newParent.id, true);
                SetSelection(draggedRows.Select(i => i.id).ToList(), TreeViewSelectionOptions.RevealAndFrame);
            }
            return DragAndDropVisualMode.Move;
        }

        private bool IsAncestor(SimpleTreeViewItem parent, SimpleTreeViewItem child)
        {
            while (child != null)
            {
                if (child == parent)
                    return true;
                child = child.Parent;
            }
            return false;
        }

        protected override bool CanRename(TreeViewItem item) =>
            item is SimpleTreeViewItem && (item as SimpleTreeViewItem).SupportsRenaming;

        protected override void RenameEnded(RenameEndedArgs args)
        {
            var allItems = GetAllItems();
            if (args.acceptedRename && !string.IsNullOrWhiteSpace(args.newName))
            {
                var item = allItems.FirstOrDefault(i => i.id == args.itemID);
                if (item != null)
                {
                    item.displayName = args.newName;
                    Reload();
                }
            }
        }

        private List<SimpleTreeViewItem> GetAllItems()
        {
            var result = new List<SimpleTreeViewItem>();
            CollectItems(RootItem, result);
            return result;
        }

        private void CollectItems(SimpleTreeViewItem item, List<SimpleTreeViewItem> list)
        {
            list.Add(item);
            foreach (var child in item.Children)
                CollectItems(child, list);
        }
    }
}
#endif