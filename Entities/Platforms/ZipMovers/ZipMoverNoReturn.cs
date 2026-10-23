// SPDX-License-Identifier: MIT

using System.Collections.Generic;
using System.Numerics;
using Celeste.Core.Components.Timing;
using Celeste.Core.Display;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Rendering;
using Celeste.Core.Scheduling;
using Celeste.Everest.Gameplay.Visibility;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Content.Maps.Entities;
using Celeste.Game.Content.Maps.Levels;
using Celeste.Game.Execution.Levels;
using Celeste.Game.Presentation.Platforms.ZipMovers;
using Celeste.Graphics.Animation;
using Celeste.Graphics.Content;
using Celeste.Graphics.Content.Atlas;
using Celeste.Scene.Tracking;
using Celeste.Simulation.Audio.Sources;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Simulation.Presentation.Lighting;
using Celeste.Simulation.Presentation.PostProcessing.Bloom;

namespace Celeste.Mods.AdventureHelper.Entities.Platforms.ZipMovers
{
    [CustomEntity("AdventureHelper/ZipMoverNoReturn")]
    [RuntimeTypeAlias("Celeste.Mod.AdventureHelper.Entities.ZipMoverNoReturn")]
    public partial class ZipMoverNoReturn : SourceSolidEntityState
    {
        public const string defaultPath = "objects/AdventureHelper/noreturnzipmover";
        private static readonly PackedColor ropeColor = new PackedColor(209, 209, 209);
        private static readonly PackedColor ropeLightColor = new PackedColor(158, 158, 158);
        private readonly ISourceEngineContext engine;
        private readonly string spritePath;
        private readonly float speedMultiplier;
        private readonly AtlasEntry[,] edges = new AtlasEntry[3, 3];
        private readonly Sprite streetlight;
        private readonly BloomPointState bloom;
        private readonly SoundSourceComponentState sfx;
        private readonly List<AtlasEntry> innerCogs;
        private readonly AtlasEntry temp = new AtlasEntry();
        private ZipMoverPathRenderer pathRenderer;
        private Vector2 start;
        private Vector2 target;
        private float percent;
        private bool firstDirection;

        public ZipMoverNoReturn(ISourceEngineContext engine, Vector2 position, int width, int height,
            Vector2 target, float speedMultiplier, string spritePath, ISourceSolidWorld world)
            : base(position, width, height, false, world)
        {
            spritePath.Trim('/');
            spritePath.Trim('\\');
            if (spritePath == string.Empty) spritePath = defaultPath;
            this.engine = engine;
            this.spritePath = spritePath;
            this.speedMultiplier = speedMultiplier;
            var atlas = ((ISourceGameGraphicsEnvironment)engine).GameGraphics.GameAtlas;
            var frames = atlas.GetAtlasSubtextures(spritePath + "/innercog");
            innerCogs = frames.Count > 0 ? frames : atlas.GetAtlasSubtextures(defaultPath + "/innercog");
            percent = 0f;
            sfx = new SoundSourceComponentState(engine);
            Depth = -9999;
            start = Position;
            this.target = target;
            firstDirection = true;
            Add(new SimulationCoroutine(engine, Sequence()));
            Add(new LightOccludeState(1f));
            var light = atlas.GetAtlasSubtexturesAt(spritePath + "/light", 0);
            Add(streetlight = new Sprite(engine, atlas,
                light?.AtlasPath is not ("__fallback" or null) ? spritePath + "/light" : "objects/zipmover/light"));
            streetlight.Add("frames", "", 1f);
            streetlight.Play("frames");
            streetlight.Active = false;
            streetlight.SetAnimationFrame(1);
            streetlight.Position =
                new Vector2(Width / 2f - streetlight.Width / 2f, Height / 2f - streetlight.Height / 2f);
            Add(bloom = new BloomPointState(1f, 6f));
            bloom.Position = new Vector2(Width / 2f, Height / 2f - streetlight.Height / 2f + 3f);
            string path = atlas.Has(spritePath + "/block") ? spritePath : defaultPath;
            for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                edges[i, j] = atlas[path + "/block"].GetSubtexture(i * 8, j * 8, 8, 8);
            SurfaceSoundIndex = 7;
            sfx.Position = new Vector2(Width, Height) / 2f;
            Add(sfx);
        }

        public ZipMoverNoReturn(ISourceEngineContext engine, EntityData data, Vector2 offset, ISourceSolidWorld world)
            : this(engine, data.Position + offset, data.Width, data.Height, data.Nodes[0] + offset,
                data.Float("speedMultiplier", 1f), data.Attr("spritePath", defaultPath), world)
        {
        }

        public static ZipMoverNoReturn Load(OriginalLevelSceneRoot level, OriginalLevelRoomData room,
            Vector2 offset, OriginalLevelEntityData data) =>
            new ZipMoverNoReturn(level.EngineContext, data, offset,
                level.SolidWorldFactory.Create(data.Position + offset, false));

        public override void Added(object scene)
        {
            base.Added(scene);
            ((ISourceScene)scene).SourceScene.QueueSourceEntity(pathRenderer =
                new ZipMoverPathRenderer(this, spritePath));
        }

        public override void Removed(object scene)
        {
            ((ISourceScene)scene).SourceScene.RemoveEntity(pathRenderer);
            pathRenderer = null;
            base.Removed(scene);
        }

        public override void Update()
        {
            base.Update();
            bloom.Y = Height / 2f - streetlight.Height / 2f + streetlight.CurrentAnimationFrame * 3;
        }

        public override void Render(ISourceRenderContext context)
        {
            if (!EverestCullHelper.IsRectangleVisible(engine, X, Y, Width, Height)) return;
            var position = Position;
            Position += Shake;
            ZipMoverDrawing.Body(context, this, percent, innerCogs, edges, temp, 0f);
            base.Render(context);
            Position = position;
        }
    }
}
