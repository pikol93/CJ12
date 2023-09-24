using Godot;
using System;

public partial class AmbientSoundPlayer : AudioStreamPlayer
{
	public override void _Process(double delta)
	{
		if (!Playing)
		{
			Play();
		}
	}
}
