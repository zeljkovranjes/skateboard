using System;
using System.Collections.Generic;
using Sandbox;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The community portraits on the underside of the board in the board editor: the portrait layer (one model per portrait)
/// put on the deck's copy, each portrait its own object, so the one under the mouse grows and lifts off the deck, and
/// the one clicked pops up, a little larger again, with an outline round it. Each portrait's size follows a spring, so it
/// grows, pops and settles without jumping. A portrait is found under the mouse by the ray through it meeting its box,
/// from below the deck only: seen through the deck from above, none is. The portraits face the board's down, which the
/// board's parts are laid out by (+Z up, the model's placement taking it there), whatever way the model was authored.
/// </summary>
internal sealed class BoardEditorPortraits
{
	// Sizes the portraits are drawn at, at rest, under the mouse and picked.
	private const float HoverScale = 1.35f;
	private const float PickedScale = 1.5f;
	// The spring each portrait's size follows (per second squared and per second): a little bouncy, settled in about a
	// third of a second.
	private const float Stiffness = 320f;
	private const float Damping = 19f;
	// Speed a pick starts the spring with, for its pop (per second).
	private const float PickKick = 9f;
	// Inches a portrait lifts off the deck for each unit it has grown, and the whole layer's lift off the deck's own
	// printed portraits, so the two never draw over each other.
	private const float LiftPerScale = 0.6f;
	private const float LayerLift = 0.02f;

	private readonly List<GameObject> _tiles = new();
	private readonly List<string> _ids = new();
	private readonly List<BBox> _boxes = new();
	private readonly List<float> _scales = new();
	private readonly List<float> _speeds = new();
	private GameObject _layer;
	// The portraits' holder on the deck's copy, in the board model's own space (rays are taken into it to pick).
	private GameObject _deck;
	// The way the portraits face, off the deck, in the deck model's space.
	private Vector3 _outward = Vector3.Down;
	private int _hovered = -1;
	private int _picked = -1;

	/// <summary>How many portraits are on the board.</summary>
	internal int Count => _tiles.Count;

	/// <summary>The portrait id under the mouse, or null.</summary>
	internal string Hovered => _hovered >= 0 ? _ids[ _hovered ] : null;

	/// <summary>
	/// Puts the portrait layer on <paramref name="deck"/> (the deck's copy), in place of any earlier one. The layer is
	/// made in the board model's own space, which <paramref name="placement"/> takes into the deck's, and
	/// <paramref name="upright"/> turns that space to the board's (+Z up). Nothing without a deck or without the portrait
	/// layer in the game.
	/// </summary>
	internal void Attach( Scene scene, GameObject deck, Transform placement, Rotation upright )
	{
		Clear();
		var prefab = deck.IsValid() ? ResourceLibrary.Get<PrefabFile>( PortraitDirectory.PrefabPath ) : null;
		if ( prefab is null )
			return;
		using ( scene.Push() )
		{
			_deck = new GameObject( deck, true, "Portraits" ) { LocalTransform = placement };
			_layer = SceneUtility.GetPrefabScene( prefab ).Clone( global::Transform.Zero );
			_layer.Parent = _deck;
			_layer.LocalTransform = global::Transform.Zero;
		}
		foreach ( var tile in _layer.Children )
		{
			var renderer = tile.Components.Get<ModelRenderer>();
			if ( renderer?.Model is null )
				continue;
			_tiles.Add( tile );
			_ids.Add( tile.Name );
			_boxes.Add( renderer.Model.Bounds );
			_scales.Add( 1f );
			_speeds.Add( 0f );
		}
		_outward = (upright.Inverse * Vector3.Down).Normal;
		_layer.LocalPosition = _outward * LayerLift;
	}

	/// <summary>
	/// The portrait the ray (in the editor's scene) meets first from below the deck, or -1. Its box is taken where it
	/// sits at rest, so a portrait does not slip from under the mouse as it grows.
	/// </summary>
	internal int Pick( Ray ray )
	{
		if ( _tiles.Count == 0 || !_deck.IsValid() )
			return -1;
		var deck = _deck.WorldTransform;
		var from = deck.PointToLocal( ray.Position ) - _outward * LayerLift;
		var way = deck.NormalToLocal( ray.Forward );
		// The portraits face off the deck: seen through it from its other face, none is under the mouse.
		if ( Vector3.Dot( way, _outward ) >= -0.0001f )
			return -1;
		var best = -1;
		var nearest = float.MaxValue;
		for ( var tile = 0; tile < _tiles.Count; tile++ )
		{
			if ( Meets( _boxes[ tile ], from, way, out var distance ) && distance < nearest )
			{
				nearest = distance;
				best = tile;
			}
		}
		return best;
	}

	/// <summary>The portrait under the mouse grows (none with -1).</summary>
	internal void Hover( int tile ) => _hovered = tile;

	/// <summary>Picks a portrait (none with -1): it pops up, outlined, and the last one picked settles back.</summary>
	internal string Choose( int tile )
	{
		if ( tile == _picked )
			return tile >= 0 ? _ids[ tile ] : null;
		Outline( _picked, false );
		_picked = tile;
		if ( tile < 0 )
			return null;
		Outline( tile, true );
		_speeds[ tile ] += PickKick;
		return _ids[ tile ];
	}

	/// <summary>Moves each portrait's size along its spring by <paramref name="delta"/> seconds; one at rest is left be.</summary>
	internal void Animate( float delta )
	{
		if ( !(delta > 0f) )
			return;
		delta = MathF.Min( delta, 1f / 30f );
		for ( var tile = 0; tile < _tiles.Count; tile++ )
		{
			var target = tile == _picked ? PickedScale : tile == _hovered ? HoverScale : 1f;
			var scale = _scales[ tile ];
			var speed = _speeds[ tile ];
			if ( target == 1f && scale == 1f && speed == 0f )
				continue;
			speed += ((target - scale) * Stiffness - speed * Damping) * delta;
			scale += speed * delta;
			if ( target == 1f && MathF.Abs( scale - 1f ) < 0.001f && MathF.Abs( speed ) < 0.01f )
			{
				scale = 1f;
				speed = 0f;
			}
			_scales[ tile ] = scale;
			_speeds[ tile ] = speed;
			Place( tile, scale );
		}
	}

	// A portrait drawn at a size, grown about its own middle and lifted off the deck as it grows.
	private void Place( int tile, float scale )
	{
		var piece = _tiles[ tile ];
		if ( !piece.IsValid() )
			return;
		var middle = _boxes[ tile ].Center;
		piece.LocalScale = scale;
		piece.LocalPosition = middle * (1f - scale) + _outward * ((scale - 1f) * LiftPerScale);
	}

	private void Outline( int tile, bool on )
	{
		if ( tile < 0 || tile >= _tiles.Count || !_tiles[ tile ].IsValid() )
			return;
		var outline = _tiles[ tile ].Components.Get<HighlightOutline>( FindMode.EverythingInSelf );
		if ( outline is null )
			return;
		outline.Color = Color.White;
		outline.Enabled = on;
	}

	private void Clear()
	{
		if ( _deck.IsValid() )
			_deck.Destroy();
		else if ( _layer.IsValid() )
			_layer.Destroy();
		_layer = null;
		_deck = null;
		_tiles.Clear();
		_ids.Clear();
		_boxes.Clear();
		_scales.Clear();
		_speeds.Clear();
		_hovered = -1;
		_picked = -1;
	}

	// Whether the ray meets the box, and how far along it.
	private static bool Meets( BBox box, Vector3 from, Vector3 way, out float distance )
	{
		var near = 0f;
		var far = float.MaxValue;
		for ( var axis = 0; axis < 3; axis++ )
		{
			var start = from[ axis ];
			var step = way[ axis ];
			var low = box.Mins[ axis ];
			var high = box.Maxs[ axis ];
			if ( MathF.Abs( step ) < 1e-6f )
			{
				if ( start < low || start > high )
				{
					distance = 0f;
					return false;
				}
				continue;
			}
			var a = (low - start) / step;
			var b = (high - start) / step;
			near = MathF.Max( near, MathF.Min( a, b ) );
			far = MathF.Min( far, MathF.Max( a, b ) );
			if ( near > far )
			{
				distance = 0f;
				return false;
			}
		}
		distance = near;
		return true;
	}
}
