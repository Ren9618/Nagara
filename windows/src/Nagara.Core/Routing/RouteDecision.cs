using Nagara.Core.Media;

namespace Nagara.Core.Routing;

public sealed record RouteDecision(MediaSessionSnapshot? Session, RouteReason Reason)
{
    public static RouteDecision None { get; } = new(null, RouteReason.None);

    public bool HasTarget => Session is not null;
}
