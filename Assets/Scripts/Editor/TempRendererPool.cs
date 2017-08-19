using UnityEngine;
using UnityEditor;

[CanEditMultipleObjects]
public class TempRendererPoolEditor : Editor
{
    [MenuItem("GameObject/Temp Renderer Pool", false, 10)]
    static void CreateTempRendererPool()
    {
        var group = new GameObject("Temp Renderer Pool");

        for (var i = 0; i < 32; i++)
        {
            var temp = new GameObject("Temp Renderer");
            temp.AddComponent<TempRenderer>();
            temp.transform.parent = group.transform;
        }

        Selection.activeGameObject = group;
    }
}
