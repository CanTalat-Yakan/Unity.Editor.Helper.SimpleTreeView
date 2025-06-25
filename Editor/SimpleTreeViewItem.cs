#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace UnityEssentials
{
    public class SimpleTreeViewItem : TreeViewItem
    {
        public bool SupportsChildren = true;
        public bool SupportsRenaming = true;

        public SimpleTreeViewItem Support(bool allowChildren = true, bool allowRenaming = true)
        {
            SupportsChildren = allowChildren;
            SupportsRenaming = allowRenaming;
            return this;
        }

        public Texture Icon => icon;
        public SimpleTreeViewItem SetIcon(Texture2D icon)
        {
            this.icon = icon;
            return this;
        }

        public string Name => _name;
        private string _name;
        public string UniqueName => _uniqueName;
        private string _uniqueName;
        public SimpleTreeViewItem SetName(string name, bool unique = true)
        {
            _name = name;
            //if (string.IsNullOrEmpty(_uniqueName))
            _uniqueName = name;

            GetUniqueName();

            displayName = unique ? UniqueName : Name;
            if (Name == string.Empty)
                displayName = Name;

            return this;
        }

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
                };
            }
        }
        public SimpleTreeViewItem SetParent(SimpleTreeViewItem parent)
        {
            Parent = parent;
            return this;
        }

        public int ChildCount => children?.Count ?? 0;
        public bool HasChildren => ChildCount > 0;
        public SimpleTreeViewItem GetChild(int index) => children?[index] as SimpleTreeViewItem;
        public IReadOnlyList<SimpleTreeViewItem> Children =>
            children?.Cast<SimpleTreeViewItem>().ToList() ?? new List<SimpleTreeViewItem>();

        public SimpleTreeViewItem() : base(Guid.NewGuid().GetHashCode(), 1, "TreeViewItem") { }

        private void GetUniqueName()
        {
            if (string.IsNullOrEmpty(Name))
                _uniqueName = UserData?.GetType().Name ?? "FALLBACK";

            if (parent == null)
                return;

            var nameCache = new HashSet<string>();
            foreach (var sibling in Parent.Children)
                if (sibling != this)
                    nameCache.Add(sibling.UniqueName);

            if (!nameCache.Contains(UniqueName))
                return;

            int increment = 1;
            while (nameCache.Contains($"{Name} ({increment})"))
                increment++;

            _uniqueName = $"{Name} ({increment})";
        }
    }
}
#endif