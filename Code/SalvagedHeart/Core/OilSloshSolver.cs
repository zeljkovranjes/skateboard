using System;

namespace SalvagedHeart.Core;

using Vector3 = System.Numerics.Vector3;
using Quaternion = System.Numerics.Quaternion;

/// <summary>
/// The oil's surface as a damped angular spring: its normal is pulled toward the felt gravity (gravity less the case's
/// acceleration), lagging, overshooting a little and settling, as thick oil does. Positions are in the case's own frame.
/// </summary>
internal sealed class OilSloshSolver
{
	// Longest step taken at once (seconds) and steps per second within it.
	private const float LongestStep = 0.05f;
	private const float StepsPerSecond = 120f;

	// Felt gravity weaker than this (squared, in m/s²) leaves the surface where it is.
	private const float LeastGravitySquared = 0.04f;

	// The spring's pull toward level and its damping: a restrained overshoot, then a slow return.
	private const float Stiffness = 64f;
	private const float Damping = 8f;

	// Fastest the surface turns (radians per second).
	private const float FastestTurn = 14f;

	private Vector3 _angularVelocity;

	/// <summary>The surface's upward normal, in the case's frame.</summary>
	public Vector3 Normal { get; private set; } = Vector3.UnitZ;

	public void Reset()
	{
		Normal = Vector3.UnitZ;
		_angularVelocity = Vector3.Zero;
	}

	/// <summary>Carries the surface over a turn of the case's frame, so it stays level in the world as the case turns.</summary>
	public void RotateFrame( Quaternion oldLocalToNewLocal )
	{
		Normal = Vector3.Normalize( Vector3.Transform( Normal, oldLocalToNewLocal ) );
		_angularVelocity = Vector3.Transform( _angularVelocity, oldLocalToNewLocal );
	}

	/// <summary>Moves the surface toward the felt gravity (in the case's frame, m/s²) over <paramref name="deltaTime"/>.</summary>
	public void Step( Vector3 effectiveGravity, float deltaTime )
	{
		if ( deltaTime <= 0 || !float.IsFinite( deltaTime ) )
			return;
		var duration = MathF.Min( deltaTime, LongestStep );
		var steps = Math.Max( 1, (int)MathF.Ceiling( duration * StepsPerSecond ) );
		var dt = duration / steps;
		var target = effectiveGravity.LengthSquared() > LeastGravitySquared
			? -Vector3.Normalize( effectiveGravity )
			: Normal;
		for ( var i = 0; i < steps; i++ )
		{
			var axis = Vector3.Cross( Normal, target );
			var dot = Math.Clamp( Vector3.Dot( Normal, target ), -1, 1 );
			var angle = MathF.Acos( dot );
			if ( axis.LengthSquared() < 1e-8f && dot < 0 )
				axis = Vector3.Cross( Normal, MathF.Abs( Normal.X ) < 0.8f ? Vector3.UnitX : Vector3.UnitY );
			if ( axis.LengthSquared() > 1e-8f )
				axis = Vector3.Normalize( axis );
			_angularVelocity += (axis * angle * Stiffness - _angularVelocity * Damping) * dt;
			_angularVelocity -= Normal * Vector3.Dot( _angularVelocity, Normal );
			var speed = _angularVelocity.Length();
			if ( speed > FastestTurn )
				_angularVelocity *= FastestTurn / speed;
			Normal = Vector3.Normalize( Normal + Vector3.Cross( _angularVelocity, Normal ) * dt );
		}
	}
}
