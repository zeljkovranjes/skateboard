namespace Skateboarding.Engine.Collision;

using Skateboarding.Core.Animation;
using Skateboarding.Core.Collision;

internal sealed partial class MapCollisionSource
{
	/// <summary>Refreshes the selected map source and issues ordinary ground queries in query-space metres.</summary>
	internal void ProcessGroundQueries( PhysicalGroundMeshQueryBatch batch, Span<uint> owner,
		ReadOnlySpan<uint> frame, TrajectoryWorldQuery trajectory )
	{
		ArgumentNullException.ThrowIfNull( batch );
		UpdateTransforms();
		batch.Issue( owner, frame, _query, trajectory, _instances );
	}
}
