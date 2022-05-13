#region Using
using UnityEditor.IMGUI.Controls;
using System.Collections.Generic;
using System.Collections;
using System.Reflection;
using UnityEngine;
using System.Linq;
using System; 
#endregion

namespace Nexerate.Nodes.Editor
{
    class GenericDropdown : AdvancedDropdown
    {
        public Action<Type> clickedType;
        public Action<string> clickedName;

        readonly List<Type> types;
        readonly List<string> names;
        readonly string header;
        readonly string formatOut;
        public GenericDropdown(string header, string formatOut, List<Type> types) : base(new())
        {
            this.header = header;
            this.formatOut = formatOut;
            this.types = types;
        }

        public GenericDropdown(string header, List<string> names) : base(new())
        {
            this.header = header;
            this.names = names;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem(header);
            List<List<string>> paths = new();

            if (types != null)
            {
                for (int i = 0; i < types.Count; i++)
                {
                    var attribute = types[i].GetCustomAttributes<AddNodeComponentMenuAttribute>().FirstOrDefault();

                    List<string> path = attribute == null ? new() : attribute.menuName.Split("/").ToList();
                    if (attribute == null) path.Add(types[i].Name.Remove(formatOut).SpaceBeforeUppercase());
                    paths.Add(path);
                }
            }
            else if (names != null)
            {
                for (int i = 0; i < names.Count; i++)
                    paths.Add(names[i].Split("/").ToList());
            }
            BuildRecursive(root, paths, new(), 0);

            void BuildRecursive(AdvancedDropdownItem parent, List<List<string>> paths, List<string> path, int depth)
            {
                List<List<string>> categories = new();
                for (int i = 0; i < paths.Count; i++)
                {
                    //To see if we already have the category, compare path[depth] to category[depth]
                    if (Path1ContainsPath2(paths[i], path) && !ContainsCategory(paths[i]))
                        categories.Add(paths[i]);
                }

                bool Path1ContainsPath2(List<string> path1, List<string> path2)
                {
                    //Iterate over all elements in path to look for inconsistencies
                    for (int i = 0; i < depth; i++)
                        if (path1[i] != path2[i]) return false;
                    return true;
                }

                bool ContainsCategory(List<string> category) => categories.Where(c => c[depth] == category[depth]).Any();

                //On Depth = 0, this will simply be all top level categories/items
                for (int i = 0; i < categories.Count; i++)
                {
                    AdvancedDropdownItem item = new(categories[i][depth].SpaceBeforeUppercase());

                    //If category has any children, then continue with the recursion
                    if (categories[i].Count > depth + 1)
                        BuildRecursive(item, paths, categories[i], depth + 1);

                    parent.AddChild(item);
                }
            }

            return root;
        }
        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            if (names != null)
            {
                clickedName?.Invoke(item.name);
                return;
            }

            var output = types.Where(type => type.Name == item.name.RemoveSpaces()).FirstOrDefault();

            //First we try and find a type where "format out" was formatted out
            if (output == null) output = types.Where(type => type.Name == item.name.RemoveSpaces() + formatOut).FirstOrDefault();

            //Try again, but without adding "format out" back in
            if (output == null) output = types.Where(type => type.Name == item.name.RemoveSpaces()).FirstOrDefault();

            clickedType?.Invoke(output);
        }
    }
}
