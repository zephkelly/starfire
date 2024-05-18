using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire
{
  public class StarfieldLayer : ParallaxLayer
  {
    private ParticleSystem startfieldParticleSystem;
    private ParticleSystem.Particle[] stars;

    [SerializeField] int starsMax = 100;
    [SerializeField] float starSizeMin = 0.15f;
    [SerializeField] float starSizeMax = 0.4f;
    [SerializeField] float starSpawnRadius = 100;
    [SerializeField] float parallaxFactor;

    private float starDistanceSqr;
    [SerializeField] private static int particleZ = 2;
    
    //----------------------------------------------------------------------------------------------

    protected override void Awake()
    {
        base.Awake();

        starDistanceSqr = starSpawnRadius * starSpawnRadius;
        startfieldParticleSystem = GetComponent<ParticleSystem>();
    }

    private void Start () 
    {
      CreateStars();
    }

    private void CreateStars()
    {
      stars = new ParticleSystem.Particle[starsMax];

      for (int i = 0; i < starsMax; i++)
      {
        stars[i].position = ((Vector3)Random.insideUnitCircle * starSpawnRadius) + cameraTransform.position;
        stars[i].position = new Vector3(stars[i].position.x, stars[i].position.y, particleZ);
        stars[i].startColor = new Color(1,1,1, 1);

        float baseSize = Random.Range(starSizeMin, starSizeMax);
        float chance = Random.Range(0f, 1f);

        if (chance <= 0.05f)
        {
            baseSize = starSizeMax;
            float increase = Random.Range(0.30f, 0.5f);
            baseSize *= (1 + increase);
        }

        stars[i].startSize = baseSize;
      }

      startfieldParticleSystem.SetParticles(stars, stars.Length);
    }
 
    private Vector2 starRandomPosition;
    private Vector2 starLastPosition;

    private void Update() 
    {
      Vector2 cameraTransformPosition = new Vector2(cameraTransform.position.x, cameraTransform.position.y);

      Vector2 cameraParallaxDelta = cameraTransform.position - transform.position;

      for (int i = 0; i < starsMax; i++)
      {
        Vector2 starPosition = stars[i].position + transform.position;

        if((starPosition - cameraTransformPosition).sqrMagnitude > starDistanceSqr) 
        {
          starRandomPosition = Random.insideUnitCircle;
          stars[i].position =  ((starRandomPosition + starLastPosition) / 2).normalized * starSpawnRadius + cameraParallaxDelta;
          starLastPosition = starRandomPosition;
        }
      }
    }

    public override void Parallax(Transform obj, Vector3 cameraLastPosition, float j = 1f)   //called on camera controller
    {
        base.Parallax(this.transform, cameraLastPosition, parallaxFactor);

        startfieldParticleSystem.SetParticles(stars, stars.Length);
    }
  }
}