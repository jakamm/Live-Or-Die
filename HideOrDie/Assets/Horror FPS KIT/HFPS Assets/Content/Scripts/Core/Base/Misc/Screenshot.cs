using ThunderWire.CrossPlatform.Input;
using UnityEngine;

public class Screenshot : MonoBehaviour
{
    private CrossPlatformInput input;

    const string path = "Assets/Screenshots/";
    public KeyCode screesnhotKey;
    public bool crossPlatformInput;

    private bool isTaken;
    private int count;

    private void Awake()
    {
        if (crossPlatformInput)
        {
            input = CrossPlatformInput.Instance;
        }
    }

    void Update()
    {
        if (crossPlatformInput)
        {
            if (input.inputsLoaded)
            {
                if (input.GetControlPressedOnce(this, screesnhotKey.ToString()) && !isTaken)
                {
                    TakeScreenshot();
                    isTaken = true;
                }
                else if (isTaken)
                {
                    isTaken = false;
                }
            }
        }
        else
        {
            if (Input.GetKeyDown(screesnhotKey) && !isTaken)
            {
                TakeScreenshot();
                isTaken = true;
            }
            else if (isTaken)
            {
                isTaken = false;
            }
        }
    }

    void TakeScreenshot()
    {
        if (!System.IO.Directory.Exists(path))
        {
            System.IO.Directory.CreateDirectory(path);
        }

        string name = path + "Screenshot_" + count + ".png";
        ScreenCapture.CaptureScreenshot(name);
        Debug.Log("Captured: " + name);
        count++;
    }
}
