# Unity Essentials

This module is part of the Unity Essentials ecosystem and follows the same lightweight, editor-first approach.
Unity Essentials is a lightweight, modular set of editor utilities and helpers that streamline Unity development. It focuses on clean, dependency-free tools that work well together.

All utilities are under the `UnityEssentials` namespace.

```csharp
using UnityEssentials;
```

## Installation

Install the Unity Essentials entry package via Unity's Package Manager, then install modules from the Tools menu.

- Add the entry package (via Git URL)
    - Window → Package Manager
    - "+" → "Add package from git URL…"
    - Paste: `https://github.com/CanTalat-Yakan/UnityEssentials.git`

- Install or update Unity Essentials packages
    - Tools → Install & Update UnityEssentials
    - Install all or select individual modules; run again anytime to update

---

# Simple Tree View

> Quick overview: A lightweight, IMGUI-based TreeView wrapper for editor tools: drag-and-drop reparenting/reorder, unique naming, rename, selection helpers, per-item and global context menus, and a breadcrumb UI helper.

A small wrapper around `UnityEditor.IMGUI.Controls.TreeView` that gives you a batteries-included hierarchical UI for custom editor windows. It provides a friendly `SimpleTreeViewItem` model, optional unique-name enforcement, drag-and-drop reparenting with type filtering, and a convenient breadcrumbs bar.

![screenshot](Documentation/Screenshot.png)

## Features
- Simple API around Unity's `TreeView`
  - `SimpleTreeView` to host and draw; `SimpleTreeViewItem` to model nodes
  - Configurable constructor: `rootName`, `rowHeight`, `showBorder`, `showAlternatingRowBackgrounds`, `allowDuplicateNames`
- Selection and rename helpers
  - `GetSelectedItem()`, `GetSelectedItems()`, `SetSelectedItems(params int[])`, `ClearAllSelections()`
  - `BeginRename(int id)`, `OnRename` callback after name changes
  - Custom rename rect for a smooth inline experience
- Unique naming per parent
  - Auto-appends "(n)" to avoid sibling name collisions when duplicates are disallowed
- Drag-and-drop built-in
  - Reparent and reorder nodes with visual feedback
  - Prevents illegal drops (root, ancestors, parents that disallow children, or type-incompatible)
- Type filtering for children
  - On a parent: `item.Support(typeof(MyTypeA), typeof(MyTypeB))`
  - On a child: set `UserData` to an instance; drop allowed only if `child.UserData.GetType()` is in the parent's supported types
  - If a parent has no `SupportsTypes`, all types are allowed
- Per-item and global context menus
  - `SimpleTreeViewItem.SetContextMenu(GenericMenu)` for items
  - `SimpleTreeView.GlobalContextMenu` for global actions on containers
  - Built-in "Rename" and "Delete" entries where supported
- Breadcrumbs helper
  - `SimpleTreeViewItemBreadcrumbs.Draw(current, onClick)` draws a toolbar path with sibling jump menus
- Visual polish
  - Styled root row background, proper indenting, icons via `SetIcon(Texture2D)`

## Requirements
- Unity Editor 6000.0+ (Editor-only; no runtime code)
- Uses `UnityEditor.IMGUI.Controls` (TreeView)

## Usage

Minimal editor window with a tree, context menu, drag-and-drop, and breadcrumbs.

```csharp
using UnityEditor;
using UnityEngine;
using UnityEssentials;

public class SimpleTreeViewExample : EditorWindow
{
    private SimpleTreeView _tree;

    [MenuItem("Window/Examples/Simple TreeView Example")]
    private static void Open() => GetWindow<SimpleTreeViewExample>("Simple TreeView");

    private void OnEnable()
    {
        _tree = new SimpleTreeView(rootName: "Root", rowHeight: 18, showBorder: true, showAlternatingRowBackgrounds: true);

        // Global actions available on containers
        var global = new GenericMenu();
        global.AddItem(new GUIContent("Create/Folder"), false, () => CreateFolder(_tree.RootItem));
        global.AddItem(new GUIContent("Create/Item"), false, () => CreateItem(_tree.RootItem));
        _tree.GlobalContextMenu = global;

        // Add a few nodes
        var a = new SimpleTreeViewItem()
            .SetIcon(EditorGUIUtility.IconContent("Folder Icon").image as Texture2D)
            .SetName("Folder A")
            .Support(allowChildren: true, allowRenaming: true)
            .Support(typeof(string)); // only accepts children whose UserData is string

        var b = new SimpleTreeViewItem()
            .SetIcon(EditorGUIUtility.IconContent("d_TextAsset Icon").image as Texture2D)
            .SetName("Item B")
            .Support(allowChildren: false) // leaf
            .SetUserData("payload");       // child type is string

        _tree.AddItem(a);
        _tree.AddItem(b, parent: a.id);

        _tree.OnRename += item => Debug.Log($"Renamed: {item.UniqueName}");
    }

    private void OnGUI()
    {
        // Optional: breadcrumb bar for current selection
        var current = _tree.GetSelectedItem() ?? _tree.RootItem;
        SimpleTreeViewItemBreadcrumbs.Draw(current, item => _tree.SetSelectedItems(item.id));

        _tree.PreProcess(); // mouse/context selection tweaks
        _tree.Draw();       // draws the entire TreeView

        // Example footer with actions
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("New Folder")) CreateFolder(_tree.GetSelectedItem() ?? _tree.RootItem);
            if (GUILayout.Button("New Item"))   CreateItem(_tree.GetSelectedItem() ?? _tree.RootItem);
        }
    }

    private void CreateFolder(SimpleTreeViewItem parent)
    {
        var folder = new SimpleTreeViewItem()
            .SetIcon(EditorGUIUtility.IconContent("Folder Icon").image as Texture2D)
            .SetName("Folder")
            .Support(allowChildren: true)
            .Support(typeof(string));
        _tree.AddItem(folder, parent: parent.id);
        _tree.BeginRename(folder.id);
    }

    private void CreateItem(SimpleTreeViewItem parent)
    {
        var item = new SimpleTreeViewItem()
            .SetIcon(EditorGUIUtility.IconContent("d_TextAsset Icon").image as Texture2D)
            .SetName("Item")
            .Support(allowChildren: false)
            .SetUserData("payload"); // must match a type supported by parent (string here)
        _tree.AddItem(item, parent: parent.id);
        _tree.BeginRename(item.id);
    }
}
```

### Key concepts
- Root and depth
  - `SimpleTreeView.RootItem` is always present; it can’t be dragged or deleted
- Names and uniqueness
  - `SetName("X", unique: true)` auto-adjusts to `X (n)` if siblings already have `X`
  - Pass `allowDuplicateNames: true` to the `SimpleTreeView` ctor to display raw names
- Type filtering
  - Parent calls `item.Support(typeof(TypeA), typeof(TypeB))`
  - Child sets `item.SetUserData(object)`; the user data’s runtime type must match an allowed type on the parent
- Context menus
  - Right‑click an item to get built-ins (Rename/Delete) and your added menus
  - Add per-item menus via `SetContextMenu(GenericMenu)`; add global menus via `GlobalContextMenu`
- Drag and drop
  - Works out of the box; illegal operations are ignored (dragging root, reparenting into descendants, parents without children, or type-incompatible)

## Notes and Limitations
- Editor-only: not included in player builds
- UI framework: IMGUI TreeView (not UIToolkit)
- Performance: suitable for small to mid-size trees; very large trees are not virtualized
- Selection: clicking empty area clears selection by design
- Root item: cannot be removed or moved

## Files in This Package
- `Editor/SimpleTreeView.cs` – Host, drawing, selection, rename, drag-and-drop, context menus
- `Editor/SimpleTreeViewItem.cs` – Node model (name uniqueness, icon, user data, type filtering, parenting)
- `Editor/SimpleTreeViewItemBreadcrumbs.cs` – Breadcrumbs toolbar helper with sibling dropdowns
- `Editor/UnityEssentials.SimpleTreeView.Editor.asmdef` – Editor assembly definition
- `package.json` – Package manifest metadata

## Tags
unity, unity-editor, treeview, imgui, hierarchy, drag-and-drop, context-menu, breadcrumbs, rename, utility
