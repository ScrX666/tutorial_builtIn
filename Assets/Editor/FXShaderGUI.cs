using UnityEditor;
using UnityEngine;

// Document : https://docs.unity3d.com/ScriptReference/MaterialEditor.html
public class FXShaderGUI : ShaderGUI 
{
    // 用于记录折叠栏状态
    private bool showBaseSettings = true;
    private bool showDissolveSettings = true;
    private bool showFresnelSettings = true;
    
    // GUI样式
    private GUIStyle groupBoxStyle;
    
    // 初始化样式
    private void InitStyles()
    {
        if (groupBoxStyle == null)
        {
            groupBoxStyle = new GUIStyle(EditorStyles.helpBox);
            groupBoxStyle.margin = new RectOffset(10, 10, 10, 10);
            groupBoxStyle.padding = new RectOffset(10, 10, 10, 10);
        }
    }
    
    // --- 3 --- 绘制 OnGUI 重载函数
    public override void OnGUI (MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // 初始化样式
        InitStyles();
        
        // 获取材质对象
        Material material = materialEditor.target as Material;
        
        // 绘制标题
        GUILayout.Label("溶解效果设置", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        //  --- 4 ---找到所有需要的属性
        MaterialProperty mainTexProp = FindProperty("_MainTex", properties);
        MaterialProperty mainTexSTProp = FindProperty("_MainTex_ST", properties, false);
        
        MaterialProperty enableDissolveProp = FindProperty("_EnableDissolve", properties);
        MaterialProperty dissolveMapProp = FindProperty("_DissolveMap", properties);
        MaterialProperty dissolveMapSTProp = FindProperty("_DissolveMap_ST", properties, false);
        MaterialProperty dissolveAmountProp = FindProperty("_DissolveAmount", properties);
        MaterialProperty dissolveColorProp = FindProperty("_DissolveColor", properties);
        MaterialProperty dissolveWidthProp = FindProperty("_DissolveWidth", properties);
        
        // 找到菲涅尔效果相关属性
        MaterialProperty enableFresnelProp = FindProperty("_EnableFresnel", properties);
        MaterialProperty fresnelColorProp = FindProperty("_FresnelColor", properties);
        MaterialProperty fresnelPowerProp = FindProperty("_FresnelPower", properties);
        MaterialProperty fresnelScaleProp = FindProperty("_FresnelScale", properties);
        
        //  --- 5 --- 使用折叠栏绘制主纹理属性
        showBaseSettings = EditorGUILayout.Foldout(showBaseSettings, "基础设置", true, EditorStyles.foldoutHeader);
        if (showBaseSettings)
        {
            EditorGUILayout.BeginVertical(groupBoxStyle);
            
            materialEditor.TexturePropertySingleLine(new GUIContent("主纹理"), mainTexProp);
            
            //  --- 6 --- 添加主纹理的Tiling和Offset设置
            if (mainTexProp.textureValue != null)
            {
                EditorGUI.indentLevel++;
                materialEditor.TextureScaleOffsetProperty(mainTexProp);
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();
        
        //  --- 7 --- 使用折叠栏绘制溶解相关属性
        showDissolveSettings = EditorGUILayout.Foldout(showDissolveSettings, "溶解设置", true, EditorStyles.foldoutHeader);
        if (showDissolveSettings)
        {
            EditorGUILayout.BeginVertical(groupBoxStyle);
            
            // 绘制溶解效果开关
            bool isDissolveEnabled = material.IsKeywordEnabled("_ENABLEDISSOLVE_ON");
            EditorGUI.BeginChangeCheck();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("启用溶解效果", GUILayout.Width(EditorGUIUtility.labelWidth));
                isDissolveEnabled = EditorGUILayout.Toggle(isDissolveEnabled);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                // 设置 Keywords
                if (isDissolveEnabled)
                    material.EnableKeyword("_ENABLEDISSOLVE_ON");
                else
                    material.DisableKeyword("_ENABLEDISSOLVE_ON");
                
                enableDissolveProp.floatValue = isDissolveEnabled ? 1 : 0;
            }
            
            EditorGUILayout.Space(5);
            
            if (isDissolveEnabled)
            {
                materialEditor.TexturePropertySingleLine(new GUIContent("溶解贴图"), dissolveMapProp);
                
                //  --- 8 --- 添加溶解贴图的Tiling和Offset设置
                if (dissolveMapProp.textureValue != null)
                {
                    EditorGUI.indentLevel++;
                    materialEditor.TextureScaleOffsetProperty(dissolveMapProp);
                    EditorGUI.indentLevel--;
                }
                
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField("溶解参数", EditorStyles.boldLabel);
                
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    //  --- 9 --- 绘制溶解相关属性
                    materialEditor.RangeProperty(dissolveAmountProp, "溶解程度");
                    materialEditor.ColorProperty(dissolveColorProp, "溶解边缘颜色");
                    materialEditor.RangeProperty(dissolveWidthProp, "溶解边缘宽度");
                }
                
                EditorGUILayout.Space(5);
                //  --- 10 --- 添加溶解贴图使用说明
                EditorGUILayout.HelpBox("溶解贴图使用R通道作为溶解基准。白色区域最后溶解，黑色区域最先溶解。", MessageType.Info);
            }
            
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space();
        
        // --- 12 --- 使用折叠栏绘制菲涅尔效果相关属性
        showFresnelSettings = EditorGUILayout.Foldout(showFresnelSettings, "菲涅尔效果", true, EditorStyles.foldoutHeader);
        if (showFresnelSettings)
        {
            EditorGUILayout.BeginVertical(groupBoxStyle);
            
            // 绘制开关
            bool isFresnelEnabled = material.IsKeywordEnabled("_ENABLEFRESNEL_ON");
            EditorGUI.BeginChangeCheck();
            
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Label("启用菲涅尔效果", GUILayout.Width(EditorGUIUtility.labelWidth));
                isFresnelEnabled = EditorGUILayout.Toggle(isFresnelEnabled);
            }
            
            if (EditorGUI.EndChangeCheck())
            {
                // 设置 Keywords
                if (isFresnelEnabled)
                    material.EnableKeyword("_ENABLEFRESNEL_ON");
                else
                    material.DisableKeyword("_ENABLEFRESNEL_ON");
                
                enableFresnelProp.floatValue = isFresnelEnabled ? 1 : 0;
            }
            
            EditorGUILayout.Space(5);
            if (isFresnelEnabled)
            {
                EditorGUILayout.LabelField("菲涅尔参数", EditorStyles.boldLabel);
                
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    materialEditor.ColorProperty(fresnelColorProp, "菲涅尔颜色");
                    materialEditor.RangeProperty(fresnelPowerProp, "菲涅尔强度");
                    materialEditor.RangeProperty(fresnelScaleProp, "菲涅尔范围");
                }
            }
            
            EditorGUILayout.Space(5);
            EditorGUILayout.HelpBox("菲涅尔效果在模型边缘产生发光效果，视角越倾斜，效果越明显。", MessageType.Info);
            
            EditorGUILayout.EndVertical();
        }
    }
}