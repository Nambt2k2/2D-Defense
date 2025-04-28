#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public class Screenshot {
    [MenuItem("Tools/Image/Screenshot")]
    public static void TakeScreenshotAsync() {
        string savePath = GetSavePath();
        if (string.IsNullOrEmpty(savePath)) {
            Debug.Log("Screenshot cancelled");
            return;
        }

        // Ensure directory exists
        string directory = Path.GetDirectoryName(savePath);
        if (!Directory.Exists(directory)) {
            Directory.CreateDirectory(directory);
        }

        // Capture and save screenshot
        ScreenCapture.CaptureScreenshot(savePath);
        Debug.Log("Screenshot saved at: " + savePath);
    }

    static string GetSavePath() {
        string defaultFolder = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Desktop);
        string defaultName = "Screenshot_" + System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string savePath = "";
        savePath = EditorUtility.SaveFilePanel("Save Screenshot", defaultFolder, defaultName, "png");
        return savePath;
    }
}
#endif