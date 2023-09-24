using System;
using Godot;

public partial class GameManager : Node
{
    private const string ACTION_ENABLE_MONSTER_FORCE_EDGE_CHECK = "enable_monster_force_edge_check";
    private const string ACTION_DISABLE_MONSTER_FORCE_EDGE_CHECK = "disable_monster_force_edge_check";

    public override void _Process(double delta)
    {
        if (OS.IsDebugBuild())
        {
            if (Input.IsActionJustPressed(ACTION_ENABLE_MONSTER_FORCE_EDGE_CHECK))
            {
                ShaderControllerAutoload.EnableMonsterForceEdgeCheck();
            }
            if (Input.IsActionJustPressed(ACTION_DISABLE_MONSTER_FORCE_EDGE_CHECK))
            {
                ShaderControllerAutoload.DisableMonsterForceEdgeCheck();
            }
        }
    }
}