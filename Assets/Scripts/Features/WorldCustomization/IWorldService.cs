using System;
using System.Threading;
using System.Threading.Tasks;

namespace BlockBlast.Features.WorldCustomization
{
    public interface IWorldService
    {
        string SelectedWorldId { get; }
        event Action<WorldDefinition> SelectedWorldChanged;

        bool TryGetSelectedWorld(out WorldDefinition definition);
        bool TryGetWorld(string worldId, out WorldDefinition definition);
        Task<WorldChangeResult> ChangeWorldAsync(
            string worldId,
            CancellationToken cancellationToken = default);
    }
}
