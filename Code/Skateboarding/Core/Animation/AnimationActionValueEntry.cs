using Skateboarding.Core.Input;

namespace Skateboarding.Core.Animation;

internal readonly record struct AnimationActionValueEntry( InputCommandKey Key, float Value, int Next );
