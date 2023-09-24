using System;
using Godot;

public partial class Character : CharacterBody3D
{
	[Export]
	private float WalkSpeed { get; set; } = 2.0f;
	[Export]
	private float RunningSpeed { get; set; } = 5.0f;
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
		isRunning = Input.IsActionPressed("run");
		float movementSpeed = isRunning ? RunningSpeed : WalkSpeed;

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

    private void CreatePulse()
    {
		Vector3 position = Neck.GlobalPosition;
		ShaderControllerAutoload.Pulse(position, 7.0f, 15.0f, 3f);
    }

	private void OnStep()
	{
		ShaderControllerAutoload.Pulse(GlobalPosition, 8.0f, 4.0f, 0.8f);
		StepPlayer.Play();
	}
}
