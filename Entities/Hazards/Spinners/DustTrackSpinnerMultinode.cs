// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Geometry;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Mathematics;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Physics.Collision;
using Celeste.Scene.Tracking;
using Celeste.Simulation;
using Celeste.Simulation.Gameplay.Hazards.Spinners.Dust;

namespace Celeste.Mods.AdventureHelper.Entities.Hazards.Spinners
{
    [CustomEntity("AdventureHelper/DustTrackSpinnerMultinode")]
    [RuntimeTypeAlias("Celeste.Mod.AdventureHelper.Entities.DustTrackSpinnerMultinode")]
    public sealed class DustTrackSpinnerMultinode : MultiNodeTrackSpinner
    {
        private readonly DustGraphicState dusty;
        private Vector2 previousVector, nextVector;

        public DustTrackSpinnerMultinode(ISourceEngineContext engine, EntityData data, Vector2 offset) : base(engine,
            data,
            offset)
        {
            Add(dusty = new DustGraphicState(engine, true, false, false));
            dusty.EyeDirection = dusty.EyeTargetDirection =
                ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(
                    Path[(CurrentStart + 1) % Path.Length] -
                    Path[CurrentStart]);
            dusty.OnEstablish = Establish;
            Depth = -50;
        }

        private bool CheckSolid(IntRect rectangle) => Celeste.Scene.Entities.SourceSceneCollision.CheckRect(
            ((ISourceScene)Scene).SourceScene.Entities, "Celeste.Solid", rectangle);

        private void Establish()
        {
            var start = Path[CurrentStart];
            var next = Path[(CurrentStart + 1) % Path.Length];
            var previous = Path[(CurrentStart - 1 + Path.Length) % Path.Length];
            var math = ((ISourceVectorEnvironment)Engine).VectorArithmetic;
            nextVector = math.SafeNormalize(next - start);
            previousVector = math.SafeNormalize(start - previous);
            bool wall = CheckSolid(new IntRect((int)(X + nextVector.X * 4) - 2, (int)(Y + nextVector.Y * 4) - 2, 4, 4));
            if (!wall)
            {
                nextVector = -nextVector;
                wall = CheckSolid(new IntRect((int)(X + nextVector.X * 4) - 2, (int)(Y + nextVector.Y * 4) - 2, 4, 4));
            }

            if (!wall) return;
            float length = math.Length(start - next);
            for (int i = 8; i < length && wall; i += 8)
                wall = wall && CheckSolid(new IntRect((int)(X + nextVector.X * 4 + previousVector.X * i) - 2,
                    (int)(Y + nextVector.Y * 4 + previousVector.Y * i) - 2, 4, 4));
            if (!wall) return;
            var nodes = nextVector.X < 0 ? dusty.LeftNodes :
                nextVector.X > 0 ? dusty.RightNodes :
                nextVector.Y < 0 ? dusty.TopNodes :
                nextVector.Y > 0 ? dusty.BottomNodes : null;
            if (nodes != null)
                foreach (var node in nodes)
                    node.Enabled = false;
            dusty.Position -= nextVector;
            dusty.EyeDirection = dusty.EyeTargetDirection = Vector2Geometry.AngleToVector(
                ScalarMath.AngleLerp(Vector2Geometry.Angle(previousVector), Vector2Geometry.Angle(nextVector), .3f), 1);
        }

        public override void Update()
        {
            base.Update();
            var level = (OriginalLevelSceneRoot)Scene;
            if (Moving && PauseTimer < 0 && level.OnInterval(.02f))
                level.ParticlesBG.Emit(((IOriginalGameplayProcess)Engine).ParticleTypes.DustStaticSpinner.P_Move, 1,
                    Position, Vector2.One * 4);
        }

        public override void OnPlayer(RoomPlayerSession player)
        {
            base.OnPlayer(player);
            dusty.OnHitPlayer();
        }

        public override void OnTrackEnd()
        {
            base.OnTrackEnd();
            var start = Path[CurrentStart];
            var previous = Path[(CurrentStart - 1 + Path.Length) % Path.Length];
            previousVector = ((ISourceVectorEnvironment)Engine).VectorArithmetic.SafeNormalize(previous - start);
            nextVector = Vector2Geometry.AngleToVector(Angle, 1);
            dusty.EyeTargetDirection =
                Vector2Geometry.AngleToVector(ScalarMath.AngleLerp(Vector2Geometry.Angle(previousVector), Angle, 1), 1);
        }
    }
}
