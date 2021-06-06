using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
