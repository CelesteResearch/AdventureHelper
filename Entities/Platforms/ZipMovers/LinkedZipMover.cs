// SPDX-License-Identifier: MIT

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.Easing;
using Celeste.Core.Mathematics.Extrema;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Components.Timing;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Visibility;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Everest.Modules.Runtime;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Mods.AdventureHelper.Modules;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;

namespace Celeste.Mods.AdventureHelper.Entities.Platforms.ZipMovers
{
    [CustomEntity("AdventureHelper/LinkedZipMover")]
    [RuntimeTypeAlias("Celeste.Mod.AdventureHelper.Entities.LinkedZipMover")]
    public sealed class LinkedZipMover : LinkedZipMoverState
    {
        public const string DefaultPath = "objects/zipmover";

        public LinkedZipMover(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorld world, LinkedZipMoverSounds sounds)
            : base(engine, data, offset, world, sounds, DefaultPath, true)
        {
        }

        public static LinkedZipMover Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            var engine = level.EngineContext;
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<AdventureHelperModule>().Single();
            return new LinkedZipMover(engine, data, offset,
                level.SolidWorldFactory.Create(data.Position + offset, false), module.LinkedZipSounds);
        }

        protected override IEnumerator Sequence()
        {
            Vector2 origin = Position;
            while (true)
            {
                while (!HasActivationRider() && !SyncFlag) yield return null;
                SyncFlag = true;
                sounds.Play(ColorCode, LinkedZipMoverSoundType.Returning, this);
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Short);
                StartShaking(.1f);
                yield return .1f;
                SyncFlag = false;
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                float at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    var to = ZipMoverMotion.ForwardAtSpeed(engine, origin, target, speedMultiplier, ref at,
                        ref percent);
                    ZipMoverDrawing.ScrapeParticles(this, engine, to);
                    if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.1f, engine)) pathRenderer.CreateSparks();
                    MoveTo(to);
                }

                StartShaking(.2f);
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                    RumbleLength.Medium);
                ((OriginalLevelSceneRoot)Scene).Shake(.3f);
                StopPlayerRunIntoAnimation = true;
                yield return .5f;
                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(2);
                float returningProgress = 0f;
                while (returningProgress < 1f)
                {
                    yield return null;
                    var to = ZipMoverMotion.BackwardAtSpeed(engine, target, origin, speedMultiplier,
                        ref returningProgress, ref percent);
                    MoveTo(to);
                }

                StopPlayerRunIntoAnimation = true;
                StartShaking(.2f);
                streetlight.SetAnimationFrame(1);
                yield return .5f;
                StopSequenceSound();
            }
        }
    }
}
