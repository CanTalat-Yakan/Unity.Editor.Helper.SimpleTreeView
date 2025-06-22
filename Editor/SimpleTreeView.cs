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
        public SimpleTreeViewItem Parent
        {
            get => parent as SimpleTreeViewItem;
            set
            {
                if (Parent == value)
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
        public SimpleTreeViewItem(string displayName) : base(GenerateUniqueId(), 1, displayName) { }
        public SimpleTreeViewItem(int id, string displayName) : base(id, 1, displayName) { }
        public SimpleTreeViewItem(int id, int depth, string displayName) : base(id, depth, displayName) { }

        private static string GetDefaultDisplayName(string displayName = null) =>
            string.IsNullOrEmpty(displayName) ? "TreeViewItem" : displayName;

        private static int GenerateUniqueId() =>
            System.Guid.NewGuid().GetHashCode();
    }

    public class SimpleTreeView : TreeView
    {
        public TreeViewState TreeViewState;

        public SimpleTreeViewItem RootItem { get; private set; }
        public List<SimpleTreeViewItem> AllTreeViewItems { get; private set; } = new();

        public SimpleTreeView(SimpleTreeViewItem[] rootChildren = null,
                              string rootName = "Root",
                              int rowHeight = 20,
                              bool showBorder = false,
                              bool showAlternatingRowBackgrounds = false)
            : base(new TreeViewState())
        {
            TreeViewState = state;

            RootItem = new SimpleTreeViewItem(0, 0, rootName);
            if (rootChildren != null)
                AddChildren(rootChildren);

            base.rowHeight = rowHeight;
            base.showBorder = showBorder;
            base.showAlternatingRowBackgrounds = showAlternatingRowBackgrounds;

            Reload();
        }

        public void AddChildren(SimpleTreeViewItem[] rootChildren)
        {
            AllTreeViewItems = rootChildren.ToList();
            foreach (var child in AllTreeViewItems)
                child.Parent = RootItem;

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
                        continue;

                    if (IsAncestor(draggedItem, newParent))
                        continue;

                    draggedItem.Parent = newParent;
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
            foreach (var child in AllTreeViewItems)
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