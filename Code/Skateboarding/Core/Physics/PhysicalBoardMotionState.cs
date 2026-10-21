using Skateboarding.Core.Animation;

namespace Skateboarding.Core.Physics;

/// <summary>Retained source, target, output and clocks shared by board motion updates.</summary>
internal sealed class PhysicalBoardMotionState
{
	internal LimbRigidTransform Source { get; set; } = LimbRigidTransform.Identity;
	internal LimbRigidTransform Target { get; set; } = LimbRigidTransform.Identity;
	internal LimbRigidTransform Output { get; set; } = LimbRigidTransform.Identity;
	internal float Elapsed { get; set; }
	internal float Duration { get; set; }
	internal float Progress { get; set; }
	internal float Weight { get; set; }
}
