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
        float starMass = GetBodyMass(celestialRigidbody, celestialBehaviour);
        float distanceToStar = Vector2.Distance(celestialRigidbody.position, _body.position);

        Vector2 directionToStar = (celestialRigidbody.position - _body.position).normalized;
        Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);
        Vector2 appliedOrbitalVelocity = perpendicularDirection * Mathf.Sqrt((G * starMass) / distanceToStar);
        Vector2 deltaVelocity = appliedOrbitalVelocity - _body.velocity;

        _body.velocity += deltaVelocity;
    }

    public Vector2 GetOrbitalVelocity(Rigidbody2D _body)
    {
        float parentBodyMass = GetBodyMass(celestialRigidbody, celestialBehaviour);
        float distanceToStar = Vector2.Distance(celestialRigidbody.position, _body.position);

        Vector2 directionToStar = (celestialRigidbody.position - _body.position).normalized;
        Vector2 perpendicularDirection = Vector2.Perpendicular(directionToStar);
        Vector2 orbitalVelocity = perpendicularDirection * Mathf.Sqrt((G * parentBodyMass) / distanceToStar);

        return orbitalVelocity + celestialRigidbody.velocity;
    }

    public float GetThermalGradient(float _objectDistance)
    {
        var distanceNormalized = _objectDistance / celestialBehaviour.Radius;
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
            float starMass = GetBodyMass(celestialRigidbody, celestialBehaviour);
            float distanceToStar = Vector2.Distance(celestialRigidbody.position, body.position);

            float gravitationalForce = (G * bodyMass * starMass) / (distanceToStar * distanceToStar);
            Vector2 direction = (celestialRigidbody.position - body.position).normalized;

            body.AddForce(direction * gravitationalForce);
        }
    }

    private float GetBodyMass(Rigidbody2D _body, CelestialBehaviour _celestialBehaviour = null)
    {
        float bodyMass = _body.mass;

        if (_celestialBehaviour == null) return bodyMass;
        if (_celestialBehaviour.CelestialBodyType != CelestialBodyType.Star) return bodyMass;

        return bodyMass *= 10;
    }

    private void OnTriggerEnter2D(Collider2D _otherCollider) 
    {
      if (_otherCollider.CompareTag("Player"))
      {
        _otherCollider.gameObject.GetComponent<ShipController>().SetOrbitingBody(celestialBehaviour);
      }
      else if (_otherCollider.CompareTag("Planet"))
      {
        if (celestialBehaviour.CelestialBodyType == CelestialBodyType.Planet) return;

        Rigidbody2D planetRigidbody = _otherCollider.gameObject.GetComponent<Rigidbody2D>();
        SetChildOrbitingObject(planetRigidbody);
      }
    }

    private void OnTriggerExit2D(Collider2D _otherCollider) 
    {
        if (_otherCollider.CompareTag("Player"))
        {
            ShipController playerController = _otherCollider.gameObject.GetComponent<ShipController>();

            if (celestialBehaviour.ParentOrbitingBody == null) 
            {
                orbitingBodies.Remove(_otherCollider.gameObject.GetComponent<Rigidbody2D>());
                playerController.RemoveOrbitingBody(); 
                return;
            }

            if (playerController.OrbitingBody == null)
            {
                playerController.SetOrbitingBody(celestialBehaviour.ParentOrbitingBody, isParent: true);
                return;
            }

            if (playerController.OrbitingBody.Mass > celestialBehaviour.Mass && playerController.OrbitingBody.CelestialBodyType is CelestialBodyType.Planet) return;
            playerController.SetOrbitingBody(celestialBehaviour.ParentOrbitingBody, isParent: true);
        }
        else if (_otherCollider.CompareTag("Planet"))
        {
            Rigidbody2D planetRigidbody = _otherCollider.gameObject.GetComponent<Rigidbody2D>();
            orbitingBodies.Remove(planetRigidbody);
        }
    }
}
}