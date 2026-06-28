using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace RoguelikeMaster.EditorTools
{
    public static class LowPolyTreeGenerator
    {
        private const string RootFolder = "Assets/Art/Environment/LowPolyTree";
        private const string MeshFolder = RootFolder + "/Meshes";
        private const string MaterialFolder = RootFolder + "/Materials";
        private const string PrefabPath = RootFolder + "/LowPolyTree.prefab";
        private const string TrunkMeshPath = MeshFolder + "/LowPolyTree_Trunk.asset";
        private const string LeavesMeshPath = MeshFolder + "/LowPolyTree_Leaves.asset";
        private const string TrunkMaterialPath = MaterialFolder + "/LowPolyTree_Trunk.mat";
        private const string LeavesMaterialPath = MaterialFolder + "/LowPolyTree_Leaves.mat";

        [MenuItem("Tools/Art/Generate Low Poly Tree")]
        public static void Generate()
        {
            EnsureFolder("Assets/Art");
            EnsureFolder("Assets/Art/Environment");
            EnsureFolder(RootFolder);
            EnsureFolder(MeshFolder);
            EnsureFolder(MaterialFolder);

            DeleteAssetIfExists(TrunkMeshPath);
            DeleteAssetIfExists(LeavesMeshPath);
            DeleteAssetIfExists(TrunkMaterialPath);
            DeleteAssetIfExists(LeavesMaterialPath);
            DeleteAssetIfExists(PrefabPath);

            var trunkMesh = BuildPrismMesh(6, 0.18f, 0.14f, 1.45f, true, true, 0.012f);
            AssetDatabase.CreateAsset(trunkMesh, TrunkMeshPath);

            var leavesMesh = BuildConeMesh(8, 0.95f, 1.05f, true, 0.08f);
            AssetDatabase.CreateAsset(leavesMesh, LeavesMeshPath);

            var trunkMaterial = CreateLitMaterial("LowPolyTree_Trunk", new Color(0.36f, 0.24f, 0.11f));
            AssetDatabase.CreateAsset(trunkMaterial, TrunkMaterialPath);

            var leavesMaterial = CreateLitMaterial("LowPolyTree_Leaves", new Color(0.24f, 0.48f, 0.18f));
            AssetDatabase.CreateAsset(leavesMaterial, LeavesMaterialPath);

            var root = new GameObject("LowPolyTree");
            try
            {
                var trunk = CreateChild(root.transform, "Trunk", trunkMesh, trunkMaterial, Vector3.zero, Vector3.zero, Vector3.one);
                trunk.transform.localPosition = new Vector3(0f, 0f, 0f);

                CreateChild(root.transform, "Leaves_Lower", leavesMesh, leavesMaterial,
                    new Vector3(0f, 1.05f, 0f), new Vector3(6f, 18f, 0f), new Vector3(1.18f, 1.05f, 1.18f));
                CreateChild(root.transform, "Leaves_Middle", leavesMesh, leavesMaterial,
                    new Vector3(0f, 1.58f, 0f), new Vector3(-7f, 42f, 0f), new Vector3(0.95f, 0.86f, 0.95f));
                CreateChild(root.transform, "Leaves_Top", leavesMesh, leavesMaterial,
                    new Vector3(0f, 2.06f, 0f), new Vector3(10f, 72f, 0f), new Vector3(0.68f, 0.64f, 0.68f));

                var prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Selection.activeObject = prefab;
                EditorGUIUtility.PingObject(prefab);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }

            Debug.Log("Low poly tree generated at " + PrefabPath);
        }

        private static Material CreateLitMaterial(string name, Color color)
        {
            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            var material = new Material(shader)
            {
                name = name,
                enableInstancing = true
            };

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor("_BaseColor", color);
            }

            if (material.HasProperty("_Color"))
            {
                material.SetColor("_Color", color);
            }

            if (material.HasProperty("_Smoothness"))
            {
                material.SetFloat("_Smoothness", 0f);
            }

            if (material.HasProperty("_Glossiness"))
            {
                material.SetFloat("_Glossiness", 0f);
            }

            if (material.HasProperty("_Metallic"))
            {
                material.SetFloat("_Metallic", 0f);
            }

            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 0f);
            }

            return material;
        }

        private static GameObject CreateChild(Transform parent, string name, Mesh mesh, Material material, Vector3 localPosition, Vector3 localEulerAngles, Vector3 localScale)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            child.transform.localPosition = localPosition;
            child.transform.localRotation = Quaternion.Euler(localEulerAngles);
            child.transform.localScale = localScale;

            var filter = child.AddComponent<MeshFilter>();
            filter.sharedMesh = mesh;

            var renderer = child.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;

            return child;
        }

        private static Mesh BuildPrismMesh(int sides, float bottomRadius, float topRadius, float height, bool includeBottom, bool includeTop, float twist)
        {
            var mesh = new Mesh { name = "LowPolyTree_Trunk" };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();

            for (var i = 0; i < sides; i++)
            {
                var angle0 = Mathf.PI * 2f * i / sides + twist;
                var angle1 = Mathf.PI * 2f * (i + 1) / sides + twist;

                var b0 = Polar(bottomRadius, 0f, angle0);
                var b1 = Polar(bottomRadius, 0f, angle1);
                var t0 = Polar(topRadius, height, angle0);
                var t1 = Polar(topRadius, height, angle1);

                AddQuad(vertices, triangles, b0, t0, t1, b1);

                if (includeBottom)
                {
                    AddTriangle(vertices, triangles, Vector3.zero, b1, b0);
                }

                if (includeTop)
                {
                    AddTriangle(vertices, triangles, new Vector3(0f, height, 0f), t0, t1);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildConeMesh(int sides, float radius, float height, bool includeBase, float twist)
        {
            var mesh = new Mesh { name = "LowPolyTree_Leaves" };
            var vertices = new List<Vector3>();
            var triangles = new List<int>();
            var apex = new Vector3(0f, height, 0f);

            for (var i = 0; i < sides; i++)
            {
                var angle0 = Mathf.PI * 2f * i / sides + twist;
                var angle1 = Mathf.PI * 2f * (i + 1) / sides + twist;
                var p0 = Polar(radius, 0f, angle0);
                var p1 = Polar(radius, 0f, angle1);

                AddTriangle(vertices, triangles, apex, p0, p1);

                if (includeBase)
                {
                    AddTriangle(vertices, triangles, Vector3.zero, p1, p0);
                }
            }

            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Vector3 Polar(float radius, float y, float angle)
        {
            return new Vector3(Mathf.Cos(angle) * radius, y, Mathf.Sin(angle) * radius);
        }

        private static void AddTriangle(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c)
        {
            var start = vertices.Count;
            vertices.Add(a);
            vertices.Add(b);
            vertices.Add(c);
            triangles.Add(start);
            triangles.Add(start + 1);
            triangles.Add(start + 2);
        }

        private static void AddQuad(List<Vector3> vertices, List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            AddTriangle(vertices, triangles, a, b, c);
            AddTriangle(vertices, triangles, a, c, d);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            var parent = System.IO.Path.GetDirectoryName(path)?.Replace('\\', '/');
            var folderName = System.IO.Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent))
            {
                return;
            }

            if (!AssetDatabase.IsValidFolder(parent))
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, folderName);
        }

        private static void DeleteAssetIfExists(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<Object>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
