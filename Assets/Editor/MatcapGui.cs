using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;

public class MatcapGui : ShaderGUI
{
    override public void OnGUI(MaterialEditor materialEditor, MaterialProperty[] properties)
    {
        // render the shader properties using the default GUI
        base.OnGUI(materialEditor, properties);
    }
}
