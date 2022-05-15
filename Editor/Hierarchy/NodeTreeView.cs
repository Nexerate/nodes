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
        #region Fields
        NodeAsset asset;
        Type nodeType;
        Node root;
        #endregion

        public NodeTreeView(NodeAsset nodeAsset, TreeViewState state) : base(state) 
        {
            asset = nodeAsset;

            #region Get Base Node Type From Generic Argument Of Node Asset Base Class
            var type = asset.GetType().BaseType;

            while (type.BaseType != typeof(NodeAsset) && type.BaseType != null)
            {
                type = type.BaseType;
            }

            nodeType = type.GetGenericArguments()[0];
            #endregion

            InitializeRootAndAsset(asset);
            Undo.undoRedoPerformed -= HandleUndo;
            Undo.undoRedoPerformed += HandleUndo;

            //Make sure the asset hierarchy is built before we try to display it
            asset.RebuildHierarchy();
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

        //protected override void SingleClickedItem(int id) => RefreshEditor();

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
            InitializeRootAndAsset(AssetDatabase.LoadAssetAtPath<NodeAsset>(AssetDatabase.GetAssetPath(asset)));
            //ReImport();
            asset.Enable();//OnEnable is not called on the asset taken from the undo stack. Initialize manually
            RefreshEditor();
            Reload();
        }

        //void ReImport() => AssetDatabase.ImportAsset(AssetDatabase.GetAssetPath(asset));

        protected override TreeViewItem BuildRoot()
        {
            TreeViewItem rootItem = new(0, -1, "Root");

            BuildRecursive(rootItem, root, 0);

            return rootItem;
        }

        //TODO (When supported, or in 2022 with UI Toolkit TreeView)
        //Disabled Nodes

        void BuildRecursive(TreeViewItem item, Node node, int depth)
        {
            TreeViewItem child = new(node.ID, depth, node.Name);

            UpdateIcon(child, node);

            item.AddChild(child);
            for (int i = 0; i < node.ChildCount; i++)
            {
                BuildRecursive(child, node[i], depth + 1);
            }
        }

        void UpdateIcon(TreeViewItem item, Node node)
        {
            //You cannot edit the children of this Node. Either because it is locked, or because it is in a locked hierarchy
            if (node.ChildrenLocked || node.HierarchyLocked || node.IsInLockedHierarchy)
            {
                item.icon = IconCache.Lock;
                return;
            }

            //Set node icon based on parent LockState
            item.icon = node.ParentLocked ? IconCache.Linked : IconCache.Node;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            SetSelection(args.draggedItemIDs);
            DragAndDrop.PrepareStartDrag();
            var draggedRows = GetRows().Where(item => args.draggedItemIDs.Contains(item.id)).ToList();
            DragAndDrop.SetGenericData("NodeDragging", draggedRows);
            DragAndDrop.StartDrag("Drag");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            List<TreeViewItem> items = (List<TreeViewItem>)DragAndDrop.GetGenericData("NodeDragging");

            var parent = args.parentItem;

            if (parent != null && parent.icon == IconCache.Lock) return DragAndDropVisualMode.Rejected;

            //Stop here if we aren't dropping any items
            if (!args.performDrop) return DragAndDropVisualMode.Move;

            CleanUpDroppedItems(items, args);

            PerformUndoableAction(() =>
            {
                //Dragged nodes will be parented to the target
                Node target = parent == null || parent.id == 0 ? root: asset.Find(parent.id);

                //Create an array of nodes from the item list
                var nodes = new Node[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    nodes[i] = asset.Find(items[i].id);
                }

                //If we drag outside the hierarchy, set the parent of all selected nodes to the root
                if (args.dragAndDropPosition == DragAndDropPosition.OutsideItems)
                {
                    foreach (var node in nodes)
                    {
                        node.SetParent(target);
                    }

                    return;
                }


                if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
                {
                    foreach (var node in nodes)
                    {
                        node.SetParent(target);
                    }

                    SetExpanded(parent.id, true);
                    return;
                }

                //Because of a TreeView bug, we have to account for args.parentItem.id being the id of the root TreeViewItem
                //This happens when we try to drop nodes right below the root node.
                int index = parent.id == 0 ? 0 : args.insertAtIndex;

                var anchorNode = index >= target.ChildCount? null : target[index];

                if (nodes.Contains(anchorNode)) return;

                for (int i = 0; i < nodes.Length; i++)
                {
                    nodes[i].SetParent(null);
                }

                index = anchorNode == null? target.ChildCount : target.IndexOf(anchorNode);

                for (int i = 0; i < nodes.Length; i++)
                {
                    target.InsertChild(index + i, nodes[i]);
                }

                SetExpanded(parent.id, true);
            }, "Reorder Node");

            RefreshEditor();

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

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            base.SelectionChanged(selectedIds);
            RefreshEditor();
        }

        /// <summary>
        /// Show a menu when the hierarchy window is clicked.
        /// </summary>
        protected override void ContextClicked() => ShowNodeMenu();
        
        /// <summary>
        /// Show a context menu when a node is right clicked.
        /// </summary>
        protected override void ContextClickedItem(int id) => ShowNodeMenu(asset.Find(id));
        
        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            Node drag = asset.Find(args.draggedItem.id);
            if (drag == null) return false;
            if (drag.ID == root.ID) return false;
            if (drag.ParentLocked) return false;
            return true;
        }
        
        protected override bool CanRename(TreeViewItem item) => true;

        protected override void RenameEnded(RenameEndedArgs args)
        {
            string name = args.newName.Trim();
            if (args.acceptedRename && name != args.originalName && !string.IsNullOrEmpty(name))
            {
                PerformUndoableAction(() =>
                {
                    asset.Find(args.itemID).Rename(name);
                }, "Rename Node");
                RefreshEditor();
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
                    DeleteSelectedNodes();
                }
            }
            else if (Keyboard.current.ctrlKey.isPressed)
            {
                if (Keyboard.current.dKey.isPressed)
                {
                    if (!wasPressed)
                    {
                        wasPressed = true;
                        DuplicateSelectedNodes();
                    }
                }
                else if (Keyboard.current.cKey.isPressed)
                {
                    if (!wasPressed)
                    {
                        wasPressed = true;
                        Copy();
                    }
                }
                else if (Keyboard.current.vKey.isPressed)
                {
                    if (!wasPressed)
                    {
                        wasPressed = true;
                        var selectedNode = asset.Find(state.lastClickedID) ?? root;
                        Paste(selectedNode);
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

        List<Node> GetSelectedNodes()
        {
            var nodes = new List<Node>();
            var selection = GetSelection().ToList();

            for (int i = 0; i < selection.Count; i++)
            {
                nodes.Add(root.Find(selection[i]));
            }
            return nodes;
        }

        #region Duplicate
        void DuplicateSelectedNodes()
        {
            var nodes = GetSelectedNodes().Where(node => node != root && !node.ParentLocked).ToList();
            var selection = GetSelection().ToList();

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

            PerformUndoableAction(() =>
            {
                for (int i = 0; i < nodes.Count; i++)
                {
                    var duplicate = nodes[i].Duplicate();
                    duplicate.SetParent(nodes[i].Parent);
                }
            }, "Duplicate Node");

            RefreshEditor();
        }
        #endregion

        #region Delete
        void DeleteSelectedNodes()
        {
            PerformUndoableAction(() =>
            {
                var selection = GetSelectedNodes();

                foreach (var node in selection)
                {
                    node?.SetParent(null);
                }
            }, "Delete Node");

            RefreshEditor(true);
        } 
        #endregion

        void ShowNodeMenu(Node target = null)
        {
            //Menu has already been opened from somewhere else
            if (Event.current.commandName == "NodeMenu") return;

            //Tell the current event that we are opening the Node Menu
            Event.current.commandName = "NodeMenu";

            bool nodeSelected = true;
            if (target == null)
            {
                target = root;
                nodeSelected = false;
            }
            int id = target.ID;
            GenericMenu menu = new();

            if (!target.ChildrenLocked && !target.HierarchyLocked && !target.IsInLockedHierarchy)
            {
                #region Add Nodes
                var types = Cache.NodeCache.Where(t => nodeType.IsAssignableFrom(t));

                foreach (var type in types)
                {
                    //Only types with [AddNodeMenuAttribute] will be added to the menu.
                    AddNodeMenuAttribute attribute = type.GetCustomAttribute<AddNodeMenuAttribute>();
                    if (attribute != null && !type.IsAbstract)
                    {
                        menu.AddItem(new(attribute.MenuName), false, () =>
                        {
                            PerformUndoableAction(() =>
                            {
                                Node child = (Node)Activator.CreateInstance(type);
                                child.SetParent(target);
                            }, "Add Node");

                            RefreshEditor();
                            SetExpanded(id, true);
                        });
                    }
                }
                #endregion

                #region Add Separator
                if (nodeSelected && types != null && types.Count() > 0)
                {
                    menu.AddSeparator("");
                }
                #endregion
            }

            //Only show this part of the menu if a node is actually selected
            if (nodeSelected)
            {
                #region Rename
                menu.AddItem(new("Rename"), false, () =>
                {
                    BeginRename(FindItem(id, rootItem));
                });
                #endregion

                bool canModifyParent = target != root && !target.ParentLocked;

                #region Duplicate
                if (target != root && canModifyParent)
                {
                    menu.AddItem(new("Duplicate"), false, DuplicateSelectedNodes);
                }
                #endregion

                #region Delete
                if (target != root && canModifyParent)
                {
                    menu.AddItem(new("Delete"), false, DeleteSelectedNodes);
                }
                #endregion
            }

            menu.AddSeparator("");

            if (nodeSelected && target != root && !target.ParentLocked)
            {
                menu.AddItem(new("Cut"), false, () =>
                {
                    Copy();
                    DeleteSelectedNodes();
                });

                menu.AddItem(new("Copy"), false, () =>
                {
                    Copy();
                });
            }
            else
            {
                menu.AddDisabledItem(new("Cut"));
                menu.AddDisabledItem(new("Copy"));
            }

            if (!target.ChildrenLocked && !target.HierarchyLocked && NodeEditorUtility.HasNodesInClipboard)
            {
                menu.AddItem(new("Paste"), false, () =>
                {
                    Paste(target);
                });
            }
            else
            {
                menu.AddDisabledItem(new("Paste"));
            }

            menu.ShowAsContext();
        }

        #region Copy/Paste
        void Copy()
        {
            var selection = GetSelectedNodes();
            selection.Remove(root);
            NodeEditorUtility.Copy(selection);
        }

        void Paste(Node target)
        {
            PerformUndoableAction(() =>
            {
                NodeEditorUtility.Paste(target);
            }, "Paste Node");
            SetExpanded(target.ID, true);
            RefreshEditor();
        } 
        #endregion

        /// <summary>
        /// Perform an action on the asset, that can be undone.
        /// </summary>
        void PerformUndoableAction(Action action, string undoMessage = "Undo")
        {
            Undo.RecordObject(asset, undoMessage);
            action.Invoke();
            EditorUtility.SetDirty(asset);

            //ReImport();
            Reload();
        }
    }
}