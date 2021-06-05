using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshFilter))]
public class MeshRope : MonoBehaviour
{
    [Header("Phsyics")]
    [SerializeField] private Transform _StartPoint;
    [SerializeField] private Transform _EndPoint;
    [SerializeField] private float _CableLength = 2f;
    [SerializeField] private int _TotalSegments = 16;
    [SerializeField] private int _VerletIterations = 1;
    [SerializeField] private int _SolverIterations = 1;
    private CableParticle[] _Points;
    private int _Segments = 0;
    private Vector3[] _Positions;

    [Header("Renderer")]
    [SerializeField] int _Sides = 6;
    [SerializeField] float _Radius = 0.1f;
    private Vector3[] _Vertices;
    private Mesh _Mesh;
    private MeshFilter _MeshFilter;
    private MeshRenderer _MeshRenderer;
    private GameObject _SphereStart;
    private GameObject _SphereEnd;

    #region MONOBEHAVIOUR

    void Start()
    {
        InitializePhsyic();
        InitializeRenderer();
    }

    void Update()
    {
        RenderCable();
    }

    void FixedUpdate()
    {
        for (int verletIdx = 0; verletIdx < _VerletIterations; verletIdx++)
        {
            VerletIntegrate();
            SolveConstraints();
        }
    }

    #endregion

    #region PHYSICS

    void InitializePhsyic()
    {
        // Calculate segments to use
        _Segments = _TotalSegments;

        Vector3 cableDirection = (_EndPoint.position - transform.position).normalized;
        float initialSegmentLength = _CableLength / _Segments;
        _Points = new CableParticle[_Segments + 1];

        // Foreach point
        for (int pointIdx = 0; pointIdx <= _Segments; pointIdx++)
        {
            // Initial position
            Vector3 initialPosition = transform.position + (cableDirection * (initialSegmentLength * pointIdx));
            _Points[pointIdx] = new CableParticle(initialPosition);
        }

        // Bind start and end particles with their respective gameobjects
        CableParticle start = _Points[0];
        CableParticle end = _Points[_Segments];
        start.Bind(_StartPoint.transform);
        end.Bind(_EndPoint.transform);
    }

    void VerletIntegrate()
    {
        Vector3 gravityDisplacement = Time.fixedDeltaTime * Time.fixedDeltaTime * Physics.gravity;
        foreach (CableParticle particle in _Points)
        {
            particle.UpdateVerlet(gravityDisplacement);
        }
    }

    void SolveConstraints()
    {
        for (int iterationIdx = 0; iterationIdx < _SolverIterations; iterationIdx++)
        {
            SolveDistanceConstraint();
        }
    }

    void SolveDistanceConstraint()
    {
        float segmentLength = _CableLength / _Segments;
        for (int SegIdx = 0; SegIdx < _Segments; SegIdx++)
        {
            CableParticle particleA = _Points[SegIdx];
            CableParticle particleB = _Points[SegIdx + 1];

            // Solve for this pair of particles
            // Find current vector between particles
            Vector3 delta = particleB.Position - particleA.Position;
            // 
            float currentDistance = delta.magnitude;
            float errorFactor = (currentDistance - segmentLength) / currentDistance;

            // Only move free particles to satisfy constraints
            if (particleA.IsFree() && particleB.IsFree())
            {
                particleA.Position += errorFactor * 0.5f * delta;
                particleB.Position -= errorFactor * 0.5f * delta;
            }
            else if (particleA.IsFree())
            {
                particleA.Position += errorFactor * delta;
            }
            else if (particleB.IsFree())
            {
                particleB.Position -= errorFactor * delta;
            }
        }
    }

    #endregion

    #region RENDERER

    void InitializeRenderer()
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

    void RenderCable()
    {
        // Get cable positions to create tube
        Vector3[] positions = new Vector3[_Segments + 1];
        for (int pointIdx = 0; pointIdx < _Segments + 1; pointIdx++)
            positions[pointIdx] = _Points[pointIdx].Position;
        _Positions = positions;

        // Generate
        GenerateMesh();
    }

    void GenerateMesh()
    {
        if (_Mesh == null || _Positions == null || _Positions.Length <= 1)
        {
            _Mesh = new Mesh();
            return;
        }

        // Start
        _SphereStart.transform.SetParent(transform);
        _SphereStart.transform.localScale = Vector3.one * _Radius * 1.75f;
        _SphereStart.transform.position = _Positions[0];
        _SphereStart.GetComponent<MeshRenderer>().material = _MeshRenderer.material;

        // End
        _SphereEnd.transform.SetParent(transform);
        _SphereEnd.transform.localScale = Vector3.one * _Radius * 1.75f;
        _SphereEnd.transform.position = _Positions[_Positions.Length - 1];
        _SphereEnd.GetComponent<MeshRenderer>().material = _MeshRenderer.material;

        // Tube
        var verticesLength = _Sides * _Positions.Length;
        if (_Vertices == null || _Vertices.Length != verticesLength)
        {
            _Vertices = new Vector3[verticesLength];

            var indices = GenerateIndices();
            var uvs = GenerateUVs();

            if (verticesLength > _Mesh.vertexCount)
            {
                _Mesh.vertices = _Vertices;
                _Mesh.triangles = indices;
                _Mesh.uv = uvs;
            }
            else
            {
                _Mesh.triangles = indices;
                _Mesh.vertices = _Vertices;
                _Mesh.uv = uvs;
            }
        }

        var currentVertIndex = 0;

        for (int i = 0; i < _Positions.Length; i++)
        {
            var circle = CalculateCircle(i);
            foreach (var vertex in circle)
            {
                _Vertices[currentVertIndex++] = transform.InverseTransformPoint(vertex);
            }
        }

        _Mesh.vertices = _Vertices;
        _Mesh.RecalculateNormals();
        _Mesh.RecalculateBounds();

        _MeshFilter.mesh = _Mesh;
    }

    Vector2[] GenerateUVs()
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

    int[] GenerateIndices()
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

    Vector3[] CalculateCircle(int index)
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

    #endregion
}

[System.Serializable]
public class CableParticle
{
    // Components
    public Transform _BoundTo = null;
    public Rigidbody _BoundRigid = null;

    // Variables
    public Vector3 _Position, _OldPosition;

    public Vector3 Position
    {
        get { return _Position; }
        set { _Position = value; }
    }

    public Vector3 Velocity
    {
        get { return (_Position - _OldPosition); }
    }

    public CableParticle(Vector3 newPosition)
    {
        _OldPosition = _Position = newPosition;
    }

    public void UpdateVerlet(Vector3 gravityDisplacement)
    {
        if (IsFree())
        {
            Vector3 newPosition = this.Position + this.Velocity + gravityDisplacement;
            this.UpdatePosition(newPosition);
        }
        else
        {
            if (_BoundRigid == null)
            {
                this.UpdatePosition(_BoundTo.position);
            }
            else
            {
                switch (_BoundRigid.interpolation)
                {
                    case RigidbodyInterpolation.Interpolate:
                        this.UpdatePosition(_BoundRigid.position + (_BoundRigid.velocity * Time.fixedDeltaTime) / 2);
                        break;
                    case RigidbodyInterpolation.None:
                    default:
                        this.UpdatePosition(_BoundRigid.position + _BoundRigid.velocity * Time.fixedDeltaTime);
                        break;
                }
            }
        }
    }

    public void UpdatePosition(Vector3 newPos)
    {
        _OldPosition = _Position;
        _Position = newPos;
    }

    public void Bind(Transform to)
    {
        _BoundTo = to;
        _BoundRigid = to.GetComponent<Rigidbody>();
        _OldPosition = _Position = _BoundTo.position;
    }

    public bool IsFree()
    {
        return (_BoundTo == null);
    }
}
