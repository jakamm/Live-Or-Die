using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class CutsceneTimer : MonoBehaviour
{

    [Header("Clip Length")]
    public int clipLength;


    [Header("Cutscene Timer")]
    public int cutsceneTimer = 0;


    [Header("Next Scene")]
    public string NextScene;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void FixedUpdate()
    {





        if (cutsceneTimer >= clipLength)
        {


            //Load next level
            SceneManager.LoadScene(NextScene);

            //Debug
            Debug.Log("Cutscene over.");


        }

        else
        {

            //increase timer
            cutsceneTimer += 1;

        }




    }
}
