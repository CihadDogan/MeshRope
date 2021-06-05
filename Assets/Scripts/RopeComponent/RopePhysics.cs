using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RopePhysics : MonoBehaviour
{
    // Components
    [SerializeField] private Transform _StartPoint;
    [SerializeField] private Transform _EndPoint;
    [SerializeField] private CableParticle[] _Points;
    public RopeRenderer MRopeRenderer;

    // Variables
    [SerializeField] private float _CableLength = 0.5f;
    [SerializeField] private int _TotalSegments = 5;
    private int _Segments = 0;
    [SerializeField] private int _VerletIterations = 1;
    [SerializeField] private int _SolverIterations = 1;

    void Start()
    {
        InitializeCablePhsyic();
    }

    void InitializeCablePhsyic()
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

    void RenderCable()
    {
        Vector3[] positions = new Vector3[_Segments + 1];

        for (int pointIdx = 0; pointIdx < _Segments + 1; pointIdx++)
            positions[pointIdx] = _Points[pointIdx].Position;

        MRopeRenderer.SetPositions(positions);
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
}