using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System.IO;
using Unity.Mathematics;

public class Tex2DCreator : EditorWindow {

    
    private string fileName;
    private List<Texture2D> textures = new List<Texture2D>();
    private bool mipmapsEnabled = true;
    private ReorderableList reorderableList;

    private Texture2DArray loadTexture2DArray;
    private List<Texture2D> tempLoadedTextures = new List<Texture2D>();
    private string savePath = "";
    private string assetName = "cloud tex array";
    Vector2 scroll;
    private MaxRectsBinPack.FreeRectChoiceHeuristic method;
    
    [MenuItem("Window/Atmosphere/Create Texture2D Tool")]
    static void Init() {
        // Get/Create EditorWindow
        Tex2DCreator window = (Tex2DCreator) GetWindow(typeof(Tex2DCreator));
        window.Show();
    }

    void OnGUI() {
        scroll = GUILayout.BeginScrollView(scroll);
        EditorGUILayout.LabelField("Load Existing Texture2DArray Asset", EditorStyles.boldLabel);
        // Load
        loadTexture2DArray = (Texture2DArray)EditorGUILayout.ObjectField("Load Texture2DArray", loadTexture2DArray, typeof(Texture2DArray), false);
        if (GUILayout.Button("Load") && loadTexture2DArray != null) {
            assetName = loadTexture2DArray.name;
            if (textures.Count != 0) {
                if (!EditorUtility.DisplayDialog("Load Texture2DArray", 
                    "Warning : This will override textures in the list!", 
                    "Load!", "Cancel!")) {
                    return;
                }
            }
            LoadTexturesFromTex2DArray();
        }

        GUILayout.Space(5);
        EditorGUILayout.LabelField("Texture Array Slices", EditorStyles.boldLabel);
        // Texture List
        reorderableList.DoLayoutList();

        // Settings
        GUILayout.Space(5);
        GUILayout.Label("Save Texture2DArray Asset", EditorStyles.boldLabel);

        GUILayout.Space(5);
        mipmapsEnabled = GUILayout.Toggle(mipmapsEnabled, "Mip Maps Enabled?");

        GUILayout.FlexibleSpace();
        // Save to Array
        if (GUILayout.Button("Save to Array (in Assets)") && textures.Count > 0) {
            savePath = EditorUtility.SaveFilePanel("Save Texture Array", "Assets", "TextureArray", "asset");
            if (!string.IsNullOrEmpty(savePath)) 
                SaveTexture2DArray();
        }

        if (loadTexture2DArray != null) {
            if (GUILayout.Button("Override Existing Texture2DArray Asset") && textures.Count > 0) {
                if (!EditorUtility.DisplayDialog("Override Existing Texture2DArray Asset", 
                    "Warning : Are you sure you want to override the existing Texture2DArray asset?", 
                    "Override!", "Cancel!")) {
                    return;
                }
                SaveTexture2DArray(true);
            }
        }
        GUILayout.Space(5);
        // Save to Atlas
        if (GUILayout.Button("Save to Tex2D Atlas (in Assets)") && textures.Count > 0) {
            savePath = EditorUtility.SaveFilePanel("Save Texture Atlas", "Assets", "TextureAtlas", "tga");
            if (!string.IsNullOrEmpty(savePath)) 
                SaveToRect();
        }
        GUILayout.Space(5);
        method = (MaxRectsBinPack.FreeRectChoiceHeuristic)EditorGUILayout.EnumPopup("Rect method", method);
        GUILayout.EndScrollView();
        EditorGUILayout.LabelField("", GUI.skin.horizontalSlider);
        EditorStyles.label.wordWrap = true;
        EditorGUILayout.LabelField("Note : The first texture is used to determine the size and format of this list.\n " +
            "Texture 2D Array\n" +                       
            "All textures must have same width/height, same mipmap count, and use the same format " +
            "/(Crunch compression not supported)\n" +
            "Texture 2D Atlas\n" +
            "You can choose any size texture with any sort order, but the sort order will influence the result." +
            " Remember to check the rect method if you want. "
            , EditorStyles.label);
    }

    private void SaveTexture2DArray(bool overrideLoadedAsset = false) {
        Texture2D tex0 = textures[0];

        UnityEngine.Experimental.Rendering.TextureCreationFlags flags = mipmapsEnabled ?
            UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain :
            UnityEngine.Experimental.Rendering.TextureCreationFlags.None;

        int mipmapCount = mipmapsEnabled ? tex0.mipmapCount : 1;

        Texture2DArray tex2dArray = new Texture2DArray(tex0.width, tex0.height, textures.Count, tex0.graphicsFormat, flags, mipmapCount);
        tex2dArray.name = assetName;
        for (int i = 0; i < textures.Count; i++) {
            Texture2D tex = textures[i];

            if (!mipmapsEnabled) {
                // Copy only Mip0
                Graphics.CopyTexture(tex, 0, 0, tex2dArray, i, 0);
            } else {
                // Copy all Mips
                Graphics.CopyTexture(tex, 0, tex2dArray, i);
            }
        }

        // string assetPath = "Assets/" + Path.GetFileNameWithoutExtension(fileName) + ".asset";
        Object existingAsset;
        if (overrideLoadedAsset) {
            existingAsset = loadTexture2DArray;
            if (existingAsset == null) {
                Debug.LogError("Attempted to override existing Texture2DArray asset, but it is null?");
            }
        } else {
            existingAsset = AssetDatabase.LoadAssetAtPath<Object>(savePath);
            if (existingAsset != null) {
                if (!EditorUtility.DisplayDialog("Save Texture2DArray", 
                    "Warning : Asset with that name already exists, override it?", 
                    "Override!", "Cancel!")) {
                    return;
                }
            }
        }
        
        if (existingAsset == null) {
            string assetFolderPath = "Assets" + savePath.Substring(Application.dataPath.Length);
            AssetDatabase.CreateAsset(tex2dArray, assetFolderPath);

            AssetDatabase.Refresh();
        } else {
            EditorUtility.CopySerialized(tex2dArray, existingAsset);
        }
        AssetDatabase.SaveAssets();
    }
    public Texture2D DeCompress(Texture2D source)
    {
        RenderTexture renderTex = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(source, renderTex);
        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = renderTex;
        Texture2D readableText = new Texture2D(source.width, source.height);
        readableText.ReadPixels(new Rect(0, 0, renderTex.width, renderTex.height), 0, 0);
        readableText.Apply();
        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(renderTex);
        return readableText;
    }
    // if you want to Save to a quad From left -> right, from bottom -> top, you can use this function.
    // this function supports sub tex mipmaps writing.
    private void SaveTextureAtlas(bool overrideLoadedAsset = false)
    {
        Texture2D tex0 = textures[0];
        int quadTexSize = tex0.width;
        int quadBlockPower = 4;
        int AtlasTexSize = quadTexSize * quadBlockPower;
        UnityEngine.Experimental.Rendering.TextureCreationFlags flags = mipmapsEnabled ?
            UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain :
            UnityEngine.Experimental.Rendering.TextureCreationFlags.None;
        int mipmapCount = mipmapsEnabled ? tex0.mipmapCount : 1;
        Texture2D tex2DAtlas = new Texture2D( AtlasTexSize, AtlasTexSize, tex0.graphicsFormat, mipmapCount, flags);
        tex2DAtlas.name = assetName;
        
        
        for (int i = 0; i < textures.Count; i++) {
            Texture2D tex = textures[i];

            int dstCoordX;
            int dstCoordY;

            dstCoordX = i % quadBlockPower * quadTexSize;
            dstCoordY = Mathf.FloorToInt((i / quadBlockPower) * quadTexSize);
            if (!mipmapsEnabled) {
                if (dstCoordX + quadTexSize > AtlasTexSize || dstCoordY + quadTexSize > AtlasTexSize)
                {
                    // Handle the error here
                    Debug.LogError("Error: Region does not fit in destination texture");
                }
                // Copy only Mip0
                Graphics.CopyTexture(tex, 0, 0, 0,0,quadTexSize,quadTexSize,
                tex2DAtlas, 0, 0,dstCoordX,dstCoordY);
            }
            //BUG can not write mip8-9 cuz srcWidth must bigger than 4
            // write each sub-tex mip maps, but if you save as a png file, this doesn't work,you can use hardware mipmap,
            // you should save it as asset file if you want. 
            else 
            {
                int mipCount = tex.mipmapCount;
                int mipWidth = quadTexSize;
                int mipHeight = quadTexSize;
                //write each sub tex's mip
                for (int mip = 0; mip < mipCount-2; ++mip) {
                    
                    Graphics.CopyTexture(tex, 0, mip, 0, 0, mipWidth, mipHeight,
                        tex2DAtlas, 0, mip, dstCoordX, dstCoordY);
                    mipWidth /= 2;
                    mipHeight /= 2;
                    dstCoordX >>= 1;
                    dstCoordY >>= 1;
                }
            }
        }
        
        
        tex2DAtlas.Apply();
        string assetFolderPath = "Assets" + savePath.Substring(Application.dataPath.Length);
        
        byte[] imgByte = DeCompress(tex2DAtlas).EncodeToPNG();
        File.WriteAllBytes(assetFolderPath, imgByte);
        
        //Save to Asset file (BUG) 
        
        //AssetDatabase.CreateAsset(tex2DAtlas, assetFolderPath);
        //AssetDatabase.Refresh();
        //AssetDatabase.SaveAssets();
    }

    //PackRect
    Rect[] PackTextures(Texture2D texture, Texture2D[] textures, int width, int height)
    {
        MaxRectsBinPack bp = new MaxRectsBinPack(width, height);
        Rect [] rects = new Rect[textures.Length];
       
        for(int i = 0; i < textures.Length; i++) {
            Texture2D tex = textures[i];
            SetTextureReadable(tex);
            Rect rect = bp.Insert(tex.width, tex.height, method);
            
            
            if(rect.width == 0 || rect.height == 0) {
                return PackTextures(texture, textures, width * (width <= height ? 2 : 1), height * (height < width ? 2 : 1));
            }
            rects[i] = rect;
           
        }
        texture.Reinitialize(width, height);
        texture.SetPixels(new Color[width * height]);
        for(int i = 0; i < textures.Length; i++) {
            Texture2D tex = textures[i];
            Rect rect = rects[i];
            Color[] colors = tex.GetPixels();
           
            if(rect.width != tex.width) {
                Color[] newColors = tex.GetPixels();
               
                for(int x = 0; x < rect.width; x++) {
                    for(int y = 0; y < rect.height; y++) {
                        int prevIndex = ((int)rect.height - (y + 1)) + x * (int)tex.width;
                        newColors[x + y * (int)rect.width] = colors[prevIndex];
                    }
                }
               
                colors = newColors;
            }
           
            texture.SetPixels((int)rect.x, (int)rect.y, (int)rect.width, (int)rect.height, colors);
            rect.x /= width;
            rect.y /= height;
            rect.width /= width;
            rect.height /= height;
            rects[i] = rect;
            SetTextureUnReadable(tex);
        }
        
        texture.Apply();
        return rects;
    }
    private void SaveToRect(bool overrideLoadedAsset = false)
    {
        Texture2D texRect = new Texture2D(0,0);
        PackTextures(texRect, textures.ToArray(), 1024, 1024);
        
        string assetFolderPath = "Assets" + savePath.Substring(Application.dataPath.Length);
        byte[] imgByte = DeCompress(texRect).EncodeToTGA();//EncodeToPNG
        File.WriteAllBytes(assetFolderPath, imgByte);
    }
    
    private void LoadTexturesFromTex2DArray() {
        CleanupTempTextures();

        int width = loadTexture2DArray.width;
        int height = loadTexture2DArray.height;
        UnityEngine.Experimental.Rendering.GraphicsFormat graphicsFormat = loadTexture2DArray.graphicsFormat;
        int mipMapCount = loadTexture2DArray.mipmapCount;
        UnityEngine.Experimental.Rendering.TextureCreationFlags flags = (loadTexture2DArray.mipmapCount > 1) ? 
            UnityEngine.Experimental.Rendering.TextureCreationFlags.MipChain : 
            UnityEngine.Experimental.Rendering.TextureCreationFlags.None;

        textures.Clear();
        for (int i = 0; i < loadTexture2DArray.depth; i++) {
            Texture2D temp = new Texture2D(width, height, graphicsFormat, mipMapCount, flags);
            Graphics.CopyTexture(loadTexture2DArray, i, temp, 0);
            tempLoadedTextures.Add(temp);
            textures.Add(temp);
        }
    }

    private void SetTextureReadable(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
        importer.isReadable = true;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }
    
    private void SetTextureUnReadable(Texture2D tex)
    {
        string path = AssetDatabase.GetAssetPath(tex);
        TextureImporter importer = (TextureImporter)TextureImporter.GetAtPath(path);
        importer.isReadable = false;
        EditorUtility.SetDirty(importer);
        importer.SaveAndReimport();
    }
    
    private void CleanupTempTextures() {
        for (int i = tempLoadedTextures.Count - 1; i >= 0; i--) {
            DestroyImmediate(tempLoadedTextures[i]);
        }
        tempLoadedTextures.Clear();
    }

    private void OnEnable() {
        // Create Reorderable List
        reorderableList = new ReorderableList(textures, typeof(Texture2D));
        reorderableList.elementHeight = 52;
        reorderableList.drawHeaderCallback = DrawHeader;
        reorderableList.drawElementCallback = DrawElement;
        reorderableList.onAddCallback = OnAdd;
        reorderableList.onRemoveCallback = OnRemove;
    }
    
    private void OnDisable() {
        CleanupTempTextures();
    }

    // List Callbacks

    private void DrawHeader(Rect rect) {
        EditorGUI.LabelField(rect, "Textures");
    }

    private void DrawElement(Rect rect, int index, bool active, bool focus) {
        Rect r = new Rect(rect.x, rect.y, 50, 50);
        Texture2D tex = textures[index];
        textures[index] = (Texture2D)EditorGUI.ObjectField(r, tex, typeof(Texture2D), false);

        if (tex != null) {
            r = new Rect(rect.x + 52, rect.y, rect.width - 52, 15);
            EditorGUI.LabelField(r, "Width : " + tex.width + " Height : " + tex.height);
            r = new Rect(rect.x + 52, rect.y + 15, rect.width - 52, 15);
            EditorGUI.LabelField(r, "Mipmap Count : " + tex.mipmapCount);
            r = new Rect(rect.x + 52, rect.y + 30, rect.width - 52, 15);
            EditorGUI.LabelField(r, "Format : " + tex.format);
        }
    }
    
    private void OnAdd(ReorderableList list) {
        textures.Add(Texture2D.whiteTexture);
    }

    private void OnRemove(ReorderableList list) {
        textures.RemoveAt(list.index);
    }

}
