#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEssentials
{
    public class SimpleTreeViewItem : TreeViewItem
    {
        public SimpleTreeViewItem Parent { get; private set; }
        public List<SimpleTreeViewItem> Children { get; private set; } = new List<SimpleTreeViewItem>();

        public SimpleTreeViewItem() { }
        public SimpleTreeViewItem(int id, string displayName) : base(id, 0, displayName) { }

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
        public List<SimpleTreeViewItem> RootItems;

        public SimpleTreeView(List<SimpleTreeViewItem> rootItems,
            int rowHeight = 20,
            bool showBorder = false,
            bool showAlternatingRowBackgrounds = false) : base(new TreeViewState())
        {
            TreeViewState = state;
            RootItems = rootItems;
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
            foreach (var item in RootItems)
                AddItemAndChildren(item, allItems);
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
                if (newParent == null || newParent.id == 0)
                {
                    // Drop at root
                    foreach (var draggedItem in draggedRows)
                    {
                        if (draggedItem.Parent != null)
                            draggedItem.Parent.RemoveChild(draggedItem);
                        if (!RootItems.Contains(draggedItem))
                            RootItems.Add(draggedItem);
                    }
                }
                else
                {
                    foreach (var draggedItem in draggedRows)
                    {
                        if (draggedItem.Parent != null)
                            draggedItem.Parent.RemoveChild(draggedItem);
                        if (RootItems.Contains(draggedItem))
                            RootItems.Remove(draggedItem);
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
            if (args.acceptedRename)
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
            foreach (var root in RootItems)
                CollectItems(root, result);
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