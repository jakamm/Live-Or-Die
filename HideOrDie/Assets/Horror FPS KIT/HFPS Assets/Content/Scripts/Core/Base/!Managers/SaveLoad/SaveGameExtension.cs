/*
 * SaveGameExtension.cs - by ThunderWire Studio
 * Version 1.0
*/

using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ThunderWire.Json;

/// <summary>
/// Provides additional SaveGame Handler System functions.
/// </summary>
public class SaveGameExtension : MonoBehaviour
{
    private JsonManager jsonManager;
    public SerializationSettings SaveLoadSettings;

    /// <summary>
    /// Get Saved Games Asynchronously.
    /// </summary>
    public async Task<List<SavedData>> RetrieveSavedGames()
    {
        List<SavedData> result = new List<SavedData>();
        string filepath = SaveLoadSettings.GetSerializationPath();

        if (jsonManager == null)
        {
            jsonManager = new JsonManager(SaveLoadSettings);
        }

        if (Directory.Exists(filepath))
        {
            DirectoryInfo dinfo = new DirectoryInfo(filepath);
            FileInfo[] finfo = dinfo.GetFiles("*.sav");

            if (finfo.Length > 0)
            {
                foreach (var file in finfo)
                {
                    await Task.Run(() => jsonManager.DeserializeDataAsync(file.Name));
                    result.Add(new SavedData()
                    {
                        SaveName = file.Name,
                        Scene = (string)jsonManager.Json()["scene"],
                        SaveTime = (string)jsonManager.Json()["dateTime"]
                    });
                }

                return result.OrderBy(x => x.SaveTime).ToList();
            }
        }

        return default;
    }
}
