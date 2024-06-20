using UnityEngine;
using UnityEditor;
using System.IO;

public class AssetGeneratorUIWindow : EditorWindow
{
    Texture2D panelUIBGTexture = null;
    string panelUIBGTexturePath = null;
    Color bgColor = Color.white;
    Color borderColor = Color.black;
    int borderWidth = 1;
    int borderRadius = 0;

    int alpha = 255;

    [MenuItem("Window/Asset Generator")]
    public static void ShowWindow()
    {
        GetWindow<AssetGeneratorUIWindow>("Asset Generator");
    }

    private void OnGUI()
    {
        
        GUILayout.Label("Background Color:");
        bgColor = EditorGUILayout.ColorField(bgColor);

        GUILayout.Label("Border Color:");
        borderColor = EditorGUILayout.ColorField(borderColor);

        GUILayout.Label("Alpha:");
        alpha = EditorGUILayout.IntSlider(alpha, 0, 255);

        GUILayout.Label("Border Width:");
        borderWidth = EditorGUILayout.IntField(borderWidth);

        GUILayout.Label("Border Radius:");
        borderRadius = EditorGUILayout.IntField(borderRadius);

        GUILayout.Label("Drag your texture here:");
        Rect drop_area = GUILayoutUtility.GetRect(0.0f, 50.0f, GUILayout.ExpandWidth(true));
        GUI.Box(drop_area, panelUIBGTexture);

        Event evt = Event.current;
        switch (evt.type) {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!drop_area.Contains(evt.mousePosition))
                    break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform) {
                    DragAndDrop.AcceptDrag();

                    foreach (UnityEngine.Object dragged_object in DragAndDrop.objectReferences) {
                        if (! (dragged_object is Texture2D)) continue;
                        panelUIBGTexture = dragged_object as Texture2D;
                        panelUIBGTexturePath = AssetDatabase.GetAssetPath(panelUIBGTexture);
                        Debug.Log("Texture path: " + panelUIBGTexturePath);
                        break;
                    }
                }
                break;
        }

        if (GUILayout.Button("Generate Texture"))
        {
            GenerateTexture();
        }
    }

    private void GenerateTexture()
    {
        // calculate alpha as 0...1 float from alpha 0...255 int
        var realAlpha = alpha / 255.0f;

        // Create a new 2D texture with a size of 512x512
        Texture2D texture = new Texture2D(512, 512, TextureFormat.ARGB32, false);

        // Fill the texture with the background color
        Color[] fillPixels = new Color[texture.width * texture.height];
        for (int i = 0; i < fillPixels.Length; i++)
        {
            fillPixels[i] = new Color(bgColor.r, bgColor.g, bgColor.b, realAlpha);
        }
        texture.SetPixels(fillPixels);

        // Create a border with the specified color, width, and radius
        for (int y = 0; y < texture.height; ++y)
        {
            for (int x = 0; x < texture.width; ++x)
            {
                if ((x < borderWidth || x > texture.width - borderWidth || y < borderWidth || y > texture.height - borderWidth) && 
                    (x >= borderRadius && x < texture.width - borderRadius && y >= borderRadius && y < texture.height - borderRadius))
                {
                    texture.SetPixel(x, y, new Color(borderColor.r, borderColor.g, borderColor.b, realAlpha));
                }
            }
        }

        texture.Apply();

        // Save the texture to panelUIBGTexture
        panelUIBGTexture = texture;

        // Ensure the preview is updated
        Repaint();

        // Save the texture to the project
        byte[] bytes = texture.EncodeToPNG();
        File.WriteAllBytes(panelUIBGTexturePath, bytes);

        Debug.Log("Texture saved to: " + panelUIBGTexturePath);

        // refresh Unity's asset database preview
        AssetDatabase.Refresh();
    }
}