// SPDX-License-Identifier: MIT

using System;
using Celeste.Simulation.Gameplay.Platforms.Foundation;
using Celeste.Mods.AdventureHelper.Entities.Platforms.ZipMovers;

namespace Celeste.Mods.AdventureHelper.Modules
{
    public sealed partial class AdventureHelperModule
    {
        public LinkedZipMoverSounds LinkedZipSounds { get; private set; }

        public event Func<SourceSolidEntityState, string, bool> AdditionalRiderCheck
        {
            add => LinkedZipMoverSounds.For(SourceEngine).AdditionalRiderCheck += value;
            remove => LinkedZipMoverSounds.For(SourceEngine).AdditionalRiderCheck -= value;
        }

        public event Action<SourceSolidEntityState, string> BeforeStopSound
        {
            add => LinkedZipMoverSounds.For(SourceEngine).BeforeStopSound += value;
            remove => LinkedZipMoverSounds.For(SourceEngine).BeforeStopSound -= value;
        }

        private void LoadLinkedZipMovers() => LinkedZipSounds = LinkedZipMoverSounds.For(SourceEngine);

        private void UnloadLinkedZipMovers()
        {
            LinkedZipSounds?.Dispose();
            LinkedZipSounds = null;
        }
    }
}
