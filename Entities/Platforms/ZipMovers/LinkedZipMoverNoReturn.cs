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
    [CustomEntity("AdventureHelper/LinkedZipMoverNoReturn")]
    [RuntimeTypeAlias("Celeste.Mod.AdventureHelper.Entities.LinkedZipMoverNoReturn")]
    public sealed class LinkedZipMoverNoReturn : LinkedZipMoverState
    {
        public const string DefaultPath = "objects/AdventureHelper/noreturnzipmover";
        private bool firstDirection = true;

        public LinkedZipMoverNoReturn(ISourceEngineContext engine, EntityData data, Vector2 offset,
            ISourceSolidWorld world, LinkedZipMoverSounds sounds)
            : base(engine, data, offset, world, sounds, DefaultPath, false)
        {
        }

        public static LinkedZipMoverNoReturn Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data)
        {
            var engine = level.EngineContext;
            var module = ((ISourceEverestModulesEnvironment)engine).Modules.OfType<AdventureHelperModule>().Single();
            return new LinkedZipMoverNoReturn(engine, data, offset,
                level.SolidWorldFactory.Create(data.Position + offset, false), module.LinkedZipSounds);
        }

        protected override IEnumerator Sequence()
        {
            Vector2 origin = Position;
            while (true)
            {
                while (!HasActivationRider() && !SyncFlag) yield return null;
                SyncFlag = true;
                sounds.Play(ColorCode, this);
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
                    var to = firstDirection
                        ? ZipMoverMotion.ForwardAtSpeed(engine, origin, target, speedMultiplier, ref at, ref percent)
                        : ZipMoverMotion.ForwardAtSpeed(engine, target, origin, speedMultiplier, ref at, ref percent);
                    ZipMoverDrawing.ScrapeParticles(this, engine, to);
                    if (((ISourceScene)Scene).SourceScene.Clock.OnInterval(.1f, engine)) pathRenderer.CreateSparks();
                    MoveTo(to);
                }

                StartShaking(.2f);
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                    RumbleLength.Medium);
                ((OriginalLevelSceneRoot)Scene).Shake(.3f);
                StopPlayerRunIntoAnimation = true;
                streetlight.SetAnimationFrame(2);
                yield return .5f;
                StopSequenceSound();
                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(1);
                firstDirection = !firstDirection;
            }
        }
    }
}
