using System.Collections.Generic;
using Godot;

public partial class ShaderControllerAutoload : Node
{
    private const int SONAR_MAX_DATA_COUNT = 32;
    private const string PULSE_PARAM_POSITIONS = "pulse_positions";
    private const string PULSE_PARAM_TIMESTAMPS = "pulse_timestamps";
    private const string PULSE_PARAM_VELOCITY = "pulse_velocities";
    private const string PULSE_PARAM_MAX_RANGE = "pulse_max_ranges";
    private const string PULSE_PARAM_MAX_LIFETIME = "pulse_max_lifetimes";

    private static ShaderControllerAutoload Instance { get; set; }

    private List<string> SonarPaths { get; } = new() {
        "res://Resources/Materials/monster_edge_detection.tres",
        "res://Resources/Materials/environment_edge_detection.tres",
    };

    private List<ShaderMaterial> SonarMaterials { get; } = new();

    private readonly LinkedList<PulseData> pulseDataList = new();
    private readonly Vector3[] positionArray = new Vector3[SONAR_MAX_DATA_COUNT];
    private readonly float[] timestampArray = new float[SONAR_MAX_DATA_COUNT];
    private readonly float[] velocityArray = new float[SONAR_MAX_DATA_COUNT];
    private readonly float[] maxRangeArray = new float[SONAR_MAX_DATA_COUNT];
    private readonly float[] maxLifetimeArray = new float[SONAR_MAX_DATA_COUNT];

    private float timeCounterSeconds;
    private bool isSonarUpdateQueued;

    public override void _Ready()
    {
        Instance = this;
        foreach (var path in SonarPaths)
        {
            SonarMaterials.Add(ResourceLoader.Load<ShaderMaterial>(path));
        }

        var clearOutdatedPulseDataTimer = new Timer
        {
            WaitTime = 1.0f,
            Autostart = true,
        };
        clearOutdatedPulseDataTimer.Timeout += OnClearOutdatedPulseDataTimerTimeout;
        AddChild(clearOutdatedPulseDataTimer);

        QueueSonarUpdate();
    }

    public override void _Process(double delta)
    {
        timeCounterSeconds += (float)delta;

        if (isSonarUpdateQueued)
        {
            isSonarUpdateQueued = false;
            UpdateSonar();
        }
    }

    public static void Pulse(Vector3 from, float velocity, float maxRange, float maxLifetime)
    {
        Instance.InternalPulse(from, velocity, maxRange, maxLifetime);
    }

    public static void EraseSonarPulseData()
    {
        Instance.InternalEraseSonarPulseData();
    }

    private void InternalPulse(Vector3 from, float velocity, float maxRange, float maxLifetime)
    {
        var pulseData = new PulseData()
        {
            Position = from,
            Timestamp = Time.GetTicksMsec() / 1000.0f,
            Velocity = velocity,
            MaxRange = maxRange,
            MaxLifetime = maxLifetime,
        };

        pulseDataList.AddFirst(pulseData);
        QueueSonarUpdate();
    }

    private void ClearOutdatedPulseData()
    {
        var node = pulseDataList.First;
        while (node != null)
        {
            var nextNode = node.Next;
            if (IsOutdated(node.Value.Timestamp, node.Value.MaxLifetime))
            {
                GD.Print($"Removed node {node.Value}");
                pulseDataList.Remove(node);
            }

            node = nextNode;
        }

        QueueSonarUpdate();
    }

    private void InternalEraseSonarPulseData()
    {
        pulseDataList.Clear();
        QueueSonarUpdate();
    }

    private void QueueSonarUpdate()
    {
        isSonarUpdateQueued = true;
    }

    private bool IsOutdated(float timestamp, float maxLifetime)
    {
        float timePassed = timeCounterSeconds - timestamp;
        return timePassed > maxLifetime;
    }

    /// <summary>
    /// Updates the shader's internal data.
    /// </summary>
    private void UpdateSonar()
    {
        var node = pulseDataList.First;
        for (int i = 0; i < SONAR_MAX_DATA_COUNT; i++)
        {
            var pulseData = new PulseData();
            if (node != null)
            {
                pulseData = node.Value;
                node = node.Next;
            }

            positionArray[i] = pulseData.Position;
            timestampArray[i] = pulseData.Timestamp;
            velocityArray[i] = pulseData.Velocity;
            maxRangeArray[i] = pulseData.MaxRange;
            maxLifetimeArray[i] = pulseData.MaxLifetime;
        }

        foreach (var sonarMaterial in SonarMaterials)
        {
            sonarMaterial.SetShaderParameter(PULSE_PARAM_POSITIONS, positionArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_TIMESTAMPS, timestampArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_VELOCITY, velocityArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_MAX_RANGE, maxRangeArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_MAX_LIFETIME, maxLifetimeArray);
        }
    }

    private void OnClearOutdatedPulseDataTimerTimeout()
    {
        GD.Print($"Pulse data clearing outdated. time counter seconds: {timeCounterSeconds}");
        ClearOutdatedPulseData();
    }

    public readonly record struct PulseData
    {
        public Vector3 Position { get; init; }

        public float Timestamp { get; init; }
        public float Velocity { get; init; }
        public float MaxRange { get; init; }
        public float MaxLifetime { get; init; }
    }
}