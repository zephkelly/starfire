namespace StarfireV2
{
    /// <summary>
    /// Sets cruise speed and acceleration limits on the blackboard.
    /// Steering actions will read these values to limit their max speed/acceleration.
    /// Use -1 to indicate "use ship's maximum" (no limit).
    /// </summary>
    public class SetCruiseSpeedAction : BTAction
    {
        private readonly float _speed;
        private readonly float _acceleration;
        private readonly string _speedKey;
        private readonly string _accelKey;

        public SetCruiseSpeedAction(
            float speed = -1f,
            float acceleration = -1f,
            string speedKey = "cruise_speed",
            string accelKey = "cruise_acceleration")
        {
            _speed = speed;
            _acceleration = acceleration;
            _speedKey = speedKey;
            _accelKey = accelKey;
        }

        protected override BTNodeStatus OnExecute(float deltaTime)
        {
            Context.Set(_speedKey, _speed);
            Context.Set(_accelKey, _acceleration);

            return BTNodeStatus.Success;
        }
    }
}
