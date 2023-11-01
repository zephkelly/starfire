using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Cam
{
  public class StarfieldLayer : MonoBehaviour
  {
    private ParticleSystem startfieldParticleSystem;
    private ParticleSystem.Particle[] stars;
    private Transform cameraTransform;

    [SerializeField] Transform starfieldTransform;
    [SerializeField] int starsMax = 100;
    [SerializeField] float starSizeMin = 0.15f;
    [SerializeField] float starSizeMax = 0.4f;
    [SerializeField] float starSpawnRadius = 100;
    [SerializeField] float parallaxFactor = 0.9f;

    private float starDistanceSqr;
    [SerializeField] private static int particleZ = 2;
    
    //----------------------------------------------------------------------------------------------

    private void Awake()
    {
      cameraTransform = Camera.main.transform;
      starDistanceSqr = starSpawnRadius * starSpawnRadius;
      startfieldParticleSystem = GetComponent<ParticleSystem>();
      starfieldTransform = GetComponent<Transform>();
    }

    private void Start () 
    {
      CreateStars();
    }

    private Vector3 lastStarPosition;

    private void CreateStars()
    {
      stars = new ParticleSystem.Particle[starsMax];

      for (int i = 0; i < starsMax; i++)
      {
        stars[i].position = (lastStarPosition + ((Vector3)Random.insideUnitCircle * starSpawnRadius) + cameraTransform.position) / 2;
        lastStarPosition = stars[i].position;
        stars[i].position = new Vector3(stars[i].position.x, stars[i].position.y, particleZ);
        stars[i].startColor = new Color(1,1,1, 1);
        stars[i].startSize = Random.Range(starSizeMin, starSizeMax);
      }

      startfieldParticleSystem.SetParticles(stars, stars.Length);
    }
 
    private void Update() 
    {
      Vector3 cameraParallaxDelta = (Vector2)(cameraTransform.position - starfieldTransform.position);

      for (int i = 0; i < starsMax; i++)
      {
        Vector2 starPosition = (Vector2)(stars[i].position + starfieldTransform.position);

        if((starPosition - (Vector2)cameraTransform.position).sqrMagnitude > starDistanceSqr) 
        {
          stars[i].position = (Vector3)(Random.insideUnitCircle.normalized * starSpawnRadius) + cameraParallaxDelta;
        }
      }
    }

    public void Parallax(Vector3 cameraLastPosition)   //called on camera controller
    {
      Vector3 cameraDelta = (Vector2)(cameraTransform.position - cameraLastPosition); 

      starfieldTransform.position = Vector3.Lerp(
        starfieldTransform.position, 
        starfieldTransform.position - cameraDelta, 
        parallaxFactor * Time.deltaTime
      );

      startfieldParticleSystem.SetParticles(stars, stars.Length);
    }
  }
}