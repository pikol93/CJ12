using Godot;
using Godot.Collections;
using System;

public partial class Monster : CharacterBody3D
{
	private const uint SIGHT_COLLISION_MASK = 1;

	[Export]
	private NodePath NavigationAgentNodePath { get; set; }
	[Export]
	private NodePath EyesNodePath { get; set; }
	[Export]
	private float WalkSpeed { get; set; } = 2.5f;
	[Export]
	private float RunSpeed { get; set; } = 5.0f;
	[Export]
	private float PlayerNoticeThreshold { get; set; } = 1.0f;
	[Export]
	private float PlayerNoticeMaximum { get; set; } = 1.5f;

	private NavigationAgent3D NavigationAgent { get; set; }
	private Node3D Eyes { get; set; }
	private Character Player { get; set; }

	private MonsterState state = MonsterState.IDLE;
	private float playerNotice = 0.0f;


	public override void _Ready()
	{
		NavigationAgent = this.GetNodeOrThrow<NavigationAgent3D>(NavigationAgentNodePath);
		Eyes = this.GetNodeOrThrow<Node3D>(EyesNodePath);
	}

	public override void _PhysicsProcess(double delta)
	{
		ValidatePlayerInstance();
		NavigationAgent.TargetPosition = GlobalData.LastKnownPlayerPosition;

		var direction = (NavigationAgent.GetNextPathPosition() - GlobalPosition).Normalized();
		var movementSpeed = GetMovementSpeed();

		Velocity = direction * movementSpeed;
		MoveAndSlide();

		HandlePlayerNotice((float)delta);
	}

	private void SetState(MonsterState state)
	{
		GD.Print($"Change state: {this.state} -> {state}");
		this.state = state;
	}

	private void HandlePlayerNotice(float delta)
	{
		playerNotice += CanSeePlayer() ? delta : -delta;
		if (playerNotice > PlayerNoticeThreshold && state != MonsterState.CHASE)
		{
			SetState(MonsterState.CHASE);
		}
	}

	private void ValidatePlayerInstance()
	{
		if (Player == null)
		{
			Array<Node> nodes = GetTree().GetNodesInGroup("player");
			if (nodes.Count > 0)
			{
				Player = (Character)nodes[0];
				GD.Print($"Set new player instance: {Player}");
			}
		}
		else
		{
			if (!IsInstanceValid(Player) || !Player.IsInsideTree() || Player.IsDead())
			{
				Player = null;
				GD.Print("Invalidated the player instance.");
			}
		}
	}

    private bool CanSeePlayer()
    {
		if (Player == null)
		{
			return false;
		}

		Vector3 playerEyesPosition = Player.GetEyesPosition();
		Vector3 eyesPosition = Eyes.GlobalPosition;

		var rayParams = PhysicsRayQueryParameters3D.Create(eyesPosition, playerEyesPosition, SIGHT_COLLISION_MASK);
		var intersectResult = GetWorld3D().DirectSpaceState.IntersectRay(rayParams);

		if (intersectResult.Count <= 0) {
			// No collision occurred.
			return true;
		}

		return false;
    }

    private float GetMovementSpeed()
	{
		if (state == MonsterState.CHASE)
		{
			return RunSpeed;
		}

		return WalkSpeed;
	}
}
