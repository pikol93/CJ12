using System;
using Godot;

public partial class Character : CharacterBody3D
{
	private static readonly StepPulseData STEP_PULSE_DATA_SNEAK = new(6.0f, 2.5f, 0.7f);
	private static readonly StepPulseData STEP_PULSE_DATA_WALK = new(8.0f, 3.0f, 0.8f);
	private static readonly StepPulseData STEP_PULSE_DATA_RUN = new(8.0f, 5.5f, 1.3f);

	[Export]
	private float SneakSpeed { get; set; } = 0.8f; 
	[Export]
	private float WalkSpeed { get; set; } = 2.0f;
	[Export]
	private float RunSpeed { get; set; } = 5.0f;
	[Export]
	private float MouseSensitivity { get; set; } = 1.0f;
	[Export]
	private NodePath NeckNodePath { get; set; }
	[Export]
	private NodePath StepPlayerPath { get; set; }
	[Export]
	private float StepDistance { get; set; } = 2.0f;

	private Node3D Neck { get; set; }
	private AudioStreamPlayer3D StepPlayer { get ;set; }
	private float Pitch { get; set;}

	private bool isSneaking = false;
	private bool isRunning = false;
	private float stepLeft;

    public override void _Ready()
    {
		stepLeft = StepDistance;
		Neck = this.GetNodeOrThrow<Node3D>(NeckNodePath);
		StepPlayer = this.GetNodeOrThrow<AudioStreamPlayer3D>(StepPlayerPath);
    }

    public override void _Process(double delta)
	{
		isSneaking = Input.IsActionPressed("sneak");
		isRunning = Input.IsActionPressed("run");
		float movementSpeed = GetMovementSpeed();

		GlobalData.LastKnownPlayerPosition = GlobalPosition;
		Vector2 input = PlayerInputAutoload.MoveVector;
		Vector3 movementDirection = Transform.Basis * new Vector3(input.X, 0, -input.Y).Normalized();
		Velocity = movementDirection * movementSpeed;
		MoveAndSlide();

		float distanceTravelled = Velocity.Length() * (float)delta;
		stepLeft -= distanceTravelled;
		if (stepLeft <= 0.0f)
		{
			stepLeft = StepDistance;
			OnStep();
		}

		if (Input.IsActionJustPressed("create_pulse")) {
			CreatePulse();
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		const float halfPi = Mathf.Pi / 2f;

		if (inputEvent is InputEventMouseMotion mouseMotion)
		{
			Vector2 mouseMotionVector = mouseMotion.Relative;
			float pitchDelta = mouseMotionVector.Y * MouseSensitivity;
			Pitch = Mathf.Clamp(Pitch - pitchDelta, -halfPi, halfPi);
			RotateY(-mouseMotionVector.X * MouseSensitivity);
			Neck.Rotation = new Vector3(Pitch, 0f, 0f);
		}
	}

	public bool IsDead()
	{
		return false;
	}

	public Vector3 GetEyesPosition()
	{
		return Neck.GlobalPosition;
	}

	private float GetMovementSpeed()
	{
		if (isRunning)
		{
			return RunSpeed;
		}

		if (isSneaking)
		{
			return SneakSpeed;
		}

		return WalkSpeed;
	}

	private StepPulseData GetStepPulseData()
	{
		if (isRunning)
		{
			return STEP_PULSE_DATA_RUN;
		}

		if (isSneaking)
		{
			return STEP_PULSE_DATA_SNEAK;
		}

		return STEP_PULSE_DATA_WALK;
	}

    private void CreatePulse()
    {
		var position = Neck.GlobalPosition;
		ShaderControllerAutoload.Pulse(position, 7.0f, 15.0f, 3f, PulseType.ONLY_RING);
    }

	private void OnStep()
	{
		var stepPulseData = GetStepPulseData();
		ShaderControllerAutoload.Pulse(GlobalPosition, stepPulseData.Velocity, stepPulseData.Range, stepPulseData.MaxLifetime, PulseType.NORMAL, ColorOverride.RED);
		StepPlayer.Play();
	}

	private readonly record struct StepPulseData(float Velocity, float Range, float MaxLifetime);
}
