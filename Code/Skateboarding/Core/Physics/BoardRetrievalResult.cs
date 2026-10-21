namespace Skateboarding.Core.Physics;

using Skateboarding.Core.Animation;

/// <summary>Retrieval clock, completion progress and the physical frame to apply in order.</summary>
internal readonly record struct BoardRetrievalResult(
	float Elapsed, float Progress, float Weight, LimbRigidTransform Frame );
