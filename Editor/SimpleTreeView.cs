#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace UnityEssentials
{
    public class SimpleTreeViewItem : TreeViewItem
    {
        private SimpleTreeViewItem _parent;
        private readonly List<SimpleTreeViewItem> _children = new();

        public SimpleTreeViewItem Parent
        {
            get => _parent;
            set
            {
                if (_parent == value)
                    return;

                // Remove from old parent's children
                _parent?._children.Remove(this);
                _parent = value;

                // Add to new parent's children
                if (_parent != null && !_parent._children.Contains(this))
                    _parent._children.Add(this);
            }
        }

        public int ChildCount => _children.Count;
        public SimpleTreeViewItem GetChild(int index) => _children[index];
        public IReadOnlyList<SimpleTreeViewItem> Children => _children.AsReadOnly();

        public SimpleTreeViewItem() : base(GenerateUniqueId(), 0, GetDefaultDisplayName()) { }
        public SimpleTreeViewItem(string displayName) : base(GenerateUniqueId(), 0, displayName) { }

        public static string GetDefaultDisplayName(string displayName = null) =>
            string.IsNullOrEmpty(displayName) ? "TreeViewItem" : displayName;

        public static int GenerateUniqueId() =>
            System.Guid.NewGuid().GetHashCode();

        public int CalculateDepth()
        {
            int depth = 0;
            var current = Parent;
            while (current != null)
            {
                depth++;
                current = current.Parent;
            }
            return depth;
        }
    }

    public class SimpleTreeView : TreeView
    {
        public TreeViewState TreeViewState;
        public SimpleTreeViewItem RootItem { get; private set; }
        public List<SimpleTreeViewItem> RootChildren { get; private set; }

        public SimpleTreeView(SimpleTreeViewItem rootItem,
                             SimpleTreeViewItem[] rootChildren = null,
                             int rowHeight = 20,
                             bool showBorder = false,
                             bool showAlternatingRowBackgrounds = false)
            : base(new TreeViewState())
        {
            TreeViewState = state;
            RootItem = rootItem ?? new SimpleTreeViewItem("Root");
            RootChildren = rootChildren?.ToList() ?? new List<SimpleTreeViewItem>();

            base.rowHeight = rowHeight;
            base.showBorder = showBorder;
            base.showAlternatingRowBackgrounds = showAlternatingRowBackgrounds;

            Reload();
        }

        public void OnGUI()
        {
            var rect = GUILayoutUtility.GetRect(
                0, float.MaxValue,
                0, float.MaxValue,
                GUILayout.ExpandHeight(true),
                GUILayout.ExpandWidth(true));

            OnGUI(rect);
        }

        protected override TreeViewItem BuildRoot()
        {
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>();

            // Add RootItem and its children
            AddItemAndChildren(RootItem, allItems);

            // Add root children and their children as if they are children of RootItem
            foreach (var child in RootChildren)
                AddItemAndChildrenAtDepth(child, RootItem.depth + 1, allItems);

            //SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        // Helper to add children at a specific depth
        private void AddItemAndChildrenAtDepth(SimpleTreeViewItem item, int depth, List<TreeViewItem> list)
        {
            item.depth = depth;
            list.Add(item);
            foreach (var child in item.Children)
                AddItemAndChildrenAtDepth(child, depth + 1, list);
        }

        private void AddItemAndChildren(SimpleTreeViewItem item, List<TreeViewItem> list)
        {
            item.depth = item.CalculateDepth();
            list.Add(item);
            foreach (var child in item.Children)
                AddItemAndChildren(child, list);
        }

        protected override bool CanStartDrag(CanStartDragArgs args) =>
            // Prevent dragging the RootItem
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

            // Prevent dragging RootChildren to the invisible root (i.e., outside RootItem)
            if (draggedRows.Any(item => RootChildren.Contains(item)) && (args.parentItem == null || args.parentItem is not SimpleTreeViewItem))
                return DragAndDropVisualMode.None;

            if (args.performDrop)
            {
                var newParent = args.parentItem as SimpleTreeViewItem;

                foreach (var draggedItem in draggedRows)
                {
                    if (draggedItem == RootItem)
                        continue; // Skip root item

                    if (newParent == RootItem) // Dropping on root item
                    {
                        // Make it a root child (Parent = null, add to RootChildren)
                        if (draggedItem.Parent != null)
                            draggedItem.Parent = null;
                        if (!RootChildren.Contains(draggedItem))
                            RootChildren.Add(draggedItem);
                    }
                    else if (newParent != null && newParent != RootItem) // Dropping on regular item
                    {
                        // Prevent cyclic parenting
                        if (!IsAncestor(draggedItem, newParent))
                        {
                            draggedItem.Parent = newParent;
                            RootChildren.Remove(draggedItem);
                        }
                    }
                }

                Reload();
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

        protected override bool CanRename(TreeViewItem item) => true;
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
            foreach (var child in RootChildren)
                if (!result.Contains(child))
                    CollectItems(child, result);
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