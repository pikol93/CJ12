using Godot;

public partial class GlobalData : Node
{
    public static Vector3 LastKnownPlayerPosition { get; set; } = new Vector3(999999.0f, 999999.0f, 999999.0f); 
} 