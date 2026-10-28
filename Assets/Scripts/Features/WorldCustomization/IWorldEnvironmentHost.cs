using System;
using System.Collections.Generic;

namespace BlockBlast.Features.WorldCustomization
{
    public interface IWorldEnvironmentHost
    {
        WorldEnvironmentSlot Slot { get; }
        IReadOnlyList<IWorldGameplayReactor> ActiveReactors { get; }
        event Action<IWorldEnvironmentHost> InstancesCommitted;

        bool TryStage(
            IReadOnlyList<WorldPrefabAssignment> assignments,
            out string error);
        void CommitStaged();
        void RollbackStaged();
        void ResetOwnedInstance();
    }
}
