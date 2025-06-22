using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEssentials
{
    public class SimpleTreeViewItem : TreeViewItem
    {
        public SimpleTreeViewItem() { }
        public SimpleTreeViewItem(int id, int depth, string displayName) : base(id, depth, displayName) { }
    }

    public class SimpleTreeView : TreeView
    {
        public TreeViewState TreeViewState;
        public List<SimpleTreeViewItem> Items;

        public SimpleTreeView(List<SimpleTreeViewItem> items,
            int rowHeight = 20,
            bool showBorder = false,
            bool showAlternatingRowBackgrounds = false) : base(new TreeViewState())
        {
            TreeViewState = state;
            Items = items;
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
            SetupParentsAndChildrenFromDepths(root, Items.Cast<TreeViewItem>().ToList());
            SetupDepthsFromParentsAndChildren(root);
            return root;
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
            DragAndDrop.objectReferences = new UnityEngine.Object[] { }; // Required, but not used
            DragAndDrop.SetGenericData("TreeViewDragging", draggedRows);
            DragAndDrop.StartDrag("Dragging TreeView");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var draggedRows = DragAndDrop.GetGenericData("TreeViewDragging") as List<SimpleTreeViewItem>;
            if (draggedRows == null)
                return DragAndDropVisualMode.None;

            if (args.performDrop)
            {
                foreach (var draggedItem in draggedRows)
                    Items.RemoveAll(i => i.id == draggedItem.id);

                int insertIndex = args.parentItem == null || args.parentItem.id == 0
                    ? args.insertAtIndex
                    : Items.FindIndex(i => i.id == args.parentItem.id) + 1;

                if (insertIndex < 0 || insertIndex > Items.Count)
                    insertIndex = Items.Count;

                Items.InsertRange(insertIndex, draggedRows);
                Reload();
            }
            return DragAndDropVisualMode.Move;
        }

        protected override bool CanRename(TreeViewItem item) => true;
        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (args.acceptedRename)
            {
                var item = Items.FirstOrDefault(i => i.id == args.itemID);
                if (item != null)
                {
                    item.displayName = args.newName;
                    Reload();
                }
            }
        }
    }
}
