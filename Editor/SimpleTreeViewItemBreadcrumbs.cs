#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public static class SimpleTreeViewItemBreadcrumbs
    {
        public static void Draw(SimpleTreeViewItem current, System.Action<SimpleTreeViewItem> onClick = null)
        {
            if (current == null)
                return;

            var chain = new List<SimpleTreeViewItem>();
            var node = current;
            while (node != null)
            {
                chain.Insert(0, node);
                node = node.Parent;
            }

            var displayNameCache = new List<string>();

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                for (int i = 0; i < chain.Count; i++)
                {
                    var item = chain[i];

                    if (i > 0)
                    {
                        var icon = EditorGUIUtility.IconContent("tab_next").image;
                        if (GUILayout.Button(icon, EditorStyles.label))
                        {
                            var buttonRect = GUILayoutUtility.GetLastRect();
                            buttonRect.y += 20;
                            buttonRect.x = Event.current.mousePosition.x;

                            var menu = new GenericMenu();
                            foreach (var child in item.Parent.Children)
                                menu.AddItem(new GUIContent(child.displayName), child == item, () => onClick?.Invoke(child));
                            menu.DropDown(buttonRect);
                        }
                    }

                    var labelStyle = i < chain.Count - 1 ? EditorStyles.label : EditorStyles.boldLabel;
                    if (GUILayout.Button(item.displayName, labelStyle))
                        onClick?.Invoke(item);
                }

                GUILayout.FlexibleSpace();
            }
        }
    }
}
#endif