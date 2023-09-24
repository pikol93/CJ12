using Godot;
using Godot.Collections;
using System;

public partial class Monster : CharacterBody3D
{
	private static readonly Random RANDOM = new();

	[Export]
	private NodePath NavigationAgentNodePath { get; set; }
	[Export]
	private NodePath EyesNodePath { get; set; }
	[Export]
	private float WalkSpeed { get; set; } = 2.5f;
	[Export]
	private float RunSpeed { get; set; } = 5.0f;
	[Export]
	private float MinRoarTime { get; set; } = 20.0f;
	[Export]
	private float MaxRoarTime { get; set; } = 40.0f;
	[Export]
	private float RoarPulseVelocity { get; set; } = 10.0f;
	[Export]
	private float RoarPulseLifetime { get; set; } = 10.0f;
	[Export]
	private int RoarPulses { get; set; } = 3;
	[Export]
	private float RoarPulseDelay { get; set; } = 0.1f;

	private NavigationAgent3D NavigationAgent { get; set; }
	private Node3D Eyes { get; set; }
	private Character Player { get; set; }

	private MonsterState state = MonsterState.IDLE;

	private float timeLeftToRoar = 50.0f;


	public override void _Ready()
	{
		NavigationAgent = this.GetNodeOrThrow<NavigationAgent3D>(NavigationAgentNodePath);
		Eyes = this.GetNodeOrThrow<Node3D>(EyesNodePath);
	}

    public override void _Process(double delta)
    {
		ProcessRoar((float)delta);

		if (OS.IsDebugBuild())
		{
			if (Input.IsActionJustPressed("force_monster_roar"))
			{
				ForceRoar();
			}
		}
    }

    public override void _PhysicsProcess(double delta)
	{
		ValidatePlayerInstance();
		NavigationAgent.TargetPosition = GlobalData.LastKnownPlayerPosition;

		var direction = (NavigationAgent.GetNextPathPosition() - GlobalPosition).Normalized();
		var movementSpeed = GetMovementSpeed();

		Velocity = direction * movementSpeed;
		// MoveAndSlide();
	}

	private void ForceRoar()
	{
		ProcessRoar(999999.0f);
	}

	private void ProcessRoar(float delta)
	{
		timeLeftToRoar -= delta;
		if (timeLeftToRoar < 0)
		{
			timeLeftToRoar = GetRandomRoarTime();
			OnRoar();
		}
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

	private float GetRandomRoarTime()
	{
		float diff = MaxRoarTime - MinRoarTime;
		return MinRoarTime + (diff * (float)RANDOM.NextDouble());
	}

	private void ProduceRoarPulse()
	{
		if (!IsInsideTree())
		{
			return;
		}

		Vector3 roarPosition = Eyes.GlobalPosition;
		float roarPulseRange = RoarPulseVelocity * RoarPulseLifetime;
		ShaderControllerAutoload.Pulse(roarPosition, RoarPulseVelocity, roarPulseRange, RoarPulseLifetime, PulseType.ONLY_RING, ColorOverride.RED);
	}
	
	private void OnRoar()
	{
		ProduceRoarPulse();
		for (int i = 1; i < RoarPulses; i++)
		{
			float delay = i * RoarPulseDelay;
			var timer = GetTree().CreateTimer(delay);
			timer.Timeout += ProduceRoarPulse;
		}
	}
}
