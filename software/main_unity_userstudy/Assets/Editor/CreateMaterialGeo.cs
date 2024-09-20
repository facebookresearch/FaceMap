using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Reflection;

public class CreateMaterialGeo : MonoBehaviour {

	// Use this for initialization
	[MenuItem("GameObject/Create Material Geometric-only")]
    static void CreateMaterialGeoFunction()
    {
		List<Object> meshes = Resources.LoadAll("ExampleDataset/ASAP_Meshes",typeof(Object)).Cast<Object>().ToList();
        for(int i = 0; i < meshes.Count; i++)
        {
            if (meshes[i].name.Substring(0,4) == "head")
            {
                string objModelPath = "Assets/Resources/ExampleDataset/ASAP_Meshes/" + meshes[i].name + ".obj";
                Debug.Log(objModelPath);
                var objAssetImporter = AssetImporter.GetAtPath(objModelPath);
                string materialToAssignPath = "Assets/Resources/ExampleDataset/headmat.mat";

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




}
