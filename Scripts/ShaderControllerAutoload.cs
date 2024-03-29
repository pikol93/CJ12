using System.Collections.Generic;
using Godot;

public enum PulseType
{
    NORMAL,
    ONLY_RING,
}

public enum ColorOverride
{
    NONE,
    WHITE,
    RED,
}

public partial class ShaderControllerAutoload : Node
{
    private const int SONAR_MAX_DATA_COUNT = 256;
    private const string PARAM_FORCE_EDGE_CHECK = "force_edge_check";
    private const string PULSE_PARAM_POSITIONS = "pulse_positions";
    private const string PULSE_PARAM_TIMESTAMPS = "pulse_timestamps";
    private const string PULSE_PARAM_VELOCITY = "pulse_velocities";
    private const string PULSE_PARAM_MAX_RANGE = "pulse_max_ranges";
    private const string PULSE_PARAM_MAX_LIFETIME = "pulse_max_lifetimes";
    private const string PULSE_PARAM_TYPE = "pulse_types";
    private const string PULSE_PARAM_COLOR_OVERRIDE = "pulse_color_overrides";
    private const string PATH_MONSTER_EDGE_DETECTION = "res://Resources/Materials/monster_edge_detection.tres";
    private const string PATH_ENVIRONMENT_EDGE_DETECTION = "res://Resources/Materials/environment_edge_detection.tres";

    private static ShaderControllerAutoload Instance { get; set; }

    private List<string> SonarPaths { get; } = new() {
        PATH_MONSTER_EDGE_DETECTION,
        PATH_ENVIRONMENT_EDGE_DETECTION,
    };

    private List<ShaderMaterial> SonarMaterials { get; } = new();

    private ShaderMaterial MonsterShaderMaterial { get; set; }

    private readonly LinkedList<PulseData> pulseDataList = new();
    private readonly Vector3[] positionArray = new Vector3[SONAR_MAX_DATA_COUNT];
    private readonly float[] timestampArray = new float[SONAR_MAX_DATA_COUNT];
    private readonly float[] velocityArray = new float[SONAR_MAX_DATA_COUNT];
    private readonly float[] maxRangeArray = new float[SONAR_MAX_DATA_COUNT];
    private readonly float[] maxLifetimeArray = new float[SONAR_MAX_DATA_COUNT];
    private readonly int[] type = new int[SONAR_MAX_DATA_COUNT];
    private readonly Vector3[] colorOverrides = new Vector3[SONAR_MAX_DATA_COUNT];

    private float timeCounterSeconds;
    private bool isSonarUpdateQueued;

    public override void _Ready()
    {
        Instance = this;
        foreach (var path in SonarPaths)
        {
            SonarMaterials.Add(ResourceLoader.Load<ShaderMaterial>(path));
        }
        MonsterShaderMaterial = ResourceLoader.Load<ShaderMaterial>(PATH_MONSTER_EDGE_DETECTION);

        var clearOutdatedPulseDataTimer = new Timer
        {
            WaitTime = 0.2f,
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

    public static void Pulse(Vector3 from, float velocity, float maxRange, float maxLifetime, PulseType pulseType = PulseType.NORMAL, ColorOverride colorOverride = ColorOverride.NONE)
    {
        Instance.InternalPulse(from, velocity, maxRange, maxLifetime, pulseType, colorOverride);
    }

    public static void EraseSonarPulseData()
    {
        Instance.InternalEraseSonarPulseData();
    }

    public static void EnableMonsterForceEdgeCheck()
    {
        Instance.MonsterShaderMaterial.SetShaderParameter(PARAM_FORCE_EDGE_CHECK, true);
    }

    public static void DisableMonsterForceEdgeCheck()
    {
        Instance.MonsterShaderMaterial.SetShaderParameter(PARAM_FORCE_EDGE_CHECK, false);
    }

    private void InternalPulse(Vector3 from, float velocity, float maxRange, float maxLifetime, PulseType pulseType = PulseType.NORMAL, ColorOverride colorOverride = ColorOverride.NONE)
    {
        var pulseData = new PulseData()
        {
            Position = from,
            Timestamp = timeCounterSeconds,
            Velocity = velocity,
            MaxRange = maxRange,
            MaxLifetime = maxLifetime,
            Type = PulseTypeToInteger(pulseType),
            ColorOverride = ColorOverrideToVector3(colorOverride),
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
            type[i] = pulseData.Type;
            colorOverrides[i] = pulseData.ColorOverride;
        }

        foreach (var sonarMaterial in SonarMaterials)
        {
            sonarMaterial.SetShaderParameter(PULSE_PARAM_POSITIONS, positionArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_TIMESTAMPS, timestampArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_VELOCITY, velocityArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_MAX_RANGE, maxRangeArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_MAX_LIFETIME, maxLifetimeArray);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_TYPE, type);
            sonarMaterial.SetShaderParameter(PULSE_PARAM_COLOR_OVERRIDE, colorOverrides);
        }
    }

    private void OnClearOutdatedPulseDataTimerTimeout()
    {
        ClearOutdatedPulseData();
    }

    private static int PulseTypeToInteger(PulseType pulseType)
    {
        return pulseType switch
        {
            PulseType.ONLY_RING => 1,
            _ => 0,
        };
    }

    private static Vector3 ColorOverrideToVector3(ColorOverride colorOverride)
    {
        return colorOverride switch
        {
            ColorOverride.WHITE => Vector3.One,
            ColorOverride.RED => new Vector3(1.0f, 0.0f, 0.0f),
            _ => Vector3.Zero,
        };
    }

    public readonly record struct PulseData
    {
        public Vector3 Position { get; init; }
        public float Timestamp { get; init; }
        public float Velocity { get; init; }
        public float MaxRange { get; init; }
        public float MaxLifetime { get; init; }
        public int Type { get; init; }
        public Vector3 ColorOverride { get; init; }
    }
}