using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Starfire;

namespace zephkelly
{
  public enum PointerType
  {
    Star,
    Planet,
    Moon,
    Asteroid,
    Station,
    Player
  }

  public class Pointer : MonoBehaviour
  {
    [SerializeField] TextMeshProUGUI _text;
    [SerializeField] Image _pointerImage;
    [SerializeField] Transform _pointerImageContainer;
    private PointerType pointerType;
    private GameObject _pointerObject;

    private Vector2 screenPosition;
    private Vector2 lastScreenPosition;
    private Vector2 moveDirection;

    public Vector2 PointerWorldPosition { get; private set;}
    public Vector2 ScreenPosition { get => screenPosition; }
    public Vector2 LastScreenPosition { get => lastScreenPosition; }
    public Vector2 MoveDirection { get => moveDirection; }

    public float PointerDistance { get; private set;}
    public PointerType PointerType { get => pointerType; }
    public Image PointerImage { get => _pointerImage; }
    public GameObject PointerObject { get => _pointerObject; }
    public Transform PointerImageContainer { get => _pointerImageContainer; }
    public TextMeshProUGUI Distance { get => _text; }

    private void Start()
    {
      _pointerObject = this.gameObject;
    }

    public void UpdateTargetInfo(float distance, Vector2 position)
    {
      PointerWorldPosition = position;
      PointerDistance = distance;
      distance = (int)distance / 3;
      _text.text = $"{distance.ToString()}Km";
    }

    public void UpdateScreenPosition(Vector2 cappedPosition)
    {
      lastScreenPosition = screenPosition;
      screenPosition = cappedPosition;
    }

    public void UpdateMoveDirection()
    {
      moveDirection = (screenPosition - lastScreenPosition).normalized;
    }

    public void ResetMoveDirection()
    {
      moveDirection = Vector2.zero;
    }

    public void SetupStarPointer(CelestialBodyType bodyType)
    {
      pointerType = PointerType.Star;

      switch (bodyType)
      {
        default:
          _pointerImage.color = Color.white;
          break;
      }
    }

    public void SetupDepoPointer()
    {
      pointerType = PointerType.Station;
      _pointerImage.color = new Color(0.25f, 1f, 1f, 0.7f);
    }
  }
}
