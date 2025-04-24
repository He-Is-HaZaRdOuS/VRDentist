using System;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System.Threading;
using System.Globalization;

namespace Utils
{
    /// <summary>
    /// Happily kanged and modified below code from https://discussions.unity.com/u/b3x2088/summary
    /// </summary>
    public static class ExportMeshToOBJ
    {
        /// <summary>
        /// Whether to apply matrix transformations while exporting a GameObject to OBJ.
        /// </summary>
        public static bool ApplyObjTransformations = true;

        public static void ExportToObjAsync(GameObject obj, string writePath)
        {
            if (obj == null || !obj.TryGetComponent(out MeshFilter meshFilter) || meshFilter.sharedMesh == null)
            {
                Debug.Log("No valid mesh found.");
                return;
            }

            /* TODO: Reverse nesting of MainThreadUtility.ExecuteOnMainThread with ThreadPool.QueueUserWorkItem 
            ** to actually execute asynchronously while keeping restricted API calls on main thread.
            */
            // Dispatch Unity-related work to the main thread
            MainThreadUtility.ExecuteOnMainThread(() =>
            {
                string objName = obj.name;
                Matrix4x4 transformMatrix = obj.transform.localToWorldMatrix;

                // Get the OBJ data (this part can remain on the main thread)
                string objData = GetMeshOBJ(objName, meshFilter.sharedMesh, transformMatrix);

                // Perform file I/O asynchronously on a background thread
                ThreadPool.QueueUserWorkItem(_ =>
                {
                    try
                    {
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

        // This is used for direct exporting in editor (synchronous version)
        private static void ExportToObjImpl(GameObject obj, string writePath)
        {
            if (obj == null)
            {
                Debug.Log("No object selected.");
                return;
            }

            if (!obj.TryGetComponent(out MeshFilter meshFilter))
            {
                Debug.Log("No MeshFilter is found in selected GameObject.", obj);
                return;
            }

            if (meshFilter.sharedMesh == null)
            {
                Debug.Log("No mesh is found in selected GameObject.", obj);
                return;
            }

            CultureInfo previousCurrentCulture = Thread.CurrentThread.CurrentCulture;

            try
            {
                using StreamWriter writer = new StreamWriter(writePath);

                // Set the culture to always be invariant/standard
                Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
                writer.Write(GetMeshOBJ(obj.name, meshFilter.sharedMesh, ApplyObjTransformations ? obj.transform.localToWorldMatrix : Matrix4x4.identity));
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[ExportToObjImpl] An exception occured while exporting selected GameObject : {e.Message}\n{e.StackTrace}", obj);
            }

            Thread.CurrentThread.CurrentCulture = previousCurrentCulture;
        }

        // Method to generate the mesh OBJ string
        public static string GetMeshOBJ(string name, Mesh mesh, Matrix4x4 objTransform)
        {
            StringBuilder sb = new StringBuilder();

            foreach (Vector3 v in mesh.vertices)
            {
                Vector3 writeV = (objTransform != Matrix4x4.identity && objTransform != default) ? objTransform.MultiplyPoint(v) : v;
                sb.Append(string.Format("v {0} {1} {2}\n", writeV.x, writeV.y, writeV.z));
            }

            // Also export UV's
            foreach (Vector3 v in mesh.uv)
            {
                sb.Append(string.Format("vt {0} {1} {2}\n", v.x, v.y, v.z));
            }

            foreach (Vector3 v in mesh.normals)
            {
                sb.Append(string.Format("vn {0} {1} {2}\n", v.x, v.y, v.z));
            }

            for (int material = 0; material < mesh.subMeshCount; material++)
            {
                sb.Append(string.Format("\ng {0}\n", name));
                int[] triangles = mesh.GetTriangles(material);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    sb.Append(string.Format("f {0}/{0} {1}/{1} {2}/{2}\n",
                    triangles[i] + 1,
                    triangles[i + 1] + 1,
                    triangles[i + 2] + 1));
                }
            }

            return sb.ToString();
        }

        private static string PreviousSelectedDirectory = "";

        [MenuItem("GameObject/Export to OBJ")]
        static void ExportToOBJ()
        {
            ApplyObjTransformations = false;

            GameObject obj = Selection.activeObject as GameObject;
            string path = EditorUtility.SaveFilePanel("Export OBJ", PreviousSelectedDirectory, obj.name, "obj");

            ExportToObjImpl(obj, path);
        }

        [MenuItem("GameObject/Export to OBJs")]
        static void ExportToOBJs()
        {
            ApplyObjTransformations = false;

            GameObject[] objs = Selection.gameObjects;
            if (objs.Length < 1)
            {
                Debug.Log("No object selected.");
                return;
            }
            string directory = EditorUtility.SaveFolderPanel("Export OBJs to", PreviousSelectedDirectory, "OBJFiles");
            PreviousSelectedDirectory = directory;

            foreach (GameObject obj in objs)
            {
                ExportToObjImpl(obj, Path.Combine(directory, obj.name + ".obj"));
            }
        }

        [MenuItem("GameObject/Export to OBJ (Apply GameObject Transform)")]
        static void ExportToOBJWithTransformations()
        {
            ApplyObjTransformations = true;

            GameObject obj = Selection.activeObject as GameObject;
            string path = EditorUtility.SaveFilePanel("Export OBJ", PreviousSelectedDirectory, obj.name, "obj");

            ExportToObjImpl(obj, path);
        }

        [MenuItem("GameObject/Export to OBJs (Apply GameObject Transform)")]
        static void ExportToOBJsWithTransformations()
        {
            ApplyObjTransformations = true;

            GameObject[] objs = Selection.gameObjects;
            if (objs.Length < 1)
            {
                Debug.Log("No object selected.");
                return;
            }
            string directory = EditorUtility.SaveFolderPanel("Export OBJs to", PreviousSelectedDirectory, "OBJFiles");
            PreviousSelectedDirectory = directory;

            foreach (GameObject obj in objs)
            {
                ExportToObjImpl(obj, Path.Combine(directory, obj.name + ".obj"));
            }
        }
    }
}
