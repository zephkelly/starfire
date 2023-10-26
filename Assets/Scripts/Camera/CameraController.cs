using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Starfire.Controllers
{
  public class CameraController : MonoBehaviour
  {
    public static CameraController Instance;
    // private InputManager inputs;
    private Camera mainCamera;
    private Transform cameraTransform;

    private Transform target;
    private Rigidbody2D targetRigidbody;
    private float targetVelocityMagnitude;

    private Vector3 mouseLerpPosition;
    private Vector3 currentOffsetAmount;

    [SerializeField] float mouseInterpolateDistance = 2f;
    [SerializeField] float cameraPanSpeed = 0.125f;

    [SerializeField] private float maxZoom = 70f;
    [SerializeField] private float minZoom = 30f;
    private float currentZoom;

    //Parallaxing-layers----------------------------------------------------------------------------

    // [SerializeField] ParallaxStarfield[] starfieldsLayers;
    // [SerializeField] ParallaxGasCloud[] gasCloudLayers;
    // [SerializeField] DepoParallax depoParallax;

    //----------------------------------------------------------------------------------------------

    private void Awake()
    {
      mainCamera = Camera.main;
      cameraTransform = mainCamera.transform;

      if (Instance == null)
      {
        Instance = this;
      } else {
        Destroy(gameObject);
      }
    }

    private void Start()
    {
      target = GameObject.Find("Player").transform;
      targetRigidbody = target.GetComponent<Rigidbody2D>();

      minZoom = mainCamera.orthographicSize;
      currentZoom = minZoom;
    }

    private void LateUpdate()
    {
      if (target == null) return;

      mouseLerpPosition = (mainCamera.ScreenToWorldPoint(Input.mousePosition) - target.position).normalized;
      mouseLerpPosition.y = mouseLerpPosition.y * 1.4f;   //beacuse the camera is wider than it is tall

      targetVelocityMagnitude = targetRigidbody.velocity.magnitude;

      //if the player scrolls out, zoom out
      if (Input.mouseScrollDelta != Vector2.zero)
      {
        currentZoom += -Input.mouseScrollDelta.y;
        currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, currentZoom + (targetVelocityMagnitude * 0.2f), 0.1f);
        return;
      }

      //If the player moves quicker, zoom out further
      if (targetVelocityMagnitude > (targetVelocityMagnitude * 0.85f)) {
        mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, currentZoom + (targetVelocityMagnitude * 0.2f), 0.1f);
      }
    }

    private void FixedUpdate()
    {
      if (target == null) return;

      Vector3 targetVector = target.position + currentOffsetAmount + (mouseLerpPosition * mouseInterpolateDistance * (targetVelocityMagnitude * 0.06f));     

      targetVector.z = cameraTransform.position.z;

      Vector3 cameraLastPosition = cameraTransform.position;
      cameraTransform.position = Vector3.Lerp(cameraTransform.position, targetVector, cameraPanSpeed * (Mathf.Clamp(targetVelocityMagnitude, 25, 50) * 0.03f));
      
      // UpdateParllaxing(cameraLastPosition);
    }

    // private void UpdateParllaxing(Vector2 cameraLastPosition)
    // {
    //   //Starfields
    //   for (int i = 0; i < starfieldsLayers.Length; i++)
    //   {
    //     starfieldsLayers[i].Parallax(cameraLastPosition);
    //   }

    //   //Gas clouds
    //   foreach(ParallaxGasCloud gasCloud in gasCloudLayers)
    //   {
    //     gasCloud.Parallax(cameraLastPosition);
    //   }

    //   //Home depo
    //   // depoParallax.Parallax(cameraLastPosition);
    // }

    public void ChangeFocus(Transform newFocus) => target = newFocus;

    public void SetOffset(Vector2 offsetVector) => currentOffsetAmount = offsetVector;

    public void ClearOffset() => currentOffsetAmount = Vector3.zero;
  }
}