// SPDX-License-Identifier: MIT

using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Runtime;

namespace Celeste.Mods.AdventureHelper.Modules
{
    public sealed partial class AdventureHelperModule : SourceEverestModule
    {
        public AdventureHelperModule(ISourceEngineContext engine) : base(engine)
        {
        }

        // This subset owns attributed entity factories; the Everest loader owns their registration leases.
        public override void Load()
        {
            LoadLinkedZipMovers();
        }

        public override void Unload()
        {
            UnloadLinkedZipMovers();
        }
    }
}
