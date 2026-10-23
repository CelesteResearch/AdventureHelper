using Celeste.Core.Components;
using Celeste.Graphics.Rendering;
// SPDX-License-Identifier: MIT
using System;
using System.Numerics;
using Celeste.Core.Display;
using Celeste.Core.Geometry;
using Celeste.Core.Mathematics;
using Celeste.Core.Mathematics.LinearAlgebra;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Visibility;
using Celeste.Graphics.Content;
using Celeste.Game.Execution.Processes;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Content.Atlas;
using Celeste.Graphics.Rendering.Drawing;
using Celeste.Scene.Entities;
using Celeste.Simulation.Gameplay.Platforms.Foundation;

namespace Celeste.Mods.AdventureHelper.Entities.Platforms.ZipMovers
{
    public partial class ZipMoverNoReturn
    {
        private sealed class ZipMoverPathRenderer : SceneEntityState
        {
            private readonly ISourceEngineContext engine;
            private readonly ZipMoverNoReturn targetZip;

            private readonly Vector2 from;
            private readonly Vector2 to;
            private readonly Vector2 sparkAdd;
            private readonly float sparkDirFromA;
            private readonly float sparkDirFromB;
            private readonly float sparkDirToA;
            private readonly float sparkDirToB;
            private readonly AtlasEntry cog;


            public ZipMoverPathRenderer(ZipMoverNoReturn targetZip, string spritePath)
                : base(((ISourceEntitySourceDataEnvironment)targetZip.engine).EntitySourceDataRegistration)
            {
                var engine = targetZip.engine;
                var start = targetZip.start;
                var target = targetZip.target;
                this.engine = engine;
                this.targetZip = targetZip;


                Depth = 5000;
                var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
                cog = atlas.Has(spritePath + "/cog")
                    ? atlas[spritePath + "/cog"]
                    : atlas[defaultPath + "/cog"];
                from = SourceVector2Math.Add(start, new Vector2(targetZip.Width / 2f, targetZip.Height / 2f));
                to = SourceVector2Math.Add(target, new Vector2(targetZip.Width / 2f, targetZip.Height / 2f));
                sparkAdd = Vector2Geometry.Perpendicular(
                    ((ISourceVectorEnvironment)engine).VectorArithmetic.SafeNormalize(
                        SourceVector2Math.Subtract(from, to), 5f));
                float angle = Vector2Geometry.Angle(SourceVector2Math.Subtract(from, to));
                sparkDirFromA = angle + .3926991f;
                sparkDirFromB = angle - .3926991f;
                sparkDirToA = angle + 3.14159274f - .3926991f;
                sparkDirToB = angle + 3.14159274f + .3926991f;
            }

            public void CreateSparks() => ZipMoverDrawing.Sparks(this, engine, from, to, sparkAdd,
                sparkDirFromA, sparkDirFromB, sparkDirToA, sparkDirToB);

            public override void Render(ISourceRenderContext context)
            {
                float left, right, top, bottom;
                if (from.X > to.X)
                {
                    left = to.X;
                    right = from.X;
                }
                else
                {
                    left = from.X;
                    right = to.X;
                }

                if (from.Y > to.Y)
                {
                    top = to.Y;
                    bottom = from.Y;
                }
                else
                {
                    top = from.Y;
                    bottom = to.Y;
                }

                if (EverestCullHelper.IsRectangleVisible(engine, left, top, right - left, bottom - top))
                {
                    ZipMoverDrawing.Ropes(context, this, engine, from, to, cog, targetZip.percent, ropeColor,
                        ropeLightColor,
                        Vector2.UnitY, PackedColor.Black);
                    ZipMoverDrawing.Ropes(context, this, engine, from, to, cog, targetZip.percent, ropeColor,
                        ropeLightColor,
                        Vector2.Zero);
                }

                SourcePrimitiveDrawing.Rect((ISourceDrawScope)context,
                    new IntRect((int)(targetZip.X - 1f), (int)(targetZip.Y - 1f), (int)targetZip.Width + 2,
                        (int)targetZip.Height + 2),
                    PackedColor.Black);
            }
        }
    }
}
