// SPDX-License-Identifier: MIT

using System.Collections;
using System.Numerics;
using Celeste.Game.Controls.Source;
using Celeste.Game.Entities.Platforms.ZipMovers.Motion;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Scene.Tracking;

namespace Celeste.Mods.AdventureHelper.Entities.Platforms.ZipMovers
{
    public partial class ZipMoverNoReturn
    {
        private IEnumerator Sequence()
        {
            Vector2 start = Position;
            while (true)
            {
                while (!HasPlayerRider()) yield return null;
                sfx.Play("event:/game/01_forsaken_city/zip_mover");
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Short);
                StartShaking(.1f);
                yield return .1f;
                streetlight.SetAnimationFrame(3);
                StopPlayerRunIntoAnimation = false;
                float at = 0f;
                while (at < 1f)
                {
                    yield return null;
                    var to = firstDirection
                        ? ZipMoverMotion.ForwardAtSpeed(engine, start, target, speedMultiplier, ref at, ref percent)
                        : ZipMoverMotion.ForwardAtSpeed(engine, target, start, speedMultiplier, ref at, ref percent);
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
                sfx.Stop();
                StopPlayerRunIntoAnimation = false;
                streetlight.SetAnimationFrame(1);
                firstDirection = !firstDirection;
            }
        }
    }
}
