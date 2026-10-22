using System;
using Sandbox;
using SalvagedHeart.Core;
using SalvagedHeart.Engine;

namespace SalvagedHeart.Components;

/// <summary>
/// A one-third-full oil bath in the salvaged heart's glass case that sloshes as its board moves: the surface lags the
/// board's turns and acceleration, overshoots a little and settles (<see cref="OilSloshSolver"/>). It goes on an object
/// aligned with the board model (the model's own origin, rotation and unit scale). Cosmetic and per client: nothing is
/// networked and nothing pushes back on the board. A teleport or a long frame stall starts the motion afresh. The oil is
/// drawn under the tags of the object it sits on, so a camera leaving the board out leaves the oil out too. It steps and
/// is placed as the frame is drawn (OnPreRender), after everything has moved the board: a board shown in a trick's pose,
/// set late in the frame by its rider, carries its oil with it instead of leaving it a frame behind.
/// </summary>
public sealed class HeartOilSlosh : Component
{
	// Height of the case's middle above the model's origin (metres): where the oil's motion is read.
	private const float CaseMiddleHeight = 0.0417f;

	// A move longer than this in one frame (metres) is a teleport, and a frame longer than this (seconds) a stall.
	private const float LongestMove = 2f;
	private const float LongestFrame = 0.1f;

	// The acceleration felt is capped (m/s²) and smoothed at this rate (per second).
	private const float HardestAcceleration = 40f;
	private const float AccelerationSmoothing = 12f;

	/// <summary>The opaque, glossy black oil material imported with the board.</summary>
	[Property] public Material OilMaterial { get; set; }

	/// <summary>Share of the case's inside the oil fills.</summary>
	[Property, Range( 0.05f, 0.9f )] public float Fill { get; set; } = 1f / 3f;

	/// <summary>Engine units per metre of the board model (s&amp;box draws in inches).</summary>
	[Property] public float UnitsPerMeter { get; set; } = 39.3700787f;

	private readonly OilSloshSolver _solver = new();
	private readonly OilVolumeMesh _volume = new();
	private OilSloshRenderer _renderer;
	private Material _drawnWith;
	private Vector3 _lastPosition;
	private Vector3 _lastVelocity;
	private Vector3 _acceleration;
	private Rotation _lastRotation;
	private bool _hasPosition;
	private bool _hasVelocity;

	protected override void OnEnabled()
	{
		ResetMotion();
	}

	protected override void OnPreRender()
	{
		if ( OilMaterial is null || !float.IsFinite( UnitsPerMeter ) || UnitsPerMeter <= 0 )
			return;
		// A new material (another colour picked) is drawn with from the next frame.
		if ( _drawnWith != OilMaterial )
		{
			_renderer?.Dispose();
			_renderer = null;
		}
		_renderer ??= new OilSloshRenderer( Scene.SceneWorld, OilMaterial, UnitsPerMeter );
		_drawnWith = OilMaterial;
		var dt = Time.Delta;
		if ( dt <= 0 )
			return;
		if ( dt > LongestFrame )
			ResetMotion();
		var center = WorldPosition + WorldRotation * new Vector3( 0, 0, CaseMiddleHeight * UnitsPerMeter );
		if ( !_hasPosition )
			Remember( center );
		var delta = (center - _lastPosition) / UnitsPerMeter;
		if ( delta.Length > LongestMove )
		{
			ResetMotion();
			Remember( center );
			delta = Vector3.Zero;
		}
		var velocity = delta / MathF.Max( dt, 0.001f );
		if ( _hasVelocity )
		{
			var raw = (velocity - _lastVelocity) / MathF.Max( dt, 0.001f );
			if ( raw.Length > HardestAcceleration )
				raw = raw.Normal * HardestAcceleration;
			_acceleration += (raw - _acceleration) * (1 - MathF.Exp( -AccelerationSmoothing * dt ));
		}
		var frameChange = WorldRotation.Inverse * _lastRotation;
		_solver.RotateFrame( new System.Numerics.Quaternion( frameChange.x, frameChange.y, frameChange.z, frameChange.w ) );
		var gravity = Scene.PhysicsWorld.Gravity / UnitsPerMeter;
		var local = WorldRotation.Inverse * (gravity - _acceleration);
		_solver.Step( new System.Numerics.Vector3( local.x, local.y, local.z ), dt );
		_volume.Build( _solver.Normal, Fill );
		_renderer.Tag( GameObject.Parent?.Tags ?? GameObject.Tags );
		_renderer.Update( _volume, WorldTransform, UnitsPerMeter );
		_lastPosition = center;
		_lastVelocity = velocity;
		_lastRotation = WorldRotation;
		_hasVelocity = true;
	}

	private void Remember( Vector3 center )
	{
		_lastPosition = center;
		_lastRotation = WorldRotation;
		_hasPosition = true;
	}

	private void ResetMotion()
	{
		_hasPosition = false;
		_hasVelocity = false;
		_acceleration = Vector3.Zero;
		_lastVelocity = Vector3.Zero;
		_solver.Reset();
	}

	protected override void OnDisabled()
	{
		_renderer?.Dispose();
		_renderer = null;
		_drawnWith = null;
		ResetMotion();
	}
}
