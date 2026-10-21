using Skateboarding.Core.Animation;

namespace Skateboarding.Core.Physics;

using Vector3 = System.Numerics.Vector3;

/// <summary>Constructed primitive geometry, both retained mass variants and the body descriptor.</summary>
internal readonly record struct PhysicalRigBodyConstructionResult(
	int Shape, Vector3 HalfExtents, float Radius, float HalfLength, float PoseVolume,
	PhysicalBodyMass Primary, PhysicalBodyMass Alternate, PhysicalBodyMassResult Descriptor );
