using System;
using UnityEngine;
using System.IO;
using System.Threading;

namespace Utils
{
    public class RuntimeExporter : MonoBehaviour
    {
        public static void ExportToObjAsync(GameObject obj, string writePath)
        {
            if (obj == null || !obj.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
            {
                Debug.Log("No valid mesh found.");
                return;
            }

            // Validate the writePath before proceeding
            if (string.IsNullOrWhiteSpace(writePath))
            {
                Debug.LogWarning("Invalid write path provided. Cannot export OBJ.");
                return;
            }

            // Prepare the mesh data on the main thread
            MainThreadUtility.ExecuteOnMainThread(() =>
            {
                string objName = obj.name;
                Matrix4x4 transformMatrix = obj.transform.localToWorldMatrix;
                string objData = ExportMeshToOBJ.GetMeshOBJ(objName, meshFilter.sharedMesh, transformMatrix);

                // Log the generated OBJ data
                if (string.IsNullOrEmpty(objData))
                {
                    Debug.LogWarning("OBJ data is empty or not generated.");
                    return;
                }

                Debug.Log($"Generated OBJ Data (first 100 chars): {objData.Substring(0, 100)}...");

                // Proceed with file I/O asynchronously
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
                        // Get the directory from the write path and create if necessary
                        string directoryPath = Path.GetDirectoryName(writePath);
                        if (!Directory.Exists(directoryPath))
                        {
                            Debug.LogWarning($"Directory does not exist: {directoryPath}. Creating directory.");
                            Directory.CreateDirectory(directoryPath); // Create the directory if it doesn't exist
                        }

                        // Write the OBJ data to the file
                        File.WriteAllText(writePath, objData);
                        Debug.Log($"✅ Exported OBJ to: {writePath}");
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"❌ Failed to export OBJ: {e.Message}\n{e.StackTrace}");
                    }
                });
            });
        }
    }
}