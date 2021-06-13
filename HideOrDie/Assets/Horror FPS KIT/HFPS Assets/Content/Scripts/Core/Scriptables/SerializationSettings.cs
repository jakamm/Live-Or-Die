using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ThunderWire.Helpers;

[Serializable]
public class SerializationSettings : ScriptableObject
{
    [Serializable]
    public class RuntimeSaveablePath
    {
        public string PrefabPath;
        public GameObject Prefab;

        public RuntimeSaveablePath(string path, GameObject prefab)
        {
            PrefabPath = path;
            Prefab = prefab;
        }
    }

    public bool EncryptData;
    public SerializationPath SerializePath = SerializationPath.GameDataPath;
    public string EncryptionKey;

    public List<RuntimeSaveablePath> runtimeSaveablePaths = new List<RuntimeSaveablePath>();

    /// <summary>
    /// Get Serialization Settings File Path
    /// </summary>
    public string GetSerializationPath()
    {
        return SerializationTool.GetSerializationPath(SerializePath);
    }

    /// <summary>
    /// Get Resources Prefab Path according to the Prefab.
    /// </summary>
    public string GetRuntimeSaveablePath(GameObject prefab)
    {
        if(runtimeSaveablePaths.Any(x => x.Prefab == prefab))
        {
            return runtimeSaveablePaths.FirstOrDefault(x => x.Prefab == prefab).PrefabPath;
        }

        return default;
    }

    /// <summary>
    /// Get Resources Prefab Name according to the Prefab.
    /// </summary>
    public string GetRuntimeSaveableName(GameObject prefab)
    {
        if (runtimeSaveablePaths.Any(x => x.Prefab == prefab))
        {
            return runtimeSaveablePaths.FirstOrDefault(x => x.Prefab == prefab).Prefab.name;
        }

        return default;
    }

    /// <summary>
    /// Get Resources Prefab according to the Prefab Path.
    /// </summary>
    public GameObject GetRuntimeSaveablePrefab(string prefab_path)
    {
        if (runtimeSaveablePaths.Any(x => x.PrefabPath.Equals(prefab_path)))
        {
            return runtimeSaveablePaths.FirstOrDefault(x => x.PrefabPath.Equals(prefab_path)).Prefab;
        }

        return default;
    }

    /// <summary>
    /// Get Resources Prefab according to the Prefab Path.
    /// </summary>
    public GameObject GetRuntimeSaveablePrefabByName(string prefab_name)
    {
        if (runtimeSaveablePaths.Any(x => x.Prefab.name.Equals(prefab_name)))
        {
            return runtimeSaveablePaths.FirstOrDefault(x => x.Prefab.name.Equals(prefab_name)).Prefab;
        }

        return default;
    }
}

namespace ThunderWire.Helpers
{
    public class SerializationTool
    {
        /// <summary>
        /// Get Serialization File Path
        /// </summary>
        public static string GetSerializationPath(SerializationPath path)
        {
            switch (path)
            {
                case SerializationPath.GamePath:
                    return Application.dataPath + "/";
                case SerializationPath.GameDataPath:
                    return Application.dataPath + "/Data/";
                case SerializationPath.GameSavesPath:
                    return Application.dataPath + "/Data/SavedGame/";
                case SerializationPath.DocumentsGamePath:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/" + Application.productName + "/";
                case SerializationPath.DocumentsDataPath:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/" + Application.productName + "/Data/";
                case SerializationPath.DocumentsSavesPath:
                    return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "/" + Application.productName + "/SavedGame/";
                default:
                    return default;
            }
        }
    }
}

[Serializable]
public enum SerializationPath
{
    GamePath,
    GameDataPath,
    GameSavesPath,
    DocumentsGamePath,
    DocumentsDataPath,
    DocumentsSavesPath
}