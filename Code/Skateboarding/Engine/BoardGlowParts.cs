using System;
using System.Collections.Generic;
using Sandbox;

namespace Skateboarding.Engine;

/// <summary>
/// One board's glowing parts: each material slot of its drawn models that uses one of the glowing blue materials draws
/// with the shared copy (<see cref="BoardGlowMaterials"/>) for its place along the board, tail to nose. The parts are
/// only looked for again when the board's drawn models change.
/// </summary>
internal sealed class BoardGlowParts
{
	private readonly List<ModelRenderer> _renderers = new();

	/// <summary>
	/// Gives the board's glowing parts their shared copies, unless its drawn models are the ones they were given to last
	/// time. <paramref name="glowing"/> are the file names of the materials that glow.
	/// </summary>
	internal void Find( GameObject board, string[] glowing, BoardGlowMaterials shared )
	{
		var renderers = board.Components.GetAll<ModelRenderer>( FindMode.EnabledInSelfAndDescendants );
		if ( Same( renderers ) )
			return;
		_renderers.Clear();
		_renderers.AddRange( renderers );
		// The board's length along its forward, over everything drawn of it.
		var forward = board.WorldRotation.Forward;
		var tail = float.MaxValue;
		var nose = float.MinValue;
		foreach ( var renderer in _renderers )
		{
			if ( renderer.Model is null )
				continue;
			foreach ( var corner in renderer.Model.Bounds.Transform( renderer.WorldTransform ).Corners )
			{
				var along = Vector3.Dot( corner, forward );
				tail = MathF.Min( tail, along );
				nose = MathF.Max( nose, along );
			}
		}
		var length = MathF.Max( nose - tail, 0.001f );
		foreach ( var renderer in _renderers )
		{
			if ( renderer.Model is null )
				continue;
			var centre = renderer.Model.Bounds.Transform( renderer.WorldTransform ).Center;
			var place = BoardGlowCycle.Place( (Vector3.Dot( centre, forward ) - tail) / length );
			var materials = renderer.Materials;
			for ( var slot = 0; slot < materials.Count; slot++ )
			{
				var original = materials.GetOriginal( slot );
				if ( original is null || !Glows( original.Name, glowing ) )
					continue;
				// A slot already drawing with a shared copy (one found before) keeps it.
				if ( materials.HasOverride( slot ) && shared.Owns( materials.GetOverride( slot ) ) )
					continue;
				materials.SetOverride( slot, shared.For( original, place ) );
			}
		}
	}

	// True when the drawn models are the ones the parts were found on.
	private bool Same( IEnumerable<ModelRenderer> renderers )
	{
		var count = 0;
		foreach ( var renderer in renderers )
		{
			if ( count >= _renderers.Count || _renderers[ count ] != renderer )
				return false;
			count++;
		}
		return count == _renderers.Count && count > 0;
	}

	private static bool Glows( string name, string[] glowing )
	{
		if ( string.IsNullOrEmpty( name ) )
			return false;
		foreach ( var file in glowing )
		{
			if ( name.Contains( file, StringComparison.OrdinalIgnoreCase ) )
				return true;
		}
		return false;
	}
}
