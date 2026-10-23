// SPDX-License-Identifier: MIT
// Copyright (c) 2021 Ács Bence Tamás; 2022 Communal Helper Team.

using System.Numerics;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Scheduling;

namespace Celeste.Mods.AdventureHelper.Runtime.Movement.Paths
{
    public sealed class ClosedPathMotionState
    {
        private readonly ISourceEngineContext engine;
        public Vector2[] Path { get; }
        public int CurrentStart { get; private set; }
        public float MoveTime { get; }
        public float PauseTime { get; }
        public float PauseTimer;
        public float Percent { get; private set; }
        public float Angle { get; private set; }
        public Vector2 Position { get; private set; }

        public ClosedPathMotionState(ISourceEngineContext engine, Vector2[] path, float moveTime, float pauseTime)
        {
            this.engine = engine;
            Path = path;
            MoveTime = moveTime;
            PauseTime = pauseTime;
            UpdateAngle();
            UpdatePosition();
        }

        public void UpdatePosition() => Position = Vector2Math.Lerp(Path[CurrentStart],
            Path[(CurrentStart + 1) % Path.Length],
            ((ISourceEasingEnvironment)engine).EasingArithmetic.SineInOut(Percent),
            ((ISourceScalarEnvironment)engine).ScalarArithmetic);

        public void UpdateAngle() =>
            Angle = Vector2Geometry.Angle(Path[(CurrentStart + 1) % Path.Length] - Path[CurrentStart]);

        public ClosedPathMotionStep Advance()
        {
            if (PauseTimer > 0f)
            {
                PauseTimer -= engine.DeltaTime;
                return PauseTimer <= 0f ? ClosedPathMotionStep.SegmentStarted : ClosedPathMotionStep.Paused;
            }

            Percent = ScalarMath.Approach(Percent, 1f, engine.DeltaTime / MoveTime,
                ((ISourceExtremaEnvironment)engine).ExtremaArithmetic);
            UpdatePosition();
            if (Percent >= 1f)
            {
                CurrentStart = (CurrentStart + 1) % Path.Length;
                PauseTimer = PauseTime;
                Percent = 0f;
                return ClosedPathMotionStep.SegmentFinished;
            }

            return ClosedPathMotionStep.Moving;
        }
    }
}
