using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// 法线平滑工具：计算平均法线并存储到切线，用于实现轮廓线渲染等效果
public class NormalTangentTool
{
    [MenuItem("Tools/平滑法线")]
    public static void WriteAverageNormalToTangentToos()
    {
        var meshFilter = Selection.activeGameObject.GetComponent<MeshFilter>();
        var mesh = meshFilter.sharedMesh;
        WirteAverageNormalToTangent(mesh);
    }
    
    // 计算每个顶点位置的平均法线，并将其存储到切线数据中
    // 算法原理：将相同位置的顶点法线相加并归一化，得到平滑的法线效果
    private static void WirteAverageNormalToTangent(Mesh mesh)
    {
        var averageNormalHash = new Dictionary<Vector3, Vector3>();
        for (var j = 0; j < mesh.vertexCount; j++)
        {
            // 如果字典中不包含当前顶点位置，则将当前顶点位置和法线添加到字典中
            if (!averageNormalHash.ContainsKey(mesh.vertices[j]))
            {
                averageNormalHash.Add(mesh.vertices[j], mesh.normals[j]);
            }
            else
            {
                averageNormalHash[mesh.vertices[j]] =
                    (averageNormalHash[mesh.vertices[j]] + mesh.normals[j]).normalized;
            }
        }
        // 将字典中的法线存储到数组中
        var averageNormals = new Vector3[mesh.vertexCount];
        for (var j = 0; j < mesh.vertexCount; j++)
        {
            averageNormals[j] = averageNormalHash[mesh.vertices[j]];
        }

        // 创建一个包含平均法线的切线
        var tangents = new Vector4[mesh.vertexCount];
        for (var j = 0; j < mesh.vertexCount; j++)
        {
            tangents[j] = new Vector4(averageNormals[j].x, averageNormals[j].y, averageNormals[j].z, 0);
        }
        // 将切线数据存储到网格中
        mesh.tangents = tangents;
    }
}