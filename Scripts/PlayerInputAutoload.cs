using Godot;

public partial class PlayerInputAutoload : Node
{
	private const string INPUT_NAME_MOVE_FORWARD = "move_forward";
	private const string INPUT_NAME_MOVE_BACKWARD = "move_backward";
	private const string INPUT_NAME_MOVE_LEFT = "move_left";
	private const string INPUT_NAME_MOVE_RIGHT = "move_right";

    public static Vector2 MoveVector { get; private set; } = Vector2.Zero;

    public static Vector2 MouseMovement { get; private set; } = Vector2.Zero;

    public override void _Process(double delta)
    {
        MoveVector = Input.GetVector(INPUT_NAME_MOVE_LEFT, INPUT_NAME_MOVE_RIGHT, INPUT_NAME_MOVE_BACKWARD, INPUT_NAME_MOVE_FORWARD).Normalized();
    }
}