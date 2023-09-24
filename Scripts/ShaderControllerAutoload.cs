using System.Collections.Generic;
using Godot;

public partial class ShaderControllerAutoload : Node
{
    private const int SONAR_MAX_DATA_COUNT = 16;
    private const float PULSE_LIFETIME = 1.0f;
    private const string SONAR_PATH = "res://Resources/Materials/sonar.tres";
    private const string SONAR_PARAM_POSITIONS = "sonar_positions";
    private const string SONAR_PARAM_TIMESTAMPS = "sonar_timestamps";

    private static ShaderControllerAutoload Instance { get; set; }

    private readonly LinkedList<PulseData> pulseDataList = new();
    private readonly Vector3[] positionArray = new Vector3[SONAR_MAX_DATA_COUNT];
    private readonly float[] timestampArray = new float[SONAR_MAX_DATA_COUNT];

    private ShaderMaterial SonarMaterial { get; set; }

    private float timeCounterSeconds;
    private bool isSonarUpdateQueued;

    public override void _Ready()
    {
        Instance = this;
        SonarMaterial = GD.Load<ShaderMaterial>(SONAR_PATH);

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

    public static void Pulse(Vector3 from)
    {
        Instance.InternalPulse(from);
    }

    public static void EraseSonarPulseData()
    {
        Instance.InternalEraseSonarPulseData();
    }

    private void InternalPulse(Vector3 from)
    {
        var pulseData = new PulseData()
        {
            Position = from,
            Timestamp = Time.GetTicksMsec() / 1000.0f,
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
            if (IsOutdated(node.Value.Timestamp))
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

    private bool IsOutdated(float timestamp)
    {
        float timePassed = timeCounterSeconds - timestamp;
        return timePassed > PULSE_LIFETIME;
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
        }

        SonarMaterial.SetShaderParameter(SONAR_PARAM_POSITIONS, positionArray);
        SonarMaterial.SetShaderParameter(SONAR_PARAM_TIMESTAMPS, timestampArray);
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
    }
}