using UnityEngine;
using UnityEngine.InputSystem;

public class ScreenshotCapture : MonoBehaviour
{
    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.f12Key.wasPressedThisFrame)
        {
            string fileName = "screenshot_" + System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss") + ".png";

            ScreenCapture.CaptureScreenshot(fileName, 2);

            Debug.Log("Screenshot saved: " + fileName);
        }
    }
}