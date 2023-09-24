using System;
using Godot;

public partial class Pulsator : Node3D
{
    private const float DEFUALT_PULSE_REPEAT_TIME = 6.0f;

    private float pulseRepeatTime = DEFUALT_PULSE_REPEAT_TIME;

    private Timer pulseTimer;

    [Export]
    public float PulseVelocity { get; set; } = 3.0f;
    [Export]
    public float PulseRange { get; set; } = 4.0f;
    [Export]
    public float PulseMaxLifetime { get; set; } = DEFUALT_PULSE_REPEAT_TIME;
    [Export]
    public bool SmartPulseMaxLifetime { get; set; } = true;
    [Export]
    public float PulseRepeatTime
    {
        get
        {
            return pulseRepeatTime;
        }
        set
        {
            pulseRepeatTime = value;
            pulseTimer.WaitTime = value;
        }
    }
    [Export]
    public bool IsEnabled { get; set; } = true;
    [Export]
    public float MaxDistanceToPlayer { get; set; } = 50.0f;
    [Export]
    public bool IgnorePlayerDistanceCheck { get; set; } = false;


    public override void _Ready()
    {
        if (SmartPulseMaxLifetime)
        {
            PulseMaxLifetime = PulseRepeatTime + (PulseRange / PulseVelocity);
        }

        pulseTimer = new()
        {
            Autostart = true,
            WaitTime = pulseRepeatTime
        };
        pulseTimer.Timeout += OnTimerTimeout;
        AddChild(pulseTimer);
    }

    private void OnTimerTimeout()
    {
        if (!IsEnabled)
        {
            return;
        }

        Vector3 position = GlobalPosition;

        if (!IgnorePlayerDistanceCheck)
        {
            float distanceToPlayer = position.DistanceTo(GlobalData.LastKnownPlayerPosition);
            if (distanceToPlayer > MaxDistanceToPlayer)
            {
                return;
            }
        }

        ShaderControllerAutoload.Pulse(position, PulseVelocity, PulseRange, PulseMaxLifetime, PulseType.NORMAL);
    }
}