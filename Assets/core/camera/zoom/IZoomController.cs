namespace Starfire.Core.Cam.Zoom
{
    public interface IZoomController
    {
        float CurrentZoom { get; }
        float TargetZoom { get; }
        float UserZoomLevel { get; }
        float SpeedZoomDelta { get; }

        void ScrollZoom(float delta);
        void SetUserZoom(float zoom, float transitionTime = 0.5f);
        void ResetUserZoom(float transitionTime = 0.5f);
        void Update(float speed, float recommendedZoom, float deltaTime);
        void SetPreset(Config.CameraPresetInstance preset);
    }
}
