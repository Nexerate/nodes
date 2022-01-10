# Nexerate Nodes
Nexerate Nodes is a framework for creating node based hierarchy tools. 

How to install through package manager:
```

```

# Basic Setup:
```csharp
using Nexerate.Nodes;
using System;

[Serializable]
[AddNodeMenu("Example/Node")]
public class ExampleNode : Node 
{

}
```

```csharp
using Nexerate.Nodes;
using UnityEngine;

[CreateAssetMenu(fileName = "Example Hierarchy", menuName = "Example/Example Hierarchy")]
public class ExampleHierarchy : NodeAsset<ExampleNode> 
{

}
```

# Features:
- ## Node Asset
    - A `NodeAsset` is the `ScriptableObject` that holds your `Node` hierarchy. It can be created by adding the `[CreateAssetMenu]` 
    attribute to your class that derives from `NodeAsset<T>`. 
- ## Node
    - The hierarchy is built up of nodes that are linked together through their relationship with their parent `Node`. 
    The `Node` class itself is abstract, but you can derive from it to create custom nodes for your tool. Nodes have their own hierarchies 
    that you can lock to prevent people from changing them. <br><br>
    
    ### This is possible through the following methods:
    - `Node.LockChildren()` Lock the children directly below this `Node`. These children can be moved around, but not reparented to other nodes, and they cannot be deleted.
    - `Node.LockHierarchy()` Lock the entire hierarchy below this `Node`. No nodes with this `Node` as an ancestor can be reparented. You can also not add any new nodes to its 
    hierarchy nor delete any nodes in it.
    - `Node.LockParent()` Lock the parent of a `Node`. The rest of the hierarchy is free to do what it wants, but you will be unable to change the parent of this `Node`. 
    You will also be unable to delete it.
- ## Component Node
    - A `ComponentNode` is a `Node` that can have node components.
- ## Node Component
    - A `NodeComponent<T>` can be added to component nodes that either are of type `T` or derive from `T`. By themselves, node components don't do much, 
    but here are some examples of how they can be used:
    ### Input
    ```csharp
    using Nexerate.Nodes;
    using UnityEngine;
    
    [Serializable]
    [RequireNodeComponents(typeof(ExampleComponent))]
    public class ExampleNode : ComponentNode
    {
        [SerializeReference] ExampleComponent input;
        
        //On the NodeAsset, iterate over all nodes and call Debug
        public void Debug()
        {
            Debug.Log(input.a * input.b);
        }
        
        [Serializable, DisallowMultiple]
        public class ExampleComponent : NodeComponent<ExampleNode>
        {
            public float a = 2;
            public float b = 5;
            
            public ExampleComponent(ExampleNode target) : base(target)
            {
                target.input = this;
            }
        }
    }
    ```
    ### Iterate over
    ```csharp
    using Nexerate.Nodes;
    using UnityEngine;
    
    [Serializable]
    [RequireNodeComponents(typeof(ExampleComponent))]
    public class ExampleNode : ComponentNode
    {
        [SerializeReference] ExampleComponent input;
        
        //On the NodeAsset, iterate over all nodes and call Debug
        public void Debug()
        {
            for (int i = 0; i < components.Count; i++)
            {
                var component = (ExampleComponent)components[i];
                component.Debug();
            }
        }
    }
    
    [Serializable]
    [AddNodeComponentMenu("Example/Component")]
    public class ExampleComponent : NodeComponent<ExampleNode>
    {
        public float a = 2;
        public float b = 5;

        public void Debug()
        {
            Debug.Log(a * b);
        }
    }
    ```
    
- ## Attributes:
    - `[AddNodeMenu]` Decide where in the "Add Node" menu your `Node` should show up.
    - `[RequireNodeComponents]` Decide what components are required by this `ComponentNode`. Required components are added automatically, and cannot be removed. 
        Attribute is inherited.
    - `[DisallowMultiple]` Add this attribute to a `NodeComponent` to disallow multiple components of this type on the same `Node`.
