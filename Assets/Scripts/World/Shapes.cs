using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace RSPP
{
    /// <summary>
    /// Small helpers for building the low-poly world out of Unity's built-in shapes.
    /// Objects made here have NO colliders (cheaper for Quest); colliders are added
    /// only where an interaction needs one.
    /// </summary>
    public static class Shapes
    {
        private static readonly Dictionary<PrimitiveType, Mesh> meshCache = new Dictionary<PrimitiveType, Mesh>();
        private static readonly Dictionary<int, Material> materialCache = new Dictionary<int, Material>();
        private static Material baseMaterial;
        private static Material primitiveDefaultMaterial;

        /// <summary>Clears cached objects. Called once when the game is built.</summary>
        public static void ResetCaches()
        {
            meshCache.Clear();
            materialCache.Clear();
            baseMaterial = null;
            primitiveDefaultMaterial = null;
            starMesh = null;
        }

        // ------------------------------------------------------------------ Meshes

        /// <summary>Returns Unity's built-in cube / sphere / cylinder / capsule mesh.</summary>
        public static Mesh PrimitiveMesh(PrimitiveType type)
        {
            Mesh mesh;
            if (meshCache.TryGetValue(type, out mesh) && mesh != null) return mesh;

            GameObject temp = GameObject.CreatePrimitive(type);
            mesh = temp.GetComponent<MeshFilter>().sharedMesh;
            if (primitiveDefaultMaterial == null)
                primitiveDefaultMaterial = temp.GetComponent<MeshRenderer>().sharedMaterial;
            temp.SetActive(false);
            Object.Destroy(temp);

            meshCache[type] = mesh;
            return mesh;
        }

        // ------------------------------------------------------------------ Materials

        private static Material BaseMaterial()
        {
            if (baseMaterial != null) return baseMaterial;

            // Built-in render pipeline: use the small Standard material shipped in Resources,
            // which guarantees the shader is included in Quest builds.
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Material loaded = Resources.Load<Material>("Materials/BaseLit");
                if (loaded != null && loaded.shader != null && loaded.shader.isSupported)
                    baseMaterial = loaded;
            }

            // Fallback (also used if the project is switched to URP): Unity's default primitive material.
            if (baseMaterial == null)
            {
                PrimitiveMesh(PrimitiveType.Cube);
                baseMaterial = primitiveDefaultMaterial;
            }
            return baseMaterial;
        }

        /// <summary>Returns a shared, matte material with the given color (one material per color).</summary>
        public static Material Mat(Color color)
        {
            Color32 c = color;
            int key = (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;

            Material material;
            if (materialCache.TryGetValue(key, out material) && material != null) return material;

            material = new Material(BaseMaterial());
            material.name = "RSPP_" + ColorUtility.ToHtmlStringRGB(color);
            if (material.HasProperty("_Color")) material.SetColor("_Color", color);
            if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", 0.1f);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.1f);
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", 0f);
            material.enableInstancing = true;

            materialCache[key] = material;
            return material;
        }

        // ------------------------------------------------------------------ Objects

        /// <summary>Creates a mesh object without a collider.</summary>
        public static GameObject Make(string name, Transform parent, Mesh mesh, Vector3 localPosition, Vector3 localScale, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = go.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = Mat(color);
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.lightProbeUsage = LightProbeUsage.Off;
            renderer.reflectionProbeUsage = ReflectionProbeUsage.Off;
            return go;
        }

        /// <summary>A box. <paramref name="size"/> is the full width, height and depth in metres.</summary>
        public static GameObject Box(string name, Transform parent, Vector3 localPosition, Vector3 size, Color color)
        {
            return Make(name, parent, PrimitiveMesh(PrimitiveType.Cube), localPosition, size, color);
        }

        public static GameObject Sphere(string name, Transform parent, Vector3 localPosition, float diameter, Color color)
        {
            return Make(name, parent, PrimitiveMesh(PrimitiveType.Sphere), localPosition, Vector3.one * diameter, color);
        }

        /// <summary>An upright cylinder (its axis is the local Y axis).</summary>
        public static GameObject Cylinder(string name, Transform parent, Vector3 localPosition, float diameter, float height, Color color)
        {
            // Unity's cylinder mesh is 2 units tall and 1 unit wide.
            return Make(name, parent, PrimitiveMesh(PrimitiveType.Cylinder), localPosition,
                new Vector3(diameter, height * 0.5f, diameter), color);
        }

        public static void SetColor(GameObject go, Color color)
        {
            var renderer = go.GetComponent<MeshRenderer>();
            if (renderer != null) renderer.sharedMaterial = Mat(color);
        }

        // ------------------------------------------------------------------ Custom flat meshes

        /// <summary>
        /// A flat pie slice in the local XY plane, facing local -Z (towards the viewer).
        /// Angles are in degrees, counter-clockwise from the +X axis.
        /// </summary>
        public static Mesh WedgeMesh(float startAngle, float endAngle, float radius, int steps)
        {
            var vertices = new Vector3[steps + 2];
            var normals = new Vector3[steps + 2];
            var triangles = new int[steps * 3];

            vertices[0] = Vector3.zero;
            normals[0] = Vector3.back;
            for (int i = 0; i <= steps; i++)
            {
                float angle = Mathf.Lerp(startAngle, endAngle, i / (float)steps) * Mathf.Deg2Rad;
                vertices[i + 1] = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                normals[i + 1] = Vector3.back;
            }
            for (int i = 0; i < steps; i++)
            {
                // Clockwise when seen from -Z, which is Unity's front-face winding.
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 2;
                triangles[i * 3 + 2] = i + 1;
            }

            var mesh = new Mesh();
            mesh.name = "Wedge";
            mesh.vertices = vertices;
            mesh.normals = normals;
            mesh.triangles = triangles;
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh starMesh;

        /// <summary>
        /// A flat, two-sided five-point star in the local XY plane with an outer radius of 0.5.
        /// Scale the object to the size you need.
        /// </summary>
        public static Mesh StarMesh()
        {
            if (starMesh != null) return starMesh;

            const int points = 10;
            const float outer = 0.5f;
            const float inner = 0.21f;

            // Front side (facing -Z) uses vertices 0..10, back side (facing +Z) uses 11..21.
            var vertices = new Vector3[(points + 1) * 2];
            var normals = new Vector3[(points + 1) * 2];
            var triangles = new int[points * 3 * 2];
            int back = points + 1;

            vertices[0] = Vector3.zero;
            vertices[back] = Vector3.zero;
            normals[0] = Vector3.back;
            normals[back] = Vector3.forward;

            for (int i = 0; i < points; i++)
            {
                float radius = i % 2 == 0 ? outer : inner;
                float angle = (90f + i * 36f) * Mathf.Deg2Rad;
                var p = new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius, 0f);
                vertices[1 + i] = p;
                vertices[back + 1 + i] = p;
                normals[1 + i] = Vector3.back;
                normals[back + 1 + i] = Vector3.forward;
            }

            for (int i = 0; i < points; i++)
            {
                int current = 1 + i;
                int next = 1 + (i + 1) % points;

                triangles[i * 6] = 0;
                triangles[i * 6 + 1] = next;
                triangles[i * 6 + 2] = current;

                triangles[i * 6 + 3] = back;
                triangles[i * 6 + 4] = back + current;
                triangles[i * 6 + 5] = back + next;
            }

            starMesh = new Mesh();
            starMesh.name = "Star";
            starMesh.vertices = vertices;
            starMesh.normals = normals;
            starMesh.triangles = triangles;
            starMesh.RecalculateBounds();
            return starMesh;
        }

        /// <summary>Creates a star object. With no rotation it stands upright facing local -Z.</summary>
        public static GameObject Star(string name, Transform parent, Vector3 localPosition, float size, Color color)
        {
            return Make(name, parent, StarMesh(), localPosition, Vector3.one * size, color);
        }
    }
}
