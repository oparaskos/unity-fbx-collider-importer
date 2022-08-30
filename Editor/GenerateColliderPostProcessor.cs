using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace UBXColliderImporter.Editor
{
    public class GenerateColliderPostProcessor : AssetPostprocessor
    {
        private const string CONFIG_FLAG = "UBXColliderImporter.Editor.GenerateColliderPostProcessor";
        private const string MENU_PATH = "Tools/Import/Enable Collider Generation";

        [MenuItem(MENU_PATH)]
        static void ToggleColliderGeneration()
        {
            var betterColliderGenerationEnabled = EditorPrefs.GetBool(CONFIG_FLAG, false);
            EditorPrefs.SetBool(CONFIG_FLAG, !betterColliderGenerationEnabled);
        }

        [MenuItem(MENU_PATH, true)]
        static bool ValidateToggleColliderGeneration()
        {
            var betterColliderGenerationEnabled = EditorPrefs.GetBool(CONFIG_FLAG, false);
            Menu.SetChecked(MENU_PATH, betterColliderGenerationEnabled);
            return true;
        }

        void OnPostprocessModel(GameObject g)
        {
            if (!EditorPrefs.GetBool(CONFIG_FLAG, false))
                return;

            List<Transform> transformsToDestroy = new List<Transform>();
            //Skip the root
            foreach (Transform child in g.transform)
            {
                GenerateCollider(child, transformsToDestroy, g);
            }

            for (int i = transformsToDestroy.Count - 1; i >= 0; --i)
            {
                if (transformsToDestroy[i] != null)
                {
                    GameObject.DestroyImmediate(transformsToDestroy[i].gameObject);
                }
            }
        }
        bool DetectNamingConvention(Transform t, string convention)
        {
            bool result = false;
            if (t.gameObject.TryGetComponent(out MeshFilter meshFilter))
            {
                var lowercaseMeshName = meshFilter.sharedMesh.name.ToLower();
                result = lowercaseMeshName.StartsWith($"{convention}_");
            }

            if (!result)
            {
                var lowercaseName = t.name.ToLower();
                result = lowercaseName.StartsWith($"{convention}_");
            }

            return result;
        }

        void GenerateCollider(Transform t, List<Transform> transformsToDestroy, GameObject g)
        {
            foreach (Transform child in t.transform)
            {
                GenerateCollider(child, transformsToDestroy, g);
            }

            if (DetectNamingConvention(t, "ubx"))
            {
                AddBoxCollider(t, g);
                transformsToDestroy.Add(t);
            }
            else if (DetectNamingConvention(t, "ucp"))
            {
                AddCapsuleCollider(t, g);
                transformsToDestroy.Add(t);
            }
            else if (DetectNamingConvention(t,"usp"))
            {
                AddCollider<SphereCollider>(t, g);
                transformsToDestroy.Add(t);
            }
            else if (DetectNamingConvention(t, "ucx"))
            {
                TransformSharedMesh(t.GetComponent<MeshFilter>());
                var collider = AddCollider<MeshCollider>(t, g);
                collider.convex = true;
                transformsToDestroy.Add(t);
            }
            else if (DetectNamingConvention(t,"umc"))
            {
                TransformSharedMesh(t.GetComponent<MeshFilter>());
                AddCollider<MeshCollider>(t, g);
                transformsToDestroy.Add(t);
            }
        }

        void TransformSharedMesh(MeshFilter meshFilter)
        {
            if (meshFilter == null)
                return;

            var transform = meshFilter.transform;
            var mesh = meshFilter.sharedMesh;
            var vertices = mesh.vertices;

            for (int i = 0; i < vertices.Length; ++i)
            {
                vertices[i] = transform.TransformPoint(vertices[i]);
                vertices[i] = transform.parent.InverseTransformPoint(vertices[i]);
            }

            mesh.SetVertices(vertices);
        }

        BoxCollider AddBoxCollider(Transform t, GameObject g)
        {
            BoxCollider collider = t.parent.gameObject.AddComponent<BoxCollider>();
            Bounds bounds = RotatedBounds(t);

            collider.center = bounds.center;
            collider.size = bounds.size;
            return collider;
        }

        Bounds RotatedBounds(Transform t) {
            Bounds bounds = t.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 newCenter = (t.rotation * (bounds.center - t.position)) + t.position;
            Vector3 newExtents = (t.rotation * (bounds.extents - bounds.center)) + newCenter;
            bounds.extents = newExtents;
            bounds.center = newCenter;
            return bounds;
        }


        CapsuleCollider AddCapsuleCollider(Transform t, GameObject g)
        {
            CapsuleCollider collider = t.parent.gameObject.AddComponent<CapsuleCollider>();
            Bounds bounds = RotatedBounds(t);

            float x = Mathf.Abs(bounds.size.x);
            float y = Mathf.Abs(bounds.size.y);
            float z = Mathf.Abs(bounds.size.z);

            float longAxis = Mathf.Max(x, y, z);
            int direction = longAxis == x ? 0 : longAxis == y ? 1 : 2;

            float height = longAxis;
            float radius = Mathf.Min(x, y, z);

            collider.center = bounds.center;
            collider.direction = direction;
            collider.height = height;
            collider.radius = radius;
            return collider;
        }


        T AddCollider<T>(Transform t, GameObject g) where T : Collider
        {
            if(1 - Mathf.Abs(Quaternion.Dot(g.transform.rotation, t.rotation)) > 0.1f) {
                // TODO: Straight copy generated collider from child to parent doesn't respect rotation changes.
                Debug.Warn("Collision mesh transform doesn't match the parent transform rotation, Colliders may not have translated correctly.");
            }

            T collider = t.gameObject.AddComponent<T>();
            T parentCollider = t.parent.gameObject.AddComponent<T>();

            EditorUtility.CopySerialized(collider, parentCollider);
            
            SerializedObject parentColliderSo = new SerializedObject(parentCollider);
            var parentCenterProperty = parentColliderSo.FindProperty("m_Center");
            if (parentCenterProperty != null)
            {
                SerializedObject colliderSo = new SerializedObject(collider);
                var colliderCenter = colliderSo.FindProperty("m_Center");
                var worldSpaceColliderCenter = t.TransformPoint(colliderCenter.vector3Value);

                parentCenterProperty.vector3Value = t.parent.InverseTransformPoint(worldSpaceColliderCenter);
                parentColliderSo.ApplyModifiedPropertiesWithoutUndo();
            }

            return parentCollider;
        }
    }
}