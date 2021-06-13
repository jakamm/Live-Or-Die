/*
 * SurfaceDetailsScriptable.cs - by ThunderWire Studio
 * ver. 1.0
*/

using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using ThunderWire.Utility;

/// <summary>
/// Contains all Required Surface Details.
/// </summary>
public class SurfaceDetailsScriptable : ScriptableObject
{
    public List<SurfaceDetails> surfaceDetails = new List<SurfaceDetails>();

    /// <summary>
    /// Get Surface Details which contains a specified Tag.
    /// </summary>
    public SurfaceDetails GetSurfaceDetails(string Tag)
    {
        if(surfaceDetails.Any(x => !string.IsNullOrEmpty(x.SurfaceTag) && x.SurfaceTag.Equals(Tag)))
        {
            return surfaceDetails.FirstOrDefault(x => x.SurfaceTag.Equals(Tag));
        }

        return default;
    }

    /// <summary>
    /// Get Surface Details which contains a specified Texture.
    /// </summary>
    public SurfaceDetails GetSurfaceDetails(Texture2D texture)
    {
        if (surfaceDetails.Any(x => x.SurfaceTextures.Length > 0 && x.SurfaceTextures.Any(y => y == texture)))
        {
            return surfaceDetails.FirstOrDefault(x => x.SurfaceTextures.Any(y => y == texture));
        }

        return default;
    }

    /// <summary>
    /// Get Surface Details which contains any of a specified Textures.
    /// </summary>
    public SurfaceDetails GetSurfaceDetails(Texture2D[] textures)
    {
        if (surfaceDetails.Any(x => x.SurfaceTextures.Length > 0 && x.SurfaceTextures.Any(y => textures.Any(z => y == z))))
        {
            return surfaceDetails.FirstOrDefault(x => x.SurfaceTextures.Any(y => textures.Any(z => y == z)));
        }

        return default;
    }

    /// <summary>
    /// Get Surface Details which contains Texture or Tag in specified GameObject.
    /// </summary>
    public SurfaceDetails GetSurfaceDetails(GameObject gameObject, SurfaceID surface)
    {
        MeshRenderer meshRenderer;
        SurfaceDetails details;

        if (surface == SurfaceID.Texture && (meshRenderer = gameObject.GetComponent<MeshRenderer>()) != null)
        {
            Texture2D[] textures = meshRenderer.materials.Select(x => x.mainTexture).Cast<Texture2D>().ToArray();

            if ((details = GetSurfaceDetails(textures)) != null)
            {
                return details;
            }
        }
        else if ((details = GetSurfaceDetails(gameObject.tag)) != null)
        {
            return details;
        }

        return default;
    }

    /// <summary>
    /// Get Surface Details at Terrain Position
    /// </summary>
    public SurfaceDetails GetTerrainSurfaceDetails(Terrain terrain, Vector3 worldPos)
    {
        SurfaceDetails details;
        if ((details = GetSurfaceDetails(Tools.TerrainPosToTex(terrain, worldPos))) != null)
        {
            return details;
        }

        return default;
    }

    public void Reseed()
    {
        foreach (SurfaceDetails details in surfaceDetails)
        {
            details.ElementID = surfaceDetails.ToList().IndexOf(details);
        }
    }
}

public enum SurfaceID { Tag, Texture }

[Serializable]
public class SurfaceDetails
{
    public string SurfaceTag;
    [ReadOnly] public int ElementID;
    public Texture2D[] SurfaceTextures;
    public AudioClip[] SurfaceFootsteps;
    [Space(5)]
    public GameObject SurfaceBulletmark;
    public AudioClip[] BulletmarkImpact;
    [Space(5)]
    public GameObject SurfaceMeleemark;
    public AudioClip[] MeleemarkImpact;
    [Space(5)]
    [Range(0, 2)] public float ImpactVolume = 1f;
    public bool allowFootsteps = true;
    public bool allowImpactMark = true;
}
