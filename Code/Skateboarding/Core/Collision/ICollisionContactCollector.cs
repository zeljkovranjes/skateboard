namespace Skateboarding.Core.Collision;

internal interface ICollisionContactCollector
{
	Span<uint> AllocateContact();
	void CompleteContact();
}
