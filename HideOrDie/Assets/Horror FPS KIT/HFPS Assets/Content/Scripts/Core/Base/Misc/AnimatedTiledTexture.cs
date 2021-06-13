using UnityEngine;
using System.Collections;
using ThunderWire.Utility;

[ExecuteInEditMode]
public class AnimatedTiledTexture : MonoBehaviour
{
    private Camera mainCamera;

    public int columns = 2;
    public int rows = 2;
    public float framesPerSecond = 10f;

    [Header("Look Rotation")]
    public bool lookAtPlayer = true;
    public bool faceRotation = false;

    //the current frame to display
    private int index = 0;

    void Awake()
    {
        mainCamera = Tools.MainCamera();
    }

    void OnEnable()
    {
        StartCoroutine(updateTiling());

        //set the tile size of the texture (in UV units), based on the rows and columns
        Vector2 size = new Vector2(1f / columns, 1f / rows);
        GetComponent<MeshRenderer>().sharedMaterial.SetTextureScale("_MainTex", size);
    }

    private IEnumerator updateTiling()
    {
        while (true)
        {
            //move to the next index
            index++;
            if (index >= rows * columns)
                index = 0;

            //split into x and y indexes
            Vector2 offset = new Vector2((float)index / columns - (index / columns), //x index
                                          (index / columns) / (float)rows);          //y index

            GetComponent<MeshRenderer>().sharedMaterial.SetTextureOffset("_MainTex", offset);

            yield return new WaitForSeconds(1f / framesPerSecond);
        }
    }

    void Update()
    {
        if (faceRotation && mainCamera)
        {
            transform.rotation = Quaternion.LookRotation(-mainCamera.transform.up, -mainCamera.transform.forward);
        }
    }
}