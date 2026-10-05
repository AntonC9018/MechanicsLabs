using System;
using UnityEngine;

namespace Mechanics
{
    [Serializable]
    public sealed class Ball
    {
        public float Mass = 1f;
        public Vector2 Position;
        public Vector2 Velocity;
        public float Radius;
        public Color Color;
    }

    public readonly record struct BallEntity(
        Ball Ball, 
        Transform TransformInWorld);

/*    public readonly struct BallEntity
    {
        public Ball Ball { get; init; }
        public Transform TransformInWorld { get; init; }
    }*/

    public static class BallHelper
    {
        public static void ApplyBallData(BallEntity b)
        {
            Vector3 pos = b.Ball.Position;
            pos.z = b.TransformInWorld.position.z;

            b.TransformInWorld.position = pos;

            var scale = b.Ball.Radius * 2;
            var scaleVector = new Vector3(scale, scale, scale);
            b.TransformInWorld.localScale = scaleVector;

            var renderer = b.TransformInWorld.GetComponent<SpriteRenderer>();
            renderer.color = b.Ball.Color;
        }

        public static void ApplyMovement(Ball ball, float deltaTime)
        {
            var movementVec = ball.Velocity * deltaTime;
            ball.Position += movementVec;
        }
    }
}
