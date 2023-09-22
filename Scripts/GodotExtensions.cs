using System;
using System.Runtime.CompilerServices;
using Godot;

public static class GodotExtensions
{
    public static T GetNodeOrThrow<T>(this Node node, NodePath path)
    where T : class
    {
        return node.GetNode<T>(path) ?? throw new NullReferenceException($"Could not find node by path: {path}");
    }
}