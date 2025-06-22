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
        public SimpleTreeViewItem Parent { get; set; }
        public List<SimpleTreeViewItem> Children { get; private set; } = new();

        public SimpleTreeViewItem() : base(GenerateUniqueId(), 0, GetDefaultDisplayName()) { }
        public SimpleTreeViewItem(string displayName) : base(GenerateUniqueId(), 0, GetDefaultDisplayName(displayName)) { }

        public static string GetDefaultDisplayName(string displayName = null) =>
            string.IsNullOrEmpty(displayName) ? "TreeViewItem" : displayName;

        public static int GenerateUniqueId() =>
            System.Guid.NewGuid().GetHashCode();

        public void AddChildren(params SimpleTreeViewItem[] children)
        {
            foreach (var child in children)
                AddChild(child);
        }

        public void AddChild(SimpleTreeViewItem child)
        {
            if (child.Parent != null)
                child.Parent.RemoveChild(child);
            child.Parent = this;
            Children.Add(child);
        }

        public void RemoveChild(SimpleTreeViewItem child)
        {
            if (Children.Remove(child))
                child.Parent = null;
        }

        public int CalculateDepth()
        {
            int depth = -1;
            var current = this;
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

        public IList<SimpleTreeViewItem> RootChildren => RootItem.Children;

        public SimpleTreeView(SimpleTreeViewItem rootItem,
            int rowHeight = 20,
            bool showBorder = false,
            bool showAlternatingRowBackgrounds = false) : base(new TreeViewState())
        {
            TreeViewState = state;
            RootItem = rootItem ?? new SimpleTreeViewItem("Root");
            RootItem.Parent = null;
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
            // The TreeView expects a root with depth -1 and id 0
            var root = new TreeViewItem { id = 0, depth = -1, displayName = "Root" };
            var allItems = new List<TreeViewItem>();

            // Ensure RootItem has depth 0 and id != 0
            RootItem.depth = 0;
            if (RootItem.id == 0)
                RootItem.id = SimpleTreeViewItem.GenerateUniqueId();

            AddItemAndChildren(RootItem, allItems);
            SetupParentsAndChildrenFromDepths(root, allItems);
            return root;
        }

        private void AddItemAndChildren(SimpleTreeViewItem item, List<TreeViewItem> list)
        {
            item.depth = item.CalculateDepth();
            list.Add(item);
            foreach (var child in item.Children)
                AddItemAndChildren(child, list);
        }

        protected override bool CanStartDrag(CanStartDragArgs args) => true;
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
                if (newParent == null || newParent == RootItem)
                {
                    // Drop at root (as children of RootItem)
                    foreach (var draggedItem in draggedRows)
                    {
                        if (draggedItem.Parent != null)
                            draggedItem.Parent.RemoveChild(draggedItem);
                        if (!RootItem.Children.Contains(draggedItem))
                            RootItem.AddChild(draggedItem);
                    }
                }
                else
                {
                    foreach (var draggedItem in draggedRows)
                    {
                        if (draggedItem.Parent != null)
                            draggedItem.Parent.RemoveChild(draggedItem);
                        if (RootItem.Children.Contains(draggedItem))
                            RootItem.Children.Remove(draggedItem);
                        newParent.AddChild(draggedItem);
                    }
                }
                Reload();
            }
            return DragAndDropVisualMode.Move;
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
                    if (string.IsNullOrEmpty(args.newName))
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