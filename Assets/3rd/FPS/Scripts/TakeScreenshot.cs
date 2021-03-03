using System.IO;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;
using UnityEngine.UI;

public class TakeScreenshot : MonoBehaviour
{
    [Tooltip("Root of the screenshot panel in the menu")]
    public GameObject screenshotPanel;
    [Tooltip("Name for the screenshot file")]
    public string fileName = "Screenshot";
    [Tooltip("Image to display the screenshot in")]
    public RawImage previewImage;

    CanvasGroup m_MenuCanvas = null;
    Texture2D m_Texture;

    bool m_TakeScreenshot;
    bool m_ScreenshotTaken;
    bool m_IsFeatureDisable;

    string getPath() => k_ScreenshotPath + fileName + ".png";

    const string k_ScreenshotPath = "Assets/";

    void Awake()
    {
#if !UNITY_EDITOR
        // this feature is available only in the editor
        screenshotPanel.SetActive(false);
        m_IsFeatureDisable = true;
#else
        m_IsFeatureDisable = false;

        var gameMenuManager = GetComponent<InGameMenuManager>();
        DebugUtility.HandleErrorIfNullGetComponent<InGameMenuManager, TakeScreenshot>(gameMenuManager, this, gameObject);

        m_MenuCanvas = gameMenuManager.menuRoot.GetComponent<CanvasGroup>();
        DebugUtility.HandleErrorIfNullGetComponent<CanvasGroup, TakeScreenshot>(m_MenuCanvas, this, gameMenuManager.menuRoot.gameObject);

        LoadScreenshot();
#endif
    }

    void Update()
    {
        previewImage.enabled = previewImage.texture != null;

        if (m_IsFeatureDisable)
            return;

        if (m_TakeScreenshot)
        {
            m_MenuCanvas.alpha = 0;
            ScreenCapture.CaptureScreenshot(getPath());
            m_TakeScreenshot = false;
            m_ScreenshotTaken = true;
            return;
        }

        if (m_ScreenshotTaken)
        { 
            LoadScreenshot();
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif

            m_MenuCanvas.alpha = 1;
            m_ScreenshotTaken = false;
        }
    }

    public void OnTakeScreenshotButtonPressed()
    {
        m_TakeScreenshot = true;
    }

    void LoadScreenshot()
    {
        if (File.Exists(getPath()))
        {
            var bytes = File.ReadAllBytes(getPath());

            m_Texture = new Texture2D(2, 2);
            m_Texture.LoadImage(bytes);
            m_Texture.Apply();
            previewImage.texture = m_Texture;
        }
    }
}
