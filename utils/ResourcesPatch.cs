using System.Globalization;
using HarmonyLib;
using NeoModLoader.api.exceptions;
using NeoModLoader.constants;
using NeoModLoader.General;
using NeoModLoader.services;
using UnityEngine;
using UnityEngine.U2D;
using Object = UnityEngine.Object;

namespace NeoModLoader.utils;

internal static class ResourcesPatch
{
    class ResourceTree
    {
        private ResourceTreeNode root = new();
        internal Dictionary<string, UnityEngine.Object> direct_objects = new();
        public ResourceTreeNode Find(string path)
        {
            path = path.ToLower();

            string[] parts;
            if (path.EndsWith("/"))
            {
                parts = path.Substring(0, path.Length - 1).Split('/');
            }
            else
            {
                parts = path.Split('/');
            }

            var node = root;
            foreach (var part in parts)
            {
                if (!node.children.ContainsKey(part))
                {
                    return null;
                }

                node = node.children[part];
            }

            return node;
        }

        public UnityEngine.Object Get(string path)
        {
            return direct_objects.TryGetValue(path.ToLower(), out Object o) ? o : null;
        }

        public void Add(string path, Object obj)
        {
            string lower_path = path.ToLower();
            direct_objects[lower_path] = obj;
            string[] parts = lower_path.Split('/');
            var node = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!node.children.ContainsKey(parts[i]))
                {
                    node.children[parts[i]] = new ResourceTreeNode();
                }

                node = node.children[parts[i]];
            }
            node.objects[parts[parts.Length - 1]] = obj;

        }
        public void AddFromFile(string path, string absPath)
        {
            string lower_path = path.ToLower();
            if (lower_path.EndsWith(".meta")) return;
            if (lower_path.EndsWith("sprites.json"))
            {
                return;
            }
            
            string parent_path = Path.GetDirectoryName(lower_path);
            UnityEngine.Object[] objs;
            try
            {
                string abs_lower_path = absPath.ToLower();
                objs = LoadResourceFile(ref absPath, ref abs_lower_path);

                foreach (var obj in objs)
                {
                    if (parent_path == null)
                    {
                        direct_objects[obj.name] = obj;
                    }
                    else
                    {
                        direct_objects[Path.Combine(parent_path, obj.name).Replace('\\','/').ToLower()] = obj;
                    }
                }
            }
            catch (UnrecognizableResourceFileException)
            {
                LogService.LogWarning($"Cannot recognize resource file {path}");
                return;
            }
            if(objs.Length == 0) return;

            string[] parts = lower_path.Split('/');
            var node = root;
            for (int i = 0; i < parts.Length - 1; i++)
            {
                if (!node.children.ContainsKey(parts[i]))
                {
                    node.children[parts[i]] = new ResourceTreeNode();
                }

                node = node.children[parts[i]];
            }

            foreach (var obj in objs)
            {
                node.objects[obj.name.ToLower()] = obj;
            }
        }
    }

    class ResourceTreeNode
    {
        public readonly Dictionary<string, ResourceTreeNode> children = new();
        public readonly Dictionary<string, UnityEngine.Object> objects = new();
    }

    private static ResourceTree tree;

    public static Dictionary<string, UnityEngine.Object> GetAllPatchedResources()
    {
        return tree.direct_objects;
    }
    internal static void Initialize()
    {
        tree = new ResourceTree();
        SpriteAtlas atlas = Resources.FindObjectsOfTypeAll<SpriteAtlas>().FirstOrDefault(x => x.name == "SpriteAtlasUI");

        Sprite[] sprites = new Sprite[atlas.spriteCount];
        atlas.GetSprites(sprites);
        foreach (var sprite in sprites)
        {
            tree.Add($"ui/special/{sprite.name.Replace("(Clone)", "")}", sprite);
        }
    }
    /// <summary>
    /// Load a resource file from path, and named by pLowerPath.
    /// </summary>
    /// <param name="path">the path to the resource file to load</param>
    /// <param name="pLowerPath">the lower of path with <see cref="CultureInfo.CurrentCulture"/></param>
    /// <returns>The Objects loaded, if single Object, an array with single one; if no Objects, an empty array</returns>
    /// <exception cref="UnrecognizableResourceFileException">It can recognize jpg, png, jpeg, txt, json, yml, ab by postfix now</exception>
    public static UnityEngine.Object[] LoadResourceFile(ref string path, ref string pLowerPath)
    {
        if (pLowerPath.EndsWith(".png") || pLowerPath.EndsWith(".jpg") || pLowerPath.EndsWith(".jpeg"))
            return SpriteLoadUtils.LoadSprites(path);
        if (pLowerPath.EndsWith(".txt") || pLowerPath.EndsWith(".json") || pLowerPath.EndsWith(".yml"))
            return new Object[]{LoadTextAsset(path)};
        if (pLowerPath.EndsWith(".ab"))
        {
            return Array.Empty<Object>();
        }

        return new Object[]{LoadTextAsset(path)};
    }

    private static TextAsset LoadTextAsset(string path)
    {
        TextAsset textAsset = new TextAsset(File.ReadAllText(path));
        textAsset.name = Path.GetFileNameWithoutExtension(path);
        return textAsset;
    }

    internal static void LoadResourceFromMod(string pModFolder)
    {
        string path = Path.Combine(pModFolder, Paths.ModResourceFolderName);
        if (!Directory.Exists(path)) return;

        var files = SystemUtils.SearchFileRecursive(pModFolder, filename => !filename.StartsWith("."),
            dirname => !dirname.StartsWith("."));
        foreach (var file in files)
        {
            tree.AddFromFile(file.Replace(path, "").Replace('\\', '/').Substring(1), file);
        }
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Resources), nameof(Resources.LoadAll), new Type[]
    {
        typeof(string),
        typeof(Type)
    })]
    private static UnityEngine.Object[] LoadAll_Postfix(UnityEngine.Object[] __result, string path,
        Type systemTypeInstance)
    {
        ResourceTreeNode node = tree.Find(path);
        if (node == null || node.objects.Count == 0) return __result;
        
        var list = new List<UnityEngine.Object>(__result);
        // Use a list to store names, because it is faster to get name of an GameObject repeatedly.
        var names = new List<string>(__result.Length);
        foreach (var obj in list)
        {
            names.Add(obj.name);
        }
        
        foreach (var (key, value) in node.objects)
        {
            int idx = names.IndexOf(key);
            if (idx < 0)
            {
                list.Add(value);
            }
            else
            {
                list[idx] = value;
            }
        }

        return list.ToArray();
    }
    [HarmonyPostfix]
    [HarmonyPatch(typeof(Resources), nameof(Resources.Load), new Type[]
    {
        typeof(string),
        typeof(Type)
    })]
    private static UnityEngine.Object Load_Postfix(UnityEngine.Object __result, string path,
        Type systemTypeInstance)
    {
        var new_result = tree.Get(path);
        if(new_result == null) return __result;
        return new_result;
    }
}