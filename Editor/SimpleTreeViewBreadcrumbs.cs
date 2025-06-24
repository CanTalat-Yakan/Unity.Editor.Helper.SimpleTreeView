using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UnityEssentials
{
    public static class SimpleTreeViewBreadcrumbs
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

            GUILayout.BeginHorizontal();
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
                        {
                            string displayName = child.displayName;
                            if (!displayNameCache.Contains(displayName))
                                displayNameCache.Add(displayName);
                            else
                            {
                                int increment = 1;
                                while (displayNameCache.Contains($"{child.displayName} ({increment})"))
                                    increment++;

                                displayName = $"{child.displayName} ({increment})";
                                displayNameCache.Add(displayName);
                            }

                            menu.AddItem(new GUIContent(displayName), child == item, () => onClick?.Invoke(child));
                        }

                        menu.DropDown(buttonRect);
                    }
                }

                var style = i < chain.Count - 1 ? EditorStyles.label : EditorStyles.boldLabel;
                if (GUILayout.Button(item.displayName, style))
                    onClick?.Invoke(item);
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
        }
    }
}
