using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Skinner
{

    public class SkinnerModel : ScriptableObject
    {
        // ---------------
        #region Public properties
        [SerializeField]
        private int _vertexCount;

        public int vertexCount
        {
            get { return _vertexCount; }
        }

        [SerializeField]
        private Mesh _mesh;

        public Mesh mesh
        {
            get { return _mesh; }
        }
        #endregion

        // ---------------
        #region Public methods
        #if UNITY_EDITOR

        public void Initialize(Mesh source)
        {
            // Input vertices
            var inVertices = source.vertices;
            var inNormals = source.normals;
            var inTangents = source.tangents;
            var inBoneWeight = source.boneWeights;

            // Enumerate unique vertices.
            var outVertices = new List<Vector3>();
            var outNormals = new List<Vector3>();
            var outTangents = new List<Vector4>();
            var outBoneWeights = new List<BoneWeight>();

            for (var i = 0; i < inVertices.Length; i++)
            {
                if (!outVertices.Any(_ => _ == inVertices[i]))
                {
                    outVertices.Add(inVertices[i]);
                    outNormals.Add(inNormals[i]);
                    outTangents.Add(inTangents[i]);
                    outBoneWeights.Add(inBoneWeight[i]);
                }
            }

            // Assign unique UVs to the vertices.
            var outUVs = Enumerable.Range(0, outVertices.Count)
                .Select(i => Vector2.right * (i + 0.5f) / outVertices.Count).ToList();

            // Enumerate vertex indices
            var indices = Enumerable.Range(0, outVertices.Count).ToArray();

            _mesh = Instantiate<Mesh>(source);
            _mesh.name = _mesh.name.Substring(0, _mesh.name.Length - 7);

            // Clear unused attributes.
            _mesh.colors = null;
            _mesh.uv2 = null;
            _mesh.uv3 = null;
            _mesh.uv4 = null;

            // Overwrite the vertices.
            _mesh.subMeshCount = 0;
            _mesh.SetVertices(outVertices);
            _mesh.SetNormals(outNormals);
            _mesh.SetTangents(outTangents);
            _mesh.SetUVs(0, outUVs);
            _mesh.bindposes = source.bindposes;
            _mesh.boneWeights = outBoneWeights.ToArray();

            // Add point primitives.
            _mesh.subMeshCount = 1;
            _mesh.SetIndices(indices, MeshTopology.Points, 0);

            // Update new mesh to Graphic API
            _mesh.UploadMeshData(true);

            _vertexCount = outVertices.Count;
        }

        #endif
        #endregion

        // ---------------
        #region ScriptableObject functions
        private void OnEnable()
        {
            
        }
        #endregion
    }

}