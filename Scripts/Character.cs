using Godot;

public partial class Character : CharacterBody3D
{
	[Export]
	private float MovementSpeed { get; set; }

    public override void _Process(double delta)
    {
		Vector2 input = PlayerInputAutoload.Movement;
		Vector3 movementDirection = Transform.Basis * new Vector3(input.X, 0, -input.Y).Normalized();
		Velocity = movementDirection * MovementSpeed;
		GD.Print($"Input: {input}, movementDirection = {movementDirection}, Velocity = {Velocity}");
		MoveAndSlide();
    }
}
