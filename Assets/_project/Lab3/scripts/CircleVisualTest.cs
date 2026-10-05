using System;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.UIElements;

namespace Mechanics
{
    public sealed class CircleVisualTest : MonoBehaviour
    {
        #region Struct

        [Serializable]
        public struct AnimatedValueConfig
        {
            public float ChangeRate;
            public float Min;
            public float Max;
        }

        #endregion

        #region Values

        [SerializeField] private Transform _circlePrefab = null!;

        public AnimatedValueConfig Radius = new()
        {
            ChangeRate = 1f,
            Min = 1f,
            Max = 5f
        };
        public AnimatedValueConfig XMove = new()
        {
            ChangeRate = 1f,
            Min = -10f,
            Max = 5f
        };
        public float ColorChangeRate = 5f;
        [SerializeField] private Color[] _colors;
        /*    public AnimatedValueConfig Red = new()
            {
                ChangeRate = 5f,
                Min = 0,
                Max = 1f
            };*/

        private Transform[] _ballsTransform = null!;
        private readonly Ball[] _balls = { new(), new() };
        #endregion

        private void Start()
        {
            if (BallSpawner.Instance == null)
            {
                Debug.LogError("BallSpawner instance is null. Make sure there is a BallSpawner in the scene.");
                return;
            }

            _ballsTransform = new Transform[_balls.Length];
            for (int i = 0; i < _balls.Length; i++)
            {
                var t = BallSpawner.Instance.SpawnBall();
                _ballsTransform[i] = t;
            }

            _balls[1].Color = Color.white;
        }
        private void FixedUpdate()
        {
            { //ball 01
                var x = GetAnimatedValue(XMove);
                _balls[0].Position.x = x;

                var radius = GetAnimatedValue(Radius);
                _balls[0].Radius = radius;

                var colorIndex = GetAnimatedValue(new()
                {
                    ChangeRate = ColorChangeRate,
                    Min = 0,
                    Max = _colors.Length
                });

                int baseline = (int)colorIndex;
                int firstColorIndex = baseline % _colors.Length;
                int secondColorIndex = (baseline + 1) % _colors.Length;

                float secondColorPortion = colorIndex - baseline; // second color portion
                float firstColorPortion = 1 - secondColorPortion;

                Color a = _colors[firstColorIndex];
                Color b = _colors[secondColorIndex];
                Color c = a * firstColorPortion + b * secondColorPortion;

                _balls[0].Color = c;
            }

            {//ball 02
                _balls[1].Position.x += 0.01f;
                _balls[1].Radius += 0.01f;
            }

            for (int i = 0; i < _balls.Length; i++)
            {
                BallHelper.ApplyBallData(new BallEntity
                {
                    Ball = _balls[i],
                    TransformInWorld = _ballsTransform[i]
                });
            }
        }

        private static float GetAnimatedValue(AnimatedValueConfig config)
        {
            var t01 = GetParameter01(config.ChangeRate);
            var ret = t01 * (config.Max - config.Min) + config.Min;
            return ret;
        }

        private static float GetParameter01(float changeRate)
        {
            var timeSinceStartInSeconds = Time.time;

            var sin = math.sin(changeRate * timeSinceStartInSeconds);
            var t02 = sin + 1;
            var t01 = t02 / 2;

            return t01;
        }
    }

}
