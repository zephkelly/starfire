namespace StarfireV2
{
    public enum RotationMode
    {
        Instant,       // Direct angle assignment
        Smooth,        // Lerp/SmoothDamp interpolation
        Physics,       // PID-based torque controller (center of mass)
        ThrusterBased  // Individual thrusters with forces at positions
    }
}
