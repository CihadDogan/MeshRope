using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MeshRope
{
    public class RopeRenderer : MonoBehaviour
    {
        // Components
        private Mesh _Mesh;
        private MeshFilter _MeshFilter;
        private MeshRenderer _MeshRenderer;
        private GameObject _SphereStart;
        private GameObject _SphereEnd;

        // Variables
        private Vector3[] _Positions;
        [SerializeField] int _Sides = 6;
        public float _Radius = 0.2f;
        private Vector3[] _vertices;

        public Material material
        {
            get { return _MeshRenderer.material; }
            set { _MeshRenderer.material = value; }
        }

        void Awake()
        {
            _MeshFilter = GetComponent<MeshFilter>();
            if (_MeshFilter == null)
            {
                _MeshFilter = gameObject.AddComponent<MeshFilter>();
            }

            _MeshRenderer = GetComponent<MeshRenderer>();
            if (_MeshRenderer == null)
            {
                _MeshRenderer = gameObject.AddComponent<MeshRenderer>();
            }

            _Mesh = new Mesh();
            _MeshFilter.mesh = _Mesh;

            _SphereStart = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _SphereEnd = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        }

        public void SetPositions(Vector3[] positions)
        {
            _Positions = positions;

            // Tube
            GenerateMesh();
        }

        private void GenerateMesh()
        {
            if (_Mesh == null || _Positions == null || _Positions.Length <= 1)
            {
                _Mesh = new Mesh();
                return;
            }

            // Start
            _SphereStart.transform.SetParent(transform);
            _SphereStart.transform.localScale = Vector3.one * _Radius * 1.85f;
            _SphereStart.transform.position = _Positions[0];
            _SphereStart.GetComponent<MeshRenderer>().material = material;

            // End
            _SphereEnd.transform.SetParent(transform);
            _SphereEnd.transform.localScale = Vector3.one * _Radius * 1.85f;
            _SphereEnd.transform.position = _Positions[_Positions.Length - 1];
            _SphereEnd.GetComponent<MeshRenderer>().material = material;

            // Tube
            var verticesLength = _Sides * _Positions.Length;
            if (_vertices == null || _vertices.Length != verticesLength)
            {
                _vertices = new Vector3[verticesLength];

                var indices = GenerateIndices();
                var uvs = GenerateUVs();

                if (verticesLength > _Mesh.vertexCount)
                {
                    _Mesh.vertices = _vertices;
                    _Mesh.triangles = indices;
                    _Mesh.uv = uvs;
                }
                else
                {
                    _Mesh.triangles = indices;
                    _Mesh.vertices = _vertices;
                    _Mesh.uv = uvs;
                }
            }

            var currentVertIndex = 0;

            for (int i = 0; i < _Positions.Length; i++)
            {
                var circle = CalculateCircle(i);
                foreach (var vertex in circle)
                {
                    _vertices[currentVertIndex++] = transform.InverseTransformPoint(vertex);
                }
            }

            _Mesh.vertices = _vertices;
            _Mesh.RecalculateNormals();
            _Mesh.RecalculateBounds();

            _MeshFilter.mesh = _Mesh;
        }

        private Vector2[] GenerateUVs()
        {
            var uvs = new Vector2[_Positions.Length * _Sides];

            for (int segment = 0; segment < _Positions.Length; segment++)
            {
                for (int side = 0; side < _Sides; side++)
                {
                    var vertIndex = (segment * _Sides + side);
                    var u = side / (_Sides - 1f);
                    var v = segment / (_Positions.Length - 1f);

                    uvs[vertIndex] = new Vector2(u, v);
                }
            }

            return uvs;
        }

        private int[] GenerateIndices()
        {
            // Two triangles and 3 vertices
            var indices = new int[_Positions.Length * _Sides * 2 * 3];

            var currentIndicesIndex = 0;
            for (int segment = 1; segment < _Positions.Length; segment++)
            {
                for (int side = 0; side < _Sides; side++)
                {
                    var vertIndex = (segment * _Sides + side);
                    var prevVertIndex = vertIndex - _Sides;

                    // Triangle one
                    indices[currentIndicesIndex++] = prevVertIndex;
                    indices[currentIndicesIndex++] = (side == _Sides - 1) ? (vertIndex - (_Sides - 1)) : (vertIndex + 1);
                    indices[currentIndicesIndex++] = vertIndex;


                    // Triangle two
                    indices[currentIndicesIndex++] = (side == _Sides - 1) ? (prevVertIndex - (_Sides - 1)) : (prevVertIndex + 1);
                    indices[currentIndicesIndex++] = (side == _Sides - 1) ? (vertIndex - (_Sides - 1)) : (vertIndex + 1);
                    indices[currentIndicesIndex++] = prevVertIndex;
                }
            }

            return indices;
        }

        private Vector3[] CalculateCircle(int index)
        {
            var dirCount = 0;
            var forward = Vector3.zero;

            // If not first index
            if (index > 0)
            {
                forward += (_Positions[index] - _Positions[index - 1]).normalized;
                dirCount++;
            }

            // If not last index
            if (index < _Positions.Length - 1)
            {
                forward += (_Positions[index + 1] - _Positions[index]).normalized;
                dirCount++;
            }

            // Forward is the average of the connecting edges directions
            forward = (forward / dirCount).normalized;
            var side = Vector3.Cross(forward, forward + new Vector3(.123564f, .34675f, .756892f)).normalized;
            var up = Vector3.Cross(forward, side).normalized;

            var circle = new Vector3[_Sides];
            var angle = 0f;
            var angleStep = (2 * Mathf.PI) / _Sides;

            var t = index / (_Positions.Length - 1f);
            var radius = _Radius;

            for (int i = 0; i < _Sides; i++)
            {
                var x = Mathf.Cos(angle);
                var y = Mathf.Sin(angle);

                circle[i] = _Positions[index] + side * x * radius + up * y * radius;

                angle += angleStep;
            }

            return circle;
        }
    }
}
