#region Using
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;
using UnityEngine.InputSystem;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System;
#endregion

namespace Nexerate.Nodes.Editor
{
    internal class NodeTreeView : TreeView
    {
        internal NodeAsset asset;
        internal Node root;
        Type nodeType;

        public NodeTreeView(NodeAsset nodeAsset, TreeViewState state) : base(state) 
        {
            asset = nodeAsset;

            #region Get Base Node Type From Generic Argument Of Node Asset Base Class
            Type type = asset.GetType().BaseType;

            while (type.BaseType != typeof(NodeAsset) && type.BaseType != null)
            {
                type = type.BaseType;
            }

            nodeType = type.GetGenericArguments()[0];
            #endregion

            InitializeRootAndAsset(asset);
            Undo.undoRedoPerformed -= HandleUndo;
            Undo.undoRedoPerformed += HandleUndo;
            Reload();
        }

        void RefreshEditor(bool clear = false)
        {
            if (Selection.activeObject != asset)
            {
                Selection.activeObject = asset;//Regive focus to the asset
            }
            NodeAssetEditor.RefreshEditor?.Invoke(clear);
        }

        protected override void SingleClickedItem(int id)
        {
            asset.SelectedID = id;
            RefreshEditor();
        }

        void InitializeRootAndAsset(NodeAsset nodeAsset)
        {
            asset = nodeAsset;
            root = nodeAsset.Root;
        }

        /// <summary>
        /// Called when an undo operation is performed.
        /// </summary>
        void HandleUndo()
        {
            //Asset is replaced with copy from undo stack.
            //Rebind the TreeView to the new asset located at the same path. 
            InitializeRootAndAsset((NodeAsset)AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GetAssetPath(asset)));
            ReImport();
            asset.Enable();//OnEnable is not called on the asset taken from the undo stack. Initialize manually
            RefreshEditor();
            Reload();
        }

        void ReImport() => AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem rootItem = new(0, -1, "Root");

            BuildRecursive(rootItem, root);

            SetupDepthsFromParentsAndChildren(rootItem);

            return rootItem;
        }

        //TODO (When supported, or in 2022 with UI Toolkit TreeView)
        //Disabled Nodes

        void BuildRecursive(TreeViewItem item, Node node)
        {
            TreeViewItem child = new(node.ID) { displayName = node.Name };

            //You cannot edit the children of this node. Either because it is locked, or because it is in a locked hierarchy
            if (node.ChildrenLocked || node.HierarchyLocked || node.IsInLockedHierarchy)
            {
                child.icon = (Texture2D)EditorGUIUtility.IconContent("InspectorLock").image;
            }
            //You cannot move this node, but you can add and remove children
            else if (node.ParentLocked)
            {
                child.icon = (Texture2D)EditorGUIUtility.IconContent("UnLinked").image;
            }
            //This node can be moved freely
            else
            {
                child.icon = (Texture2D)EditorGUIUtility.IconContent("Linked").image;
            }

            item.AddChild(child);
            for (int i = 0; i < node.ChildCount; i++)
            {
                BuildRecursive(child, node[i]);
            }
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();
            var draggedRows = GetRows().Where(item => args.draggedItemIDs.Contains(item.id)).ToList();
            DragAndDrop.SetGenericData("NodeDragging", draggedRows);
            DragAndDrop.StartDrag("Drag");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            List<TreeViewItem> items = (List<TreeViewItem>)DragAndDrop.GetGenericData("NodeDragging");

            var parent = args.parentItem;
            if (parent != null)
            {
                var node = asset.Find(parent.id);
                if (node != null)
                {
                    if (node.ChildrenLocked || node.HierarchyLocked || node.IsInLockedHierarchy) 
                    { 
                        return DragAndDropVisualMode.Rejected;
                    }
                }
            }

            //Perform drop
            if (args.performDrop)
            {
                CleanUpDroppedItems(items, args);
                Undo.RegisterCompleteObjectUndo(asset, "Reorder Nodes");

                //Create an array of nodes from the item list
                var nodes = new Node[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    nodes[i] = asset.Find(items[i].id);
                }

                if (args.dragAndDropPosition == DragAndDropPosition.OutsideItems)
                {
                    //Iterate over all of the items and set their parent to be the root
                    foreach (var node in nodes)
                    {
                        node.SetParent(root);
                    }
                }
                else
                {
                    //Because of a TreeView bug, we have to account for args.parentItem.id being the id of the root TreeViewItem
                    //This happens when we try to drop nodes right below the root node.
                    int insertIndex = parent.id == 0 ? 0 : args.insertAtIndex;
                    Node targetParent = parent.id == 0 ? root: asset.Find(parent.id);

                    //If node hierarchy is not recompiled, then asset.Find(id) might return null
                    //This, in turn, would mean that all selected nodes will have their parent set to null
                    //In other words, they will be deleted

                    if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
                    {
                        foreach (var node in nodes)
                        {
                            node.SetParent(targetParent);
                        }
                    }
                    else
                    {
                        for (int i = nodes.Length - 1; i >= 0; i--)
                        {
                            insertIndex = GetAdjustedInsertIndex(targetParent, nodes[i], insertIndex);
                            targetParent.InsertChild(insertIndex, nodes[i]);
                        }
                    }
                    SetExpanded(parent.id, true);
                }

                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(asset);
                Reload();
            }
            return DragAndDropVisualMode.Move;
        }

        /// <summary>
        /// Perform some checks on the items list and remove any items if necessary.
        /// </summary>
        void CleanUpDroppedItems(List<TreeViewItem> items, DragAndDropArgs args)
        {
            //Remove the root from our items. The root cannot be dragged nor dropped.
            items.Remove(items.Where(item => item.id == root.ID).FirstOrDefault());

            //We cannot parent a node to itself. We can however remove it from the items list and continue from there.
            //This also matches the behavior of the GameObject hierarchy.
            if (args.parentItem != null)
            {
                items.Remove(items.Where(item => item.id == args.parentItem.id).FirstOrDefault());
            }
        }

        int GetAdjustedInsertIndex(Node parent, Node child, int index)
        {
            //Need to account for parent index when moving outside of parent. Find ancestor below the parent
            //You will drop on and _then_ get the child index

            Node uppermostParent = child;

            int security = 0;

            while (uppermostParent.Parent != null && uppermostParent.Parent != parent && security < 50)
            {
                uppermostParent = uppermostParent.Parent;
                security++;
            }
            if (security >= 50) Debug.Log("Infinite loop!");

            if (uppermostParent.Parent != parent) return index;

            var childIndex = parent.IndexOf(uppermostParent);

            if (childIndex < index) index--;

            return index;
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            Node drag = asset.Find(args.draggedItem.id);
            if (args.draggedItem.id == root.ID) return false;
            if (drag.ParentLocked) return false;
            if (drag.Parent != null && drag.Parent.ChildrenLocked) return false;
            return true;
        }
        
        protected override bool CanRename(TreeViewItem item) => true;

        protected override void RenameEnded(RenameEndedArgs args)
        {
            string name = args.newName.Trim();
            if (args.acceptedRename && name != args.originalName && !string.IsNullOrEmpty(name))
            {
                Undo.RegisterCompleteObjectUndo(asset, "Rename Node");
                var node = asset.Find(args.itemID);
                node.Name = name;

                ReImport();

                Undo.FlushUndoRecordObjects();
                EditorUtility.SetDirty(asset);

                RefreshEditor();
                Reload();
            }
        }

        bool wasPressed = false;
        protected override void KeyEvent()
        {
            if (Keyboard.current.deleteKey.isPressed)
            {
                if (!wasPressed)
                {
                    wasPressed = true;
                    Delete();
                }
            }
            else if (Keyboard.current.ctrlKey.isPressed)
            {
                if (Keyboard.current.dKey.isPressed)
                {
                    if (!wasPressed)
                    {
                        wasPressed = true;
                        Duplicate();
                    }
                }
                else
                {
                    wasPressed = false;
                }
            }
            else
            {
                wasPressed = false;
            }

            base.KeyEvent();
        }

        void Duplicate()
        {
            var nodes = new List<Node>();
            var selection = GetSelection().ToList();
            
            //Root cannot be duplicated
            //All rows will also have root as ancestor, so we must remove it for the validation to work
            selection.Remove(root.ID);

            for (int i = 0; i < selection.Count; i++)
            {
                //Debug.Log(selection[i]);
                nodes.Add(root.Find(selection[i]));
            }
            for (int i = nodes.Count - 1; i >= 0; i--)
            {
                //Remove nodes with ancestors in the selection
                //Only uppermost nodes should be duplicated
                var ancestors = nodes[i].GetAncestors();
                if (ancestors.Intersect(nodes).Any())
                {
                    nodes.RemoveAt(i);
                }
            }

            Undo.RegisterCompleteObjectUndo(asset, "Duplicate Node");
            for (int i = 0; i < nodes.Count; i++)
            {
                var duplicate = nodes[i].Duplicate();
                duplicate.SetParent(nodes[i].Parent);
            }
            Undo.FlushUndoRecordObjects();
            EditorUtility.SetDirty(asset);

            ReImport();
            Reload();
        }

        void Delete()
        {
            Undo.RegisterCompleteObjectUndo(asset, "Delete Node");

            var selection = GetSelection();

            for (int i = 0; i < selection.Count; i++)
            {
                var node = asset.Find(selection[i]);
                node?.SetParent(null);
            }

            Undo.FlushUndoRecordObjects();
            EditorUtility.SetDirty(asset);

            ReImport();
            RefreshEditor(true);
            Reload();
        }

        protected override void ContextClickedItem(int id)
        {
            Node target = root.Find(id);

            GenericMenu menu = new();

            if (!target.ChildrenLocked && !target.HierarchyLocked && !target.IsInLockedHierarchy) 
            {
                #region Add Nodes
                var assemblies = AppDomain.CurrentDomain.GetAssemblies();

                var types = new List<Type>();

                for (int i = 0; i < assemblies.Length; i++)
                {
                    //Add all types from this assembly that derive from nodeType
                    types.AddRange(assemblies[i].GetTypes().Where(t => nodeType.IsAssignableFrom(t)));
                }

                foreach (var type in types)
                {
                    //Only types with [AddNodeMenuAttribute] will be added to the menu.
                    AddNodeMenuAttribute attribute = type.GetCustomAttribute<AddNodeMenuAttribute>();
                    if (attribute != null && !type.IsAbstract)
                    {
                        var menuName = attribute.MenuName;
                        menu.AddItem(new(menuName), false, () =>
                        {
                            Undo.RegisterCompleteObjectUndo(asset, "Add Node");
                            Node child = (Node)Activator.CreateInstance(type);
                            child.SetParent(target);

                            Undo.FlushUndoRecordObjects();
                            EditorUtility.SetDirty(asset);

                            SetExpanded(id, true);
                            ReImport();
                            Reload();
                        });
                    }
                }
                #endregion

                #region Add Separator
                if (types != null && types.Count > 0)
                {
                    menu.AddSeparator("");
                } 
                #endregion
            }

            #region Rename
            menu.AddItem(new("Rename"), false, () =>
            {
                BeginRename(FindItem(id, rootItem));
            });
            #endregion

            bool allowedToModifyParent = target != root && !target.ParentLocked && !target.Parent.ChildrenLocked && !target.Parent.HierarchyLocked;

            #region Duplicate
            if (target != root && allowedToModifyParent)
            {
                menu.AddItem(new("Duplicate"), false, Duplicate);
            }
            #endregion

            #region Delete
            if (target != root && allowedToModifyParent)
            {
                menu.AddItem(new("Delete"), false, Delete);
            }
            #endregion

            menu.ShowAsContext();
        }
    }
}