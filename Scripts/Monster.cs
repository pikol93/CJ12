using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

public partial class Monster : CharacterBody3D
{
	public readonly record struct PulseData
	{
		public Vector3 Position { get; init; }
		public float Timestamp { get; init; }
		public float Velocity { get; init; }
		public float MaxLifetime { get; init; }
	}

	private const string ANIMATION_NAME_CHASE = "Chase";
	private const string ANIMATION_NAME_IDLE = "Idle";
	private const string ANIMATION_NAME_WALK = "Walk";
	private const string ANIMATION_NAME_KILL = "Kill";
	private const string GROUP_MONSTER_WAYPOINT = "monster_waypoint";

	private static readonly Random RANDOM = new();

	private readonly LinkedList<PulseData> pulseDataList = new();

	[Export]
	private NodePath NavigationAgentNodePath { get; set; }
	[Export]
	private NodePath EyesNodePath { get; set; }
	[Export]
	private NodePath AnimationPlayerNodePath { get; set; } = new NodePath("monster/AnimationPlayer");
	[Export]
	private NodePath BreathingAudioPlayerNodePath { get; set; }
	[Export]
	private NodePath ScreechAudioPlayerNodePath { get; set; }
	[Export]
	private float WalkSpeed { get; set; } = 2.5f;
	[Export]
	private float RunSpeed { get; set; } = 5.0f;
	[Export]
	private float MinIdleTime { get; set; } = 3.0f;
	[Export]
	private float MaxIdleTime { get; set; } = 7.0f;
	[Export]
	private float MinRoarTime { get; set; } = 20.0f;
	[Export]
	private float MaxRoarTime { get; set; } = 40.0f;
	[Export]
	private float MinSonarPassiveTime { get; set; } = 2.0f;
	[Export]
	private float MaxSonarPassiveTime { get; set; } = 2.2f;
	[Export]
	private float MinSonarAggressiveTime { get; set; } = 1.0f;
	[Export]
	private float MaxSonarAggressiveTime { get; set; } = 1.2f;
	[Export]
	private float RoarPulseVelocity { get; set; } = 10.0f;
	[Export]
	private float RoarPulseLifetime { get; set; } = 10.0f;
	[Export]
	private int RoarPulses { get; set; } = 3;
	[Export]
	private float SonarPulseVelocity { get; set; } = 3.0f;
	[Export]
	private float SonarPulseLifetime { get; set; } = 1.0f;
	[Export]
	private int SonarPulses { get; set; } = 1;
	[Export]
	private float RoarPulseDelay { get; set; } = 0.1f;
	[Export]
	private float TargetDistanceThreshold { get; set; } = 0.2f;
	[Export]
	private float KillZoneRadius { get; set; } = 1.5f;
	[Export]
	private float TargetPlayerDistanceToEnemyWhenInKillState { get; set; } = 1.0f;
	[Export]
	private float MinimumTimeBetweenBreathingSounds { get; set; } = 10.0f;

	private NavigationAgent3D NavigationAgent { get; set; }
	private Node3D Eyes { get; set; }
	private AnimationPlayer AnimationPlayer { get; set; }
	private AudioStreamPlayer3D BreathingAudioPlayer { get; set; }
	private AudioStreamPlayer3D ScreechAudioPlayer { get; set; }
	private Character Player { get; set; }

	private MonsterState state = MonsterState.IDLE;

	private float timeLeftToRoar = 50.0f;
	private float timeLeftToSonar = 0.0f;
	private float idleTimeLeft = 0.0f;
	private Vector3 lastKnownPlayerPosition = Vector3.Zero;
	private Vector3 walkTarget = Vector3.Zero;
	private float timeSinceLastBreathingPlay = 0.0f;

	private float timePassed = 0.0f;

	public override void _Ready()
	{
		ShaderControllerAutoload.DisableMonsterForceEdgeCheck();
		NavigationAgent = this.GetNodeOrThrow<NavigationAgent3D>(NavigationAgentNodePath);
		Eyes = this.GetNodeOrThrow<Node3D>(EyesNodePath);
		AnimationPlayer = this.GetNodeOrThrow<AnimationPlayer>(AnimationPlayerNodePath);
		BreathingAudioPlayer = this.GetNodeOrThrow<AudioStreamPlayer3D>(BreathingAudioPlayerNodePath);
		ScreechAudioPlayer = this.GetNodeOrThrow<AudioStreamPlayer3D>(ScreechAudioPlayerNodePath);

		SetState(state);

		AnimationPlayer.AnimationFinished += OnAnimationFinished;
	}

	public override void _Process(double delta)
	{
		ValidatePlayerInstance();

		float floatDelta = (float)delta;
		timePassed += floatDelta;
		switch (state)
		{
			case MonsterState.IDLE:
				ProcessStateIdle(floatDelta);
				break;
			case MonsterState.WALK:
				ProcessStateWalk(floatDelta);
				break;
			case MonsterState.CHASE:
				ProcessStateChase(floatDelta);
				break;
			case MonsterState.KILL:
				ProcessStateKill(floatDelta);
				break;
		}

		if (OS.IsDebugBuild())
		{
			if (Input.IsActionJustPressed("force_monster_roar"))
			{
				ForceRoar();
			}
		}

		DetectPlayerWithPulses();
		PlayBreathingIfNotAlreadyPlaying(floatDelta);
	}

	public override void _PhysicsProcess(double delta)
	{
		ValidatePlayerInstance();
		MoveAndSlide();
	}

	public void NotifyPlayerPulse(Vector3 position)
	{
		lastKnownPlayerPosition = position;
		SetState(MonsterState.CHASE);
	}

	private void ProcessStateIdle(float delta)
	{
		Velocity = Vector3.Zero;
		ProcessSonar(delta, MinSonarPassiveTime, MaxSonarPassiveTime);
		ProcessRoar((float)delta);
		ProcessPlayerKill();

		idleTimeLeft -= delta;
		if (idleTimeLeft < 0)
		{
			idleTimeLeft = GetRandomBetween(MinIdleTime, MaxIdleTime);
			walkTarget = GetNextWalkTarget();
			SetState(MonsterState.WALK);
		}
	}

	private void ProcessStateWalk(float delta)
	{
		ProcessSonar(delta, MinSonarPassiveTime, MaxSonarPassiveTime);
		ProcessRoar((float)delta);
		ProcessPlayerKill();
		float distanceLeft = MoveTowards(walkTarget);

		if (distanceLeft < TargetDistanceThreshold)
		{
			SetState(MonsterState.IDLE);
		}
	}

	private void ProcessStateChase(float delta)
	{
		ProcessSonar(delta, MinSonarAggressiveTime, MaxSonarAggressiveTime);
		ProcessPlayerKill();
		float distanceLeft = MoveTowards(lastKnownPlayerPosition);

		if (distanceLeft < TargetDistanceThreshold)
		{
			SetState(MonsterState.IDLE);
		}
	}

	private void ProcessStateKill(float delta)
	{
		if (Player != null)
		{
			Vector3 playerGlobalPosition = Player.GlobalPosition;
			Vector3 direction = (playerGlobalPosition - GlobalPosition).Normalized();
			float rotation = Mathf.Atan2(-direction.X, -direction.Z);
			Rotation = new Vector3(0, rotation, 0);

			Vector3 offset = direction * TargetPlayerDistanceToEnemyWhenInKillState;
			GlobalPosition = playerGlobalPosition - offset;
		}
	}

	private void ProcessPlayerKill()
	{
		if (Player != null)
		{
			Vector3 playerGlobalPosition = Player.GlobalPosition;
			float distance = GlobalPosition.DistanceTo(playerGlobalPosition);
			if (distance < KillZoneRadius)
			{
				OnPlayerKill();
			}
		}
	}

	/// <summary>
	/// Moves towards target.
	/// </summary>
	/// <param name="position">Position to walk to.</param>
	/// <returns>Distance left to target in straight line.</returns>
	private float MoveTowards(Vector3 position)
	{
		NavigationAgent.TargetPosition = position;
		Vector3 diff = NavigationAgent.GetNextPathPosition() - GlobalPosition;
		var distance = diff.Length();
		var direction = diff / distance;
		var movementSpeed = GetMovementSpeed();
		Velocity = direction * movementSpeed;

		float rotation = Mathf.Atan2(-direction.X, -direction.Z);
		Rotation = new Vector3(0, rotation, 0);

		return distance;
	}

	private void DetectPlayerWithPulses()
	{
		var node = pulseDataList.First;
		while (node != null)
		{
			var nextNode = node.Next;
			float timeSincePulseDataCreation = timePassed - node.Value.Timestamp;
			if (timeSincePulseDataCreation > node.Value.MaxLifetime)
			{
				pulseDataList.Remove(node);
			}
			else
			{
				float distanceToPlayer = node.Value.Position.DistanceTo(Player.GlobalPosition);
				float distanceTravelled = timeSincePulseDataCreation * node.Value.Velocity;
				if (distanceToPlayer < distanceTravelled)
				{
					pulseDataList.Remove(node);
					OnSonaredPlayer(Player.GlobalPosition);
				}
			}

			node = nextNode;
		}
	}

	private void PlayBreathingIfNotAlreadyPlaying(float delta)
	{
		timeSinceLastBreathingPlay += delta;
		if (timeSinceLastBreathingPlay < MinimumTimeBetweenBreathingSounds)
		{
			return;
		}

		if (!BreathingAudioPlayer.Playing)
		{
			timeSinceLastBreathingPlay = 0.0f;
			BreathingAudioPlayer.Play();
		}
	}

	private void ForceRoar()
	{
		ProcessRoar(999999.0f);
	}

	private void ProcessSonar(float delta, float sonarMin, float sonarMax)
	{
		// Sonar is just a regular pulse coming out of the monster
		timeLeftToSonar -= delta;
		if (timeLeftToSonar < 0)
		{
			timeLeftToSonar = GetRandomBetween(sonarMin, sonarMax);
			Vector3 sonarPosition = GlobalPosition;
			QueuePulses(SonarPulses, sonarPosition, SonarPulseVelocity, SonarPulseLifetime);
		}
	}

	private void ProcessRoar(float delta)
	{
		timeLeftToRoar -= delta;
		if (timeLeftToRoar < 0)
		{
			timeLeftToRoar = GetRandomBetween(MinRoarTime, MaxRoarTime);
			Vector3 roarPosition = Eyes.GlobalPosition;
			QueuePulses(RoarPulses, roarPosition, RoarPulseVelocity, RoarPulseLifetime);
			ScreechAudioPlayer.Play();
		}
	}

	private void SetState(MonsterState state)
	{
		GD.Print($"Change state: {this.state} -> {state}");
		this.state = state;

		string animationName = StateToAnimationName(state);
		AnimationPlayer.Play(animationName);
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

	private void ProducePulse(Vector3 position, float velocity, float lifetime)
	{
		if (!IsInsideTree())
		{
			return;
		}

		float range = RoarPulseVelocity * RoarPulseLifetime;

		var pulseData = new PulseData()
		{
			Position = position,
			Velocity = velocity,
			MaxLifetime = lifetime,
			Timestamp = timePassed,
		};

		pulseDataList.AddFirst(pulseData);
		ShaderControllerAutoload.Pulse(position, velocity, range, lifetime, PulseType.ONLY_RING, ColorOverride.RED);
	}

	private Vector3 GetNextWalkTarget()
	{
		// TODO: Decide randomly whether to wander around in place or choose another waypoint position.
		// Also it may be good to consider how much time has been spent near the player to not overwhelm them.
		return GetRandomMonsterWaypointPosition();
	}

	private Vector3 GetRandomMonsterWaypointPosition()
	{
		Array<Node> monsterWaypoints = GetTree().GetNodesInGroup(GROUP_MONSTER_WAYPOINT);

		List<Vector3> waypoints = new(monsterWaypoints.Count);
		foreach (var node in monsterWaypoints)
		{
			if (node is Node3D waypoint)
			{
				waypoints.Add(waypoint.GlobalPosition);
			}
		}

		waypoints.Sort((a, b) => a.DistanceSquaredTo(GlobalPosition).CompareTo(b.DistanceSquaredTo(GlobalPosition)));

		return waypoints[1];
	}

	private void QueuePulses(int count, Vector3 position, float velocity, float lifetime)
	{
		ProducePulse(position, velocity, lifetime);
		for (int i = 1; i < count; i++)
		{
			float delay = i * RoarPulseDelay;
			var timer = GetTree().CreateTimer(delay);
			timer.Timeout += () => ProducePulse(position, velocity, lifetime);
		}
	}

	private void OnPlayerKill()
	{
		GD.Print("OnPlayerKill");
		SetState(MonsterState.KILL);
		ShaderControllerAutoload.EnableMonsterForceEdgeCheck();
		Player.SetKillState(Eyes);
	}

	private void OnAnimationFinished(StringName animName)
	{
		if (animName == ANIMATION_NAME_KILL)
		{
			ShaderControllerAutoload.DisableMonsterForceEdgeCheck();
			// TODO: Change scene to indicate that the player lost
			GetTree().ReloadCurrentScene();
		}
	}

	private void OnSonaredPlayer(Vector3 playerPosition)
	{
		GD.Print($"Spotted player at: {playerPosition}");
		lastKnownPlayerPosition = playerPosition;
		SetState(MonsterState.CHASE);
	}

	private static float GetRandomBetween(float min, float max)
	{
		float diff = max - min;
		return min + (diff * (float)RANDOM.NextDouble());
	}

	private static string StateToAnimationName(MonsterState monsterState)
	{
		return monsterState switch
		{
			MonsterState.WALK => ANIMATION_NAME_WALK,
			MonsterState.CHASE => ANIMATION_NAME_CHASE,
			MonsterState.KILL => ANIMATION_NAME_KILL,
			_ => ANIMATION_NAME_IDLE,
		};
	}
}
