using Skateboarding.Core.Input;

namespace Skateboarding.Core.Animation;

/// <summary>Explicit channel and motion identities supplied by initialized behavior data.</summary>
internal readonly record struct AnimationFakieHeadDefinition( InputCommandKey Channel, InputCommandKey Motion );
