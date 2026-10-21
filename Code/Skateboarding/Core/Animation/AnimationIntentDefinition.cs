using Skateboarding.Core.Input;

namespace Skateboarding.Core.Animation;

/// <summary>Copies one present input request to a named scalar output during behavior updates.</summary>
internal readonly record struct AnimationIntentDefinition(
	InputCommandKey Request,
	AnimationAttributeKey Target,
	bool PublishAnimation );
