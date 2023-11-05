using System.Collections;
using NeoModLoader.api;
using NeoModLoader.services;

namespace NeoModLoader.utils;

public class ModDependencyNode
{
    public HashSet<ModDependencyNode> necessary_depend_on;
    public HashSet<ModDependencyNode> depend_on;
    public HashSet<ModDependencyNode> depend_by;
    public api.ModDeclare mod_decl { get; }
    public ModDependencyNode(api.ModDeclare pModDecl)
    {
        mod_decl = pModDecl;
        necessary_depend_on = new HashSet<ModDependencyNode>();
        depend_on = new HashSet<ModDependencyNode>();
        depend_by = new HashSet<ModDependencyNode>();
    }
}

public class ModDependencyGraph
{
    public HashSet<ModDependencyNode> nodes;
    public ModDependencyGraph(ICollection<api.ModDeclare> mods)
    {
        Dictionary<string, ModDependencyNode> node_map = new Dictionary<string, ModDependencyNode>();
        
        foreach (api.ModDeclare mod in mods)
        {
            node_map.Add(mod.UUID, new ModDependencyNode(mod));
        }
        
        foreach(api.ModDeclare mod in mods)
        {
            ModDependencyNode node = node_map[mod.UUID];
            
            foreach (string dependency in mod.Dependencies)
            {
                if (node_map.TryGetValue(dependency, out var dependency_node))
                {
                    dependency_node.depend_by.Add(node);
                    node.necessary_depend_on.Add(dependency_node);
                }
            }
            
            
            node.depend_on.UnionWith(node.necessary_depend_on);

            foreach (string optional_dependency in mod.OptionalDependencies)
            {
                if (node_map.TryGetValue(optional_dependency, out var dependency_node))
                {
                    dependency_node.depend_by.Add(node);
                    node.depend_on.Add(dependency_node);
                }
            }
        }

        nodes = new();
        nodes.UnionWith(node_map.Values);
        ModDependencyUtils.RemoveModsWithoutRequiredDependencies(this);
    }
}
internal static class ModDependencyUtils
{
    public static ModDependencyNode TryToAppendMod(ModDependencyGraph pGraph, ModDeclare pModAppend)
    {
        bool success = true;
        if (pModAppend.IncompatibleWith != null && pModAppend.IncompatibleWith.Length > 0)
        {
            bool incom_headLog = false;
            foreach (var gnode in pGraph.nodes)
            {
                if (pModAppend.IncompatibleWith.Contains(gnode.mod_decl.UUID))
                {
                    LogService.LogError($"Mod {pModAppend.UUID} is incompatible with mod {gnode.mod_decl.UUID}");
                    success = false;
                }
            }
        }
        ModDependencyNode node = new(pModAppend);
        bool mis_depen_headLog = false;
        foreach(string dependency in pModAppend.Dependencies)
        {
            try
            {
                ModDependencyNode depen_node = pGraph.nodes.First(n => n.mod_decl.UUID == dependency);
                if(mis_depen_headLog || !success) continue;
                node.necessary_depend_on.Add(depen_node);
                depen_node.depend_by.Add(node);
            }
            catch (InvalidOperationException)
            {
                if (!mis_depen_headLog)
                {
                    LogService.LogError($"Mod {pModAppend.UUID} has missing dependencies:");
                    mis_depen_headLog = true;
                    success = false;
                    continue;
                }
                LogService.LogError($"    {dependency}");
            }
        }

        if (!success) return null;

        foreach (string option_depen in pModAppend.OptionalDependencies)
        {
            foreach (var gnode in pGraph.nodes)
            {
                if(gnode.mod_decl.UUID == option_depen)
                {
                    node.depend_on.Add(gnode);
                    gnode.depend_by.Add(node);
                }
            }
        }

        pGraph.nodes.Add(node);
        return node;
    }
    public static void RemoveCircleDependencies(ModDependencyGraph pGraph)
    {
        // Remove circle dependencies and make sure more mods load.
        // and log error/pop up warning if there is any. 
        
        // First, try to remove optional depend edges for circle.
        
        // If there is still circle, try to find the solution with minimum node count to remove to make circles disappear.
    }
        
    public static void RemoveModsWithoutRequiredDependencies(ModDependencyGraph pGraph)
    {
        // Remove mods without required dependencies. 
        // and log error/pop up warning if there is any.
        Queue<ModDependencyNode> check_nodes = new Queue<ModDependencyNode>();
        foreach (ModDependencyNode node in pGraph.nodes)
        {
            check_nodes.Enqueue(node);
        }

        while (check_nodes.Count > 0)
        {
            var curr_node = check_nodes.Dequeue();
            if(!pGraph.nodes.Contains(curr_node))
            {
                continue;
            }

            if (curr_node.necessary_depend_on.Count < curr_node.mod_decl.Dependencies.Length)
            {
                // This mod has missing dependencies.
                // Remove this mod and log error/pop up warning.
                // Then add all mods that depend on this mod to check_nodes.
                
                foreach (var depend_by_node in curr_node.depend_by)
                {
                    check_nodes.Enqueue(depend_by_node);
                }
                pGraph.nodes.Remove(curr_node);
                LogService.LogError($"Mod {curr_node.mod_decl.UUID} has missing dependencies:");
                foreach (var dependency in curr_node.mod_decl.Dependencies)
                {
                    try
                    {
                        var depen_node = pGraph.nodes.First(node => node.mod_decl.UUID == dependency);
                        if (!curr_node.necessary_depend_on.Contains(depen_node))
                        {
                            LogService.LogError($"    {dependency}");
                        }
                    }
                    catch (InvalidOperationException)
                    {
                        LogService.LogError($"    {dependency}");
                    }
                }
                
            }
            else
            {
                // This mod has all required dependencies.
                // Check this mod's optional dependencies.
                // If any optional dependency is missing, just cancel dependency.
                foreach(var optional_dependency in curr_node.mod_decl.OptionalDependencies)
                {
                    if (pGraph.nodes.All(node => node.mod_decl.UUID != optional_dependency))
                    {
                        curr_node.depend_on.Remove(pGraph.nodes.First(node => node.mod_decl.UUID == optional_dependency));
                    }
                }
            }
        }
    }
        
    public static List<ModDependencyNode> SortModsCompileOrderFromDependencyTopology(ModDependencyGraph pGraph)
    {
        // Sort mods compile order from dependency topology.
        Dictionary<ModDependencyNode, int> node_in_degree = new Dictionary<ModDependencyNode, int>();
        Queue<ModDependencyNode> queue = new Queue<ModDependencyNode>();
        foreach (var node in pGraph.nodes)
        {
            node_in_degree.Add(node, node.depend_by.Count);
            if (node.depend_by.Count == 0)
            {
                queue.Enqueue(node);
            }
        }

        List<ModDependencyNode> mods = new List<ModDependencyNode>();
        while (queue.Count > 0)
        {
            ModDependencyNode curr_node = queue.Dequeue();
            mods.Add(curr_node);
            
            foreach (var depend_on_node in curr_node.depend_on)
            {
                node_in_degree[depend_on_node]--;
                if (node_in_degree[depend_on_node] == 0)
                {
                    queue.Enqueue(depend_on_node);
                }
            }
        }

        return mods;
    }
}