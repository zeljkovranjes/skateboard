using Skateboarding.Core.Input;

namespace Skateboarding.Core.Animation;

internal readonly record struct AnimationEntryAction(
	uint Kind, InputCommandKey Source, AnimationAttributeKey Attribute,
	AnimationAttributeKey Target, float Fallback, uint Flags );
