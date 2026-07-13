using UnityEngine;
using UnityEditor;

public class CreateMaterials : MonoBehaviour
{
    [MenuItem("Tools/Create Runtime Materials")]
    public static void Create()
    {
        string path = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(path))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        CreateMat(path, "Mat_Ground", new Color(0.18f, 0.65f, 0.35f));
        CreateMat(path, "Mat_Gray", Color.gray * 0.7f);
        CreateMat(path, "Mat_Orange", new Color(1f, 0.6f, 0f));
        CreateMat(path, "Mat_Steel", new Color(0.55f, 0.55f, 0.58f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Materials created in Resources folder.");
    }

    static void CreateMat(string folder, string name, Color color)
    {
        string matPath = $"{folder}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (mat == null)
        {
            mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(mat, matPath);
        }
        mat.color = color;
        EditorUtility.SetDirty(mat);
    }
}
