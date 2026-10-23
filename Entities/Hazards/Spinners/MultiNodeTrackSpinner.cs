// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Levels.Cutscenes;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Physics.Collision.Shapes;
using Celeste.Scene.Entities;
using Celeste.Simulation;
using Celeste.Mods.AdventureHelper.Runtime.Movement.Paths;
using Celeste.Simulation.Gameplay.Player.Collision.Components;

namespace Celeste.Mods.AdventureHelper.Entities.Hazards.Spinners
{
    [RuntimeTypeAlias("Celeste.Mod.AdventureHelper.Entities.MultipleNodeTrackSpinner")]
    public class MultiNodeTrackSpinner : SceneEntityState
    {
        protected readonly ISourceEngineContext Engine;
        protected readonly ClosedPathMotionState Motion;

        public float PauseTimer
        {
            get => Motion.PauseTimer;
            set => Motion.PauseTimer = value;
        }

        public float Percent => Motion.Percent;
        public Vector2[] Path => Motion.Path;
        public int CurrentStart => Motion.CurrentStart;
        public float MoveTime => Motion.MoveTime;
        public float PauseTime => Motion.PauseTime;
        public bool Moving { get; private set; } = true;
        public float Angle => Motion.Angle;
        public string PauseFlag { get; }
        public bool HasPauseFlag { get; }
        public bool PlayerDead { get; private set; }
        public bool PauseOnCutscene { get; }

        public MultiNodeTrackSpinner(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            Engine = engine;
            PauseOnCutscene = data.Bool("pauseOnCutscene");
            PauseFlag = data.Attr("pauseFlag", "");
            HasPauseFlag = PauseFlag != "";
            Collider = new ColliderListGeometry(
                new CircleColliderGeometry(((ISourceVectorEnvironment)engine).VectorArithmetic, 6));
            Add(new PlayerColliderComponentState(OnPlayer));
            var path = new Vector2[data.Nodes.Length + 1];
            path[0] = data.Position + offset;
            for (int i = 0; i < data.Nodes.Length; i++) path[i + 1] = data.Nodes[i] + offset;
            Motion = new ClosedPathMotionState(engine, path, data.Float("moveTime", .4f), data.Float("pauseTime", .2f));
            Position = Motion.Position;
        }

        public virtual void OnPlayer(RoomPlayerSession player)
        {
            PlayerDead =
                player.Die(
                    ((ISourceVectorEnvironment)Engine).VectorArithmetic.SafeNormalize(player.Position - Position)) !=
                null;
            if (PlayerDead) Moving = false;
        }

        public override void Added(object scene)
        {
            base.Added(scene);
            if (HasPauseFlag) ((OriginalLevelSceneRoot)scene).Session.SetFlag(PauseFlag, false);
        }

        public override void Awake(object scene)
        {
            base.Awake(scene);
            OnTrackStart();
        }

        public void UpdatePosition()
        {
            Motion.UpdatePosition();
            Position = Motion.Position;
        }

        public override void Update()
        {
            base.Update();
            var level = (OriginalLevelSceneRoot)Scene;
            bool cutscene = false;
            if (PauseOnCutscene)
                foreach (var entity in level.Scheduler.Entities.Entities)
                    if (entity is OriginalCutsceneEntityState { Running: true })
                        cutscene = true;
            Moving = !cutscene && !(HasPauseFlag && level.Session.GetFlag(PauseFlag));
            if (!Moving || PlayerDead) return;
            var step = Motion.Advance();
            if (step == ClosedPathMotionStep.Moving || step == ClosedPathMotionStep.SegmentFinished)
                Position = Motion.Position;
            if (step == ClosedPathMotionStep.SegmentStarted) OnTrackStart();
            else if (step == ClosedPathMotionStep.SegmentFinished) OnTrackEnd();
        }

        public virtual void OnTrackStart()
        {
        }

        public virtual void OnTrackEnd() => Motion.UpdateAngle();
    }
}
