using Godot;
using Godot.Collections;
using System;

public partial class Monster : CharacterBody3D
{
	[Export]
	private NodePath NavigationAgentNodePath { get; set; }
	[Export]
	private NodePath EyesNodePath { get; set; }
	[Export]
	private float WalkSpeed { get; set; } = 2.5f;
	[Export]
	private float RunSpeed { get; set; } = 5.0f;

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
	}

	private void SetState(MonsterState state)
	{
		GD.Print($"Change state: {this.state} -> {state}");
		this.state = state;
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

    private float GetMovementSpeed()
	{
		if (state == MonsterState.CHASE)
		{
			return RunSpeed;
		}

		return WalkSpeed;
	}
}
