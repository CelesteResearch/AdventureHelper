// Copyright (c) Communal Helper Team. Licensed under the MIT License.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Celeste.Core.Collections;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Audio.Surfaces;
using Celeste.Game.Content.Maps;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Controls.Source;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Execution.Processes;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Tiles;
using Celeste.Graphics.Content.Tiles.Autotiling;
using Celeste.Physics.Collision;
using Celeste.Scene.Entities;
using Celeste.Scene.Tracking;
using Celeste.Scene.Tracking.Attributes;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Gameplay.Platforms.StaticMovers;
using Celeste.Simulation.Gameplay.Tiles;
using Celeste.Simulation.Presentation.Lighting;

namespace Celeste.Mods.AdventureHelper.Entities.Blocks.Falling
{
    public partial class GroupedFallingBlock : SourceSolidEntityState
    {
        private IEnumerator Sequence()
        {
            while (!Triggered && !PlayerFallCheck())
            {
                yield return null;
            }

            while (FallDelay > 0f)
            {
                FallDelay -= engine.DeltaTime;
                yield return null;
            }

            HasStartedFalling = true;
            List<GroupedFallingBlock> hitters = null;
            while (true)
            {
                ShakeSfx();
                StartShaking();
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Medium,
                    RumbleLength.Medium);
                yield return 0.2f;
                float timer = 0.4f;
                while (timer > 0f && PlayerWaitCheck())
                {
                    yield return null;
                    timer -= engine.DeltaTime;
                }

                StopShaking();
                foreach (GroupedFallingBlock block in Group)
                {
                    for (int i = 2; i < Width; i += 4)
                    {
                        if (block.CollideCheck("Celeste.Solid", block.TopLeft + new Vector2(i, -2f)))
                        {
                            Level.Particles.Emit(Process.ParticleTypes.FallingBlock.P_FallDustA, 2,
                                new Vector2(block.X + i, block.Y), Vector2.One * 4f, (float)Math.PI / 2f);
                        }

                        Level.Particles.Emit(Process.ParticleTypes.FallingBlock.P_FallDustB, 2,
                            new Vector2(block.X + i, block.Y), Vector2.One * 4f);
                    }
                }

                float speed = 0f;
                float maxSpeed = 160f;
                while (true)
                {
                    OriginalLevelSceneRoot level = Level;
                    speed = ScalarMath.Approach(speed, maxSpeed, 500f * engine.DeltaTime);

                    bool breakCollideCheck = false;
                    hitters ??= new List<GroupedFallingBlock>(Group.Count);
                    hitters.Clear();
                    foreach (GroupedFallingBlock block in Group)
                    {
                        if (block.MoveVCollideSolids(speed * engine.DeltaTime, thruDashBlocks: true))
                        {
                            breakCollideCheck = true;
                            hitters.Add(block);
                        }
                    }

                    foreach (SourceJumpThruEntityState jp in Jumpthrus)
                    {
                        jp.MoveV(speed * engine.DeltaTime);
                    }

                    if (breakCollideCheck)
                    {
                        foreach (GroupedFallingBlock block in Group)
                        {
                            if (!hitters.Contains(block))
                            {
                                block.MoveV(-speed * engine.DeltaTime);
                            }
                        }

                        foreach (SourceJumpThruEntityState jp in Jumpthrus)
                        {
                            jp.MoveV(-speed * engine.DeltaTime);
                        }

                        break;
                    }

                    if (GroupBoundsMin.Y > (float)(level.Bounds.Bottom + 16) ||
                        (GroupBoundsMin.Y > (float)(level.Bounds.Bottom - 1) &&
                         CollideCheck("Celeste.Solid", GroupPosition + new Vector2(0f, 1f))))
                    {
                        Collidable = Visible = false;
                        yield return 0.2f;
                        if (((OriginalChapterMapData)level.Session.MapData).CanTransitionTo(level,
                                new Vector2(Center.X, Bottom + 12f)))
                        {
                            yield return 0.2f;
                            Level.Shake();
                            ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                                RumbleLength.Medium);
                        }

                        foreach (GroupedFallingBlock block in Group)
                        {
                            block.RemoveSelf();
                            block.DestroyStaticMovers();
                        }

                        foreach (SourceJumpThruEntityState jp in Jumpthrus)
                        {
                            jp.RemoveSelf();
                            jp.DestroyStaticMovers();
                        }

                        yield break;
                    }

                    yield return null;
                }

                ImpactSfx();
                ((IOriginalGameInputProcessEnvironment)engine).InputSource.Rumble(RumbleStrength.Strong,
                    RumbleLength.Medium);
                Level.DirectionalShake(Vector2.UnitY, 0.3f);
                StartShaking();
                LandParticles();
                yield return 0.2f;
                StopShaking();

                bool collideSolidTiles = false;
                foreach (GroupedFallingBlock block in Group)
                {
                    if (block.CollideCheck("Celeste.SolidTiles", block.Position + new Vector2(0f, 1f)))
                    {
                        collideSolidTiles = true;
                        break;
                    }
                }

                if (collideSolidTiles)
                {
                    break;
                }

                bool collidePlatforms;
                do
                {
                    collidePlatforms = false;
                    foreach (GroupedFallingBlock block in Group)
                    {
                        foreach (SourcePlatformEntityState platform in Registry.GetTrackedEntities("Celeste.Platform"))
                        {
                            if (platform is GroupedFallingBlock && Group.Contains(platform as GroupedFallingBlock))
                            {
                                continue;
                            }

                            if (SourceEntityCollision.CheckAt(block, platform, block.Position + new Vector2(0f, 1f)))
                            {
                                collidePlatforms = true;
                                break;
                            }
                        }

                        if (collidePlatforms)
                        {
                            break;
                        }
                    }

                    if (collidePlatforms)
                    {
                        yield return 0.1f;
                    }
                } while (collidePlatforms);
            }

            Safe = true;
        }
    }
}
