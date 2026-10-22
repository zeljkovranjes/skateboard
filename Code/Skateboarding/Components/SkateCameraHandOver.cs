using System.Collections.Generic;
using Sandbox;

namespace Skateboarding.Components;

/// <summary>
/// Shows a <see cref="SkateCamera"/>'s follow view through the game's own camera, so riding keeps the look the game
/// gives its camera (its tonemapping, bloom, ambient occlusion, reflections, near and far planes), and eases the view
/// across each hand-over instead of cutting: from where the game's camera was to the follow view as the rider takes
/// over, and from the follow view to wherever the game's camera goes as the player comes back. The skate camera's own
/// camera is switched off and only holds the follow pose. A separate camera of its own showed the world ungraded, and
/// each hand-over cut between the two in a frame (third person to the player's eyes, the sky turning darker and bluer).
/// </summary>
public sealed class SkateCameraHandOver : Component, ICameraModifier
{
	/// <summary>Seconds each hand-over takes.</summary>
	[Property, Range( 0f, 2f )] public float BlendTime { get; set; } = 0.45f;

	// Nearer than this (inches) to where the player's camera goes, the view counts as through the player's eyes. While
	// it is further out, the player's own body, hidden there in first person, is shown (it vanished as the view set off
	// from behind it), and what is drawn only at the eyes (the held weapon's first-person model) is not (it hung in front
	// of the view behind the body, a second weapon beside the one in its hands).
	private const float EyeNear = 20f;
	private static readonly string[] EyesOnly = { "viewmodel", "firstperson" };

	// Seconds the followed rider may be gone before the view is handed back by itself (its board removed without it).
	private const float TargetLost = 1f;

	private CameraComponent _game;
	private CameraComponent _follow;
	private Transform _from;
	private float _fromFov;
	private Transform _shown;
	private float _shownFov;
	private bool _handingBack;
	// Where the view starts each hand-over, as an offset from where it is going (the follow view taking over, the game's
	// own handing back), taken on its first frame and shrinking to nothing: the view starts where it was and goes on with
	// what it eases to. Eased from a place fixed in the world, it stood still for a frame and then caught up as the player
	// got on or was given back walking or running.
	private Vector3 _startOffset;
	private bool _startTaken;
	private List<string> _hidden;
	private TimeSince _since;
	private TimeSince _sinceTarget;

	// After the player's own camera (0), with a vehicle's; a held weapon's (200) reshapes it after.
	int ICameraModifier.CameraOrder => 100;

	/// <summary>Starts showing the follow view through <paramref name="game"/>, eased from where it is now.</summary>
	public void TakeOver( CameraComponent game, CameraComponent follow )
	{
		_game = game;
		_follow = follow;
		_from = game.WorldTransform;
		_fromFov = game.FieldOfView;
		_shown = _from;
		_shownFov = _fromFov;
		_handingBack = false;
		_startTaken = false;
		_since = 0f;
		_sinceTarget = 0f;
		// Getting straight back on, the view is still easing back: this one eases on from where that left it (the game's
		// camera is placed there), so that one goes.
		foreach ( var other in Scene.GetAllComponents<SkateCameraHandOver>() )
		{
			if ( other != this )
				other.GameObject.Destroy();
		}
	}

	/// <summary>Eases the view from where it is shown to wherever the game's camera goes now, then removes the skate
	/// camera.</summary>
	public void HandBack()
	{
		if ( _handingBack )
			return;
		_handingBack = true;
		_startTaken = false;
		_since = 0f;
		if ( Components.Get<SkateCamera>() is { } follow )
			follow.Enabled = false;
	}

	protected override void OnUpdate()
	{
		// The game's camera gone or switched off, the view goes through the one shown now; with none, the skate camera
		// shows the follow view itself, as it did before there was one to show it through, and handing back there is
		// nothing left to show.
		if ( !_game.IsValid() || !_game.Enabled )
			_game = Scene.Camera is { } main && main != _follow ? main : null;
		if ( !_game.IsValid() )
		{
			if ( _handingBack )
				GameObject.Destroy();
			else if ( _follow.IsValid() )
			{
				_follow.Enabled = true;
				Destroy();
			}
			return;
		}
		if ( Components.Get<SkateCamera>() is { } follow && follow.Target.IsValid() )
			_sinceTarget = 0f;
		else if ( _sinceTarget > TargetLost )
			HandBack();
	}

	void ICameraModifier.ModifyCamera( CameraComponent camera, ref CameraView view )
	{
		if ( camera != _game || !_follow.IsValid() )
			return;
		var weight = BlendTime > 0f ? Smoothstep( _since / BlendTime ) : 1f;
		if ( _handingBack )
		{
			// The game's camera has placed itself by now (the player's eyes, or its boom behind them): the view eases there
			// from where the skate camera left it.
			var eye = view.Position;
			if ( !_startTaken )
			{
				_startOffset = _shown.Position - eye;
				_startTaken = true;
			}
			view.Position = eye + _startOffset * (1f - weight);
			view.Rotation = Rotation.Slerp( _shown.Rotation, view.Rotation, weight );
			view.FieldOfView = MathX.Lerp( _shownFov, view.FieldOfView, weight );
			ShowEyes( camera, view.Position.Distance( eye ) <= EyeNear || weight >= 1f );
		}
		else
		{
			// Taking over, the start goes on with the rider as it is shown (the follow view may not have placed itself yet).
			var anchor = Followed();
			if ( !_startTaken )
			{
				_startOffset = _from.Position - anchor;
				_startTaken = true;
			}
			view.Position = Vector3.Lerp( anchor + _startOffset, _follow.WorldPosition, weight );
			view.Rotation = Rotation.Slerp( _from.Rotation, _follow.WorldRotation, weight );
			view.FieldOfView = MathX.Lerp( _fromFov, _follow.FieldOfView, weight );
			_shown = new Transform( view.Position, view.Rotation );
			_shownFov = view.FieldOfView;
		}
		// What reads the camera's place (walking relative to the view, sound) reads the view shown.
		camera.WorldPosition = view.Position;
		camera.WorldRotation = view.Rotation;
		if ( _handingBack && weight >= 1f )
			GameObject.Destroy();
	}

	// Through the eyes, the body is hidden and the first-person models shown, as the game's camera has them; further out,
	// the other way round. Only exclusions added here are taken off again.
	private void ShowEyes( CameraComponent camera, bool atEyes )
	{
		if ( atEyes )
			camera.RenderExcludeTags.Add( "viewer" );
		else
			camera.RenderExcludeTags.Remove( "viewer" );
		if ( atEyes )
		{
			RestoreEyes( camera );
			return;
		}
		_hidden ??= new();
		foreach ( var tag in EyesOnly )
		{
			if ( camera.RenderExcludeTags.Has( tag ) )
				continue;
			camera.RenderExcludeTags.Add( tag );
			_hidden.Add( tag );
		}
	}

	private void RestoreEyes( CameraComponent camera )
	{
		if ( _hidden is null || _hidden.Count == 0 )
			return;
		if ( camera.IsValid() )
		{
			foreach ( var tag in _hidden )
				camera.RenderExcludeTags.Remove( tag );
		}
		_hidden.Clear();
	}

	// Gone mid-way (got straight back on, the scene unloaded), the first-person models are not left hidden.
	protected override void OnDestroy() => RestoreEyes( _game );

	// Where the followed rider is shown (the followed object's place, without a rider).
	private Vector3 Followed()
	{
		var target = Components.Get<SkateCamera>()?.Target;
		if ( !target.IsValid() )
			return _follow.WorldPosition;
		var rider = target.Components.Get<SkateRider>();
		return rider.IsValid() ? rider.ShownTransform.Position : target.WorldPosition;
	}

	private static float Smoothstep( float t )
	{
		t = MathX.Clamp( t, 0f, 1f );
		return t * t * (3f - 2f * t);
	}
}
