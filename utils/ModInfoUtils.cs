using MonoMod.Utils;
using NeoModLoader.api;
using NeoModLoader.constants;
using NeoModLoader.services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using UnityEngine;

namespace NeoModLoader.utils;

internal static class ModInfoUtils
{
    public static List<api.ModDeclare> findMods()
    {
        var mods = new List<api.ModDeclare>();
        var mod_folders = Directory.GetDirectories(Paths.ModsPath);
        foreach (var mod_folder in mod_folders)
        {
            var mod_config_path = Path.Combine(mod_folder, Paths.ModConfigFileName);
            
            if(!File.Exists(mod_config_path))
            {
                LogService.LogWarning($"No mod.json file for folder {mod_folder} in Mods");
                continue;
            }
            try
            {
                var mod = new api.ModDeclare(mod_config_path);
                mods.Add(mod);
            }
            catch (Exception e)
            {
                LogService.LogError($"Error occurs when loading mod config file {mod_config_path}");
                LogService.LogError(e.Message);
                LogService.LogError(e.StackTrace);
                continue;
            }
        }
        return mods;
    }
    // ReSharper disable once InconsistentNaming
    public static bool isModNeedRecompile(string pModUUID, string pModFolderPath)
    {
        return getModLastCompileTimestamp(pModUUID) < Others.confirmed_compile_time + getModNewestUpdateTimestamp(pModFolderPath);
    }

    private static readonly Dictionary<string, long> mod_compile_timestamps = new();

    private static readonly JsonSerializerSettings mod_compile_timestamps_serializer_settings = new()
    {
        ContractResolver = new DefaultContractResolver()
    };
    public static void updateModCompileTimestamp(string pModUUID)
    {
        mod_compile_timestamps[pModUUID] = DateTime.UtcNow.Ticks;

        File.WriteAllText(Paths.ModCompileRecordPath,
            JsonConvert.SerializeObject(mod_compile_timestamps, mod_compile_timestamps_serializer_settings));
    }
    // ReSharper disable once InconsistentNaming
    private static long getModLastCompileTimestamp(string pModUUID)
    {
        if (mod_compile_timestamps.Count > 0 && !mod_compile_timestamps.ContainsKey(pModUUID))
        {
            return 0;
        }

        if (mod_compile_timestamps.Count == 0)
        {
            try
            {
                mod_compile_timestamps.AddRange(
                    JsonConvert.DeserializeObject<Dictionary<string, long>>(
                        File.ReadAllText(Paths.ModCompileRecordPath),
                        mod_compile_timestamps_serializer_settings));
            }
            catch (Exception)
            {
                mod_compile_timestamps.Add(pModUUID, 0);
            }
        }

        mod_compile_timestamps.TryAdd(pModUUID, 0);

        return mod_compile_timestamps[pModUUID];
    }

    private static long getModNewestUpdateTimestamp(string pModFolderPath)
    {
        var dir = new DirectoryInfo(pModFolderPath);
        var files = dir.GetFiles("*", SearchOption.AllDirectories);

        long newest_timestamp = 0;
        
        foreach (var file_info in files)
        {
            newest_timestamp = Math.Max(newest_timestamp, file_info.LastWriteTimeUtc.Ticks);
        }
        return newest_timestamp;
    }
}