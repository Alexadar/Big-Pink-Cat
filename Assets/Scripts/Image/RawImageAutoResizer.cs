using UnityEngine;
using UnityEngine.UI;

public class ResizeRawImage : MonoBehaviour
{
    private RawImage rawImage;
    private float baseWidth;
    private float baseHeight;

    private ScreenOrientation lastOrientation;
    private int lastScreenWidth;
    private int lastScreenHeight;

    private void Start()
    {
        rawImage = GetComponent<RawImage>();
        baseWidth = rawImage.texture.width;
        baseHeight = rawImage.texture.height;

        lastOrientation = Screen.orientation;
        lastScreenWidth = Screen.width;
        lastScreenHeight = Screen.height;
        ResizeImage();
    }

    private void Update()
    {
        if (Screen.orientation != lastOrientation || Screen.width != lastScreenWidth || Screen.height != lastScreenHeight)
        {
            lastOrientation = Screen.orientation;
            lastScreenWidth = Screen.width;
            lastScreenHeight = Screen.height;
            ResizeImage();
        }
    }

    private void ResizeImage()
    {
        // this resize method will fill the screen with the image fully
        // without stretching the image
        // it will keep the aspect ratio of the image
        // and align the image to the top of the screen, bottom might b cut off

        float screenWidth = Screen.width;
        float screenHeight = Screen.height;

        float targetHeight = screenHeight;
        float targetWidth = screenWidth;

        float ratio = baseWidth / baseHeight;

        if (screenWidth / screenHeight > ratio)
        {
            // If the screen's aspect ratio is greater than the image's, adjust the height
            targetHeight = screenWidth / ratio;
        }
        else
        {
            // If the screen's aspect ratio is less than or equal to the image's, adjust the width
            targetWidth = screenHeight * ratio;
        }

        rawImage.rectTransform.sizeDelta = new Vector2(targetWidth, targetHeight);
        rawImage.rectTransform.anchoredPosition = new Vector2(0, 0);
    }
}