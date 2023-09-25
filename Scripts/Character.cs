using System;
using System.Collections.Generic;
using Godot;
using Godot.Collections;

public partial class Character : CharacterBody3D
{
	public readonly record struct PulseData
	{
		public Vector3 Position { get; init; }
		public float Timestamp { get; init; }
		public float Velocity { get; init; }
		public float MaxLifetime { get; init; }
	}

	private const string GROUP_MONSTERS = "monsters";

	private static readonly StepPulseData STEP_PULSE_DATA_SNEAK = new(6.0f, 1.0f, 0.0f);
	private static readonly StepPulseData STEP_PULSE_DATA_WALK = new(8.0f, 2.0f, 0.8f);
	private static readonly StepPulseData STEP_PULSE_DATA_RUN = new(10.0f, 3.5f, 1.3f);

	private readonly LinkedList<PulseData> pulseDataList = new();

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
	[Export]
	private float KillPitchRotation { get; set; } = 0.5f;
	[Export]
	private float ManualPulseVelocity { get; set; } = 7.0f;
	[Export]
	private float ManualPulseRange { get; set; } = 7.0f;
	[Export]
	private float ManualPulseLifetime { get; set; } = 3.0f;
	[Export]
	private float PulseDetectionEpsilon { get; set; } = 2.0f;

	private Node3D Neck { get; set; }
	private AudioStreamPlayer3D StepPlayer { get; set; }
	private float Pitch { get; set; }

	private Node3D MonsterEyes;

	private bool isSneaking = false;
	private bool isRunning = false;
	private float stepLeft;
	private float currentTime;

	public override void _Ready()
	{
		stepLeft = StepDistance;
		Neck = this.GetNodeOrThrow<Node3D>(NeckNodePath);
		StepPlayer = this.GetNodeOrThrow<AudioStreamPlayer3D>(StepPlayerPath);
	}

	public override void _Process(double delta)
	{
		currentTime += (float)delta;
		if (MonsterEyes != null)
		{
			LookAt(MonsterEyes.GlobalPosition, Vector3.Up);
			Neck.Rotation = new Vector3(KillPitchRotation, 0.0f, 0.0f);
			return;
		}

		ProcessPulseNotifyingMonsters();

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

		if (Input.IsActionJustPressed("create_pulse"))
		{
			CreateManualPulse();
		}
	}

	public override void _Input(InputEvent inputEvent)
	{
		if (MonsterEyes != null)
		{
			return;
		}

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

	public void SetKillState(Node3D monsterEyes)
	{
		MonsterEyes = monsterEyes;
	}

	public bool IsDead()
	{
		return false;
	}

	public Vector3 GetEyesPosition()
	{
		return Neck.GlobalPosition;
	}

	private void ProcessPulseNotifyingMonsters()
	{
		Array<Node> nodes = GetTree().GetNodesInGroup(GROUP_MONSTERS);

		foreach (var node in nodes)
		{
			if (node is not Monster monster)
			{
				continue;
			}

			var pulseNode = pulseDataList.First;
			while (pulseNode != null)
			{
				var nextNode = pulseNode.Next;
				float timeSincePulseDataCreation = currentTime - pulseNode.Value.Timestamp;
				if (timeSincePulseDataCreation > pulseNode.Value.MaxLifetime)
				{
					pulseDataList.Remove(pulseNode);
				}
				else
				{
					float distanceToMonster = pulseNode.Value.Position.DistanceTo(monster.GlobalPosition);
					float distanceTravelled = (timeSincePulseDataCreation * pulseNode.Value.Velocity) - PulseDetectionEpsilon;
					if (distanceToMonster < distanceTravelled)
					{
						pulseDataList.Remove(pulseNode);
						monster.NotifyPlayerPulse(pulseNode.Value.Position);
					}
				}

				pulseNode = nextNode;
			}
		}
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

	private void CreateManualPulse()
	{
		var position = GlobalPosition;
		CreatePulse(position, ManualPulseVelocity, ManualPulseRange, ManualPulseLifetime, false);
	}

	private void CreatePulse(Vector3 position, float velocity, float range, float maxLifetime, bool shouldAlertMonster = true)
	{
		if (shouldAlertMonster)
		{
			float alertLifetime = range / velocity;

			var pulseData = new PulseData()
			{
				Position = position,
				Velocity = velocity,
				Timestamp = currentTime,
				MaxLifetime = alertLifetime,
			};

			pulseDataList.AddFirst(pulseData);
		}

		ShaderControllerAutoload.Pulse(position, velocity, range, maxLifetime);
	}

	private void OnStep()
	{
		var stepPulseData = GetStepPulseData();
		CreatePulse(GlobalPosition, stepPulseData.Velocity, stepPulseData.Range, stepPulseData.MaxLifetime);
		StepPlayer.Play();
	}

	private readonly record struct StepPulseData(float Velocity, float Range, float MaxLifetime);
}
