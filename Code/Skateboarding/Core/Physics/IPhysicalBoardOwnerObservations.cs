namespace Skateboarding.Core.Physics;

using Skateboarding.Core.Animation;

/// <summary>Reads current owner data at the phase boundary where each consumer observes it.</summary>
internal interface IPhysicalBoardOwnerObservations
{
	bool BoardSessionEnabled { get; }
	PhysicalBoardInput ReadBoardInput();
	PhysicalBoardPoses ReadBoardPoses();
	bool SuppressQueries { get; }
	PhysicalBoardProbeInput ReadProbeInput();
	int ExcludedActor { get; }
	float DeltaTime { get; }
}
