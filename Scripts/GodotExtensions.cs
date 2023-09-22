using System;
using System.Runtime.CompilerServices;
using Godot;

public static class GodotExtensions
{
    public static T GetNodeOrThrow<T>(this Node node, NodePath path)
    where T : class
    {
        T result = node.GetNode<T>(path);

        if (result == null)
        {
            throw new NullReferenceException($"Could not find node by path: {path}");
        }

        return result;
    }
}