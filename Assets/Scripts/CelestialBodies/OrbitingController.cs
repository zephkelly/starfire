using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
  public class OrbitingController : MonoBehaviour
  {
    private CelestialBehaviour celestialBehaviour;
    private Rigidbody2D celestialRigidbody;

    private const float G = 0.2f;
    private List<Rigidbody2D> orbitingBodies = new List<Rigidbody2D>();

    public Vector2 GetVelocity() => celestialRigidbody.velocity;

    private void Awake()
    {
        celestialBehaviour = gameObject.GetComponent<CelestialBehaviour>();
        celestialRigidbody = gameObject.GetComponent<Rigidbody2D>();
    }

    private void FixedUpdate()
    {
        Gravity();
    }

    public void SetParentOrbitingObject(CelestialBehaviour _parentCelestialBehaviour)
    {
        celestialBehaviour.SetOrbitingBody(_parentCelestialBehaviour);
        Rigidbody2D _parentRigidbody = _parentCelestialBehaviour.GetComponent<Rigidbody2D>();

        orbitingBodies.Add(_parentRigidbody);
        ApplyInstantOrbitalVelocity(_parentRigidbody);
    }

    public void SetChildOrbitingObject(CelestialBehaviour _newOrbitingBody)
    {
        _newOrbitingBody.SetOrbitingBody(celestialBehaviour);
        Rigidbody2D _newOrbitingRigidbody = _newOrbitingBody.GetComponent<Rigidbody2D>();

        orbitingBodies.Add(_newOrbitingRigidbody);
        ApplyInstantOrbitalVelocity(_newOrbitingRigidbody);
    }

    public void SetChildOrbitingObject(Rigidbody2D _newOrbitingBody)
    {
        _newOrbitingBody.GetComponent<CelestialBehaviour>().SetOrbitingBody(celestialBehaviour);

        if (!orbitingBodies.Contains(_newOrbitingBody))
        {
            orbitingBodies.Add(_newOrbitingBody);
        }

        ApplyInstantOrbitalVelocity(_newOrbitingBody);
    }

    public void ApplyInstantOrbitalVelocity(Rigidbody2D _body)
    {
        float starMass = GetBodyMass(celestialRigidbody, celestialBehaviour.CelestialBodyType);
        float distanceToStar = Vector2.Distance(celestialRigidbody.position, _body.position);

        Vector2 directionToStar = (celestialRigidbody.position - _body.position).normalized;
        Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);
        Vector2 appliedOrbitalVelocity = perpendicularDirection * Mathf.Sqrt((G * starMass) / distanceToStar);
        Vector2 deltaVelocity = appliedOrbitalVelocity - _body.velocity;

        _body.velocity += deltaVelocity;
    }

    public Vector2 GetOrbitalVelocity(Rigidbody2D _body)
    {
        float parentBodyMass = GetBodyMass(celestialRigidbody, celestialBehaviour.CelestialBodyType);
        float distanceToStar = Vector2.Distance(celestialRigidbody.position, _body.position);

        Vector2 directionToStar = (celestialRigidbody.position - _body.position).normalized;
        Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);
        Vector2 orbitalVelocity = perpendicularDirection * Mathf.Sqrt((G * parentBodyMass) / distanceToStar);

        return orbitalVelocity;
    }

    public int GetOrbitDirection(Rigidbody2D _body)
    {
        Vector2 directionToStar = (celestialRigidbody.position - _body.position).normalized;
        Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);

        return Vector2.Dot(perpendicularDirection, _body.velocity) > 0 ? 1 : -1;
    }

    public float GetThermalGradient(float _objectDistance)
    {
        var distanceNormalized = _objectDistance / celestialBehaviour.InfluenceRadius;
        var trueDistance = 1 - distanceNormalized;

        return Mathf.Lerp(0, celestialBehaviour.Temperature, trueDistance);
    }

    private void Gravity()
    {
        for (int i = 0; i < orbitingBodies.Count; i++)
        {
            Rigidbody2D body = orbitingBodies[i];

            if (body == null) 
            {
                orbitingBodies.RemoveAt(i);
                continue;
            }

            float bodyMass = GetBodyMass(body);
            float starMass = GetBodyMass(celestialRigidbody, celestialBehaviour.CelestialBodyType);
            float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);

            float gravitationalForce = (G * bodyMass * starMass) / (distanceToStar * distanceToStar);
            Vector2 direction = (celestialRigidbody.position - body.position).normalized;

            body.AddForce(direction * gravitationalForce);
        }
    }

    public float GetBodyMass(Rigidbody2D _body, CelestialBodyType _bodyType)
    {
        float bodyMass = _body.mass;

        if (_bodyType is not CelestialBodyType.Star) return bodyMass;
        return bodyMass *= 10;
    }

    public float GetBodyMass(Rigidbody2D _body)
    {
        return _body.mass;
    }

    private void OnTriggerEnter2D(Collider2D _otherCollider) 
    {
        if (_otherCollider.TryGetComponent<ShipController>(out ShipController shipController))
        {
            shipController.SetOrbitingBody(celestialBehaviour);
        }
        else if (_otherCollider.CompareTag("Planet"))
        {
            if (celestialBehaviour.CelestialBodyType is CelestialBodyType.Planet) return;
            Rigidbody2D planetRigidbody = _otherCollider.gameObject.GetComponent<Rigidbody2D>();
            SetChildOrbitingObject(planetRigidbody);
        }
        else if (_otherCollider.CompareTag("Pickup"))
        {
            _otherCollider.TryGetComponent(out Rigidbody2D pickupRigidbody);
            orbitingBodies.Add(pickupRigidbody);
        }
    }

    private void OnTriggerExit2D(Collider2D _otherCollider) 
    {
        if (_otherCollider.TryGetComponent<ShipController>(out ShipController shipController))
        {
            if (celestialBehaviour.ParentOrbitingBody == null) 
            {
                orbitingBodies.Remove(_otherCollider.gameObject.GetComponent<Rigidbody2D>());
                shipController.RemoveOrbitingBody(); 
                return;
            }

            if (shipController.OrbitingBody == null)
            {
                shipController.SetOrbitingBody(celestialBehaviour.ParentOrbitingBody, returningToParent: true);
                return;
            }

            if (shipController.OrbitingBody.Mass > celestialBehaviour.Mass && shipController.OrbitingBody.CelestialBodyType is CelestialBodyType.Planet) return;
            shipController.SetOrbitingBody(celestialBehaviour.ParentOrbitingBody, returningToParent: true);
        }
        else if (_otherCollider.CompareTag("Planet"))
        {
            if (celestialBehaviour.CelestialBodyType is CelestialBodyType.Planet) return;
            Rigidbody2D planetRigidbody = _otherCollider.gameObject.GetComponent<Rigidbody2D>();
            orbitingBodies.Remove(planetRigidbody);
        }
        else if (_otherCollider.CompareTag("Pickup"))
        {
            Rigidbody2D pickupRigidbody = _otherCollider.gameObject.GetComponent<Rigidbody2D>();
            orbitingBodies.Remove(pickupRigidbody);
        }
    }

    private void OnTriggerStay2D(Collider2D _otherCollider) 
    {
        if (_otherCollider.TryGetComponent<ShipController>(out ShipController shipController))
        {
            shipController.SetOrbitingBody(celestialBehaviour);
        }
    }

    private void OnDestroy() 
    {
        foreach (Rigidbody2D body in orbitingBodies)
        {
            if (body == null) continue;
            body.GetComponent<CelestialBehaviour>().RemoveOrbitingBody();
        }
    }

    private void OnDisable() 
    {
        foreach (Rigidbody2D body in orbitingBodies)
        {
            if (body == null) continue;
            // body.GetComponent<CelestialBehaviour>().RemoveOrbitingBody();
            if (body.TryGetComponent(out CelestialBehaviour celestialBehaviour))
            {
                celestialBehaviour.RemoveOrbitingBody();
            }
        }
    }
}
}