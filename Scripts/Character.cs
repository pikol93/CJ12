using System;
using Godot;

public partial class Character : CharacterBody3D
{
	[Export]
	private float MovementSpeed { get; set; }
	[Export]
	private float MouseSensitivity { get; set; }
	[Export]
	private NodePath NeckNodePath { get; set; }

	private Node3D Neck { get; set; }
	private float Pitch { get; set;}

    public override void _Ready()
    {
		Neck = this.GetNodeOrThrow<Node3D>(NeckNodePath);
    }

    public override void _Process(double delta)
	{
		Vector2 input = PlayerInputAutoload.MoveVector;
		Vector3 movementDirection = Transform.Basis * new Vector3(input.X, 0, -input.Y).Normalized();
		Velocity = movementDirection * MovementSpeed;
		GD.Print($"Input: {input}, movementDirection = {movementDirection}, Velocity = {Velocity}");
		MoveAndSlide();
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
}
