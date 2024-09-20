using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

public class CreateMaterial : MonoBehaviour {
	static void assign_common_mat_to_geometry(List<Object> meshes)
	{
		Debug.Log($"[JOD Application] Assign common headX materials for {meshes.Count} meshes");
		for(int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i].name.Substring(0,4) == "head")
            {
                var headname = meshes[i].name.Substring(0,5); // e.g. headX
                string objModelPath = "Assets/Resources/ExampleDataset/ASAP_Meshes/" + meshes[i].name + ".obj";
                Debug.Log(objModelPath);
                var objAssetImporter = AssetImporter.GetAtPath(objModelPath);
                string materialToAssignPath = "Assets/Resources/" + headname + "_ref.mat";

                var modelImporter = objAssetImporter as ModelImporter;
                var sourceMaterials = typeof(ModelImporter)
                .GetProperty("sourceMaterials", BindingFlags.NonPublic | BindingFlags.Instance)?
                .GetValue(modelImporter) as AssetImporter.SourceAssetIdentifier[];

                Material matToUse = (Material)AssetDatabase.LoadAssetAtPath(materialToAssignPath, typeof(Material));

                foreach (var identifier in sourceMaterials ?? Enumerable.Empty<AssetImporter.SourceAssetIdentifier>())
                {
                    modelImporter.AddRemap(identifier, matToUse);
                    Debug.Log($"Assigned remapped material to {objModelPath}: {materialToAssignPath}");
                }

                AssetDatabase.WriteImportSettingsIfDirty(objModelPath);
            }
        }
	}

	static void assign_mat_to_textures(List<Texture2D> textures)
	{
		Debug.Log($"[JOD Application] Loading textures: {textures.Count}");

		for(int i = 0; i< textures.Count;i++)
		{
			Material material = new Material(Shader.Find("Universal Render Pipeline/Simple Lit"));
			material.SetTexture("_BaseMap",textures[i]);
			AssetDatabase.CreateAsset(material, "Assets/Resources/ExampleDataset/Materials/"+textures[i].name+".mat");

			// Print the path of the created asset
        	Debug.Log(AssetDatabase.GetAssetPath(material));
			string objModelPath = "Assets/Resources/ExampleDataset/ASAP_Meshes/" + textures[i].name + ".obj";
			var objAssetImporter = AssetImporter.GetAtPath(objModelPath);
			string materialToAssignPath = "Assets/Resources/ExampleDataset/Materials/" + textures[i].name + ".mat";

			Debug.Log("Loading material: " + materialToAssignPath);

			var modelImporter = objAssetImporter as ModelImporter;
			var sourceMaterials = typeof(ModelImporter)
			.GetProperty("sourceMaterials", BindingFlags.NonPublic | BindingFlags.Instance)?
			.GetValue(modelImporter) as AssetImporter.SourceAssetIdentifier[];

			Material matToUse = (Material)AssetDatabase.LoadAssetAtPath(materialToAssignPath, typeof(Material));

			foreach (var identifier in sourceMaterials ?? Enumerable.Empty<AssetImporter.SourceAssetIdentifier>())
			{
				modelImporter.AddRemap(identifier, matToUse);
				Debug.Log($"Assigned remapped material to {objModelPath}: {materialToAssignPath}");
			}

			AssetDatabase.WriteImportSettingsIfDirty(objModelPath);
		}
	}
	// Use this for initialization
	[MenuItem("Assets/[FaceMap] Assign Customized Materials")]
    public static void CreateMaterialFunction()
    {
		List<Object> meshes = Resources.LoadAll("ExampleDataset/ASAP_Meshes",typeof(Object)).Cast<Object>().ToList();
		assign_common_mat_to_geometry(meshes);

		// if texture is present, assign instead.
		List<Texture2D> textures = Resources.LoadAll("ExampleDataset/Textures", typeof(Texture2D)).Cast<Texture2D>().ToList();
		assign_mat_to_textures(textures);

	}




}
