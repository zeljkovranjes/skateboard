namespace Skateboarding.Core.Constraints;

using Quaternion = System.Numerics.Quaternion;

internal readonly record struct QuaternionJointGeometry(
	Quaternion LocalFrameA, Quaternion LocalFrameB, bool LockTwist, float MaximumTwistSpeed );
