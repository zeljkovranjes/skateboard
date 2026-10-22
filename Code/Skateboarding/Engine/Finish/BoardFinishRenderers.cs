using System.Collections.Generic;
using Sandbox;
using SalvagedHeart.Components;

namespace Skateboarding.Engine.Finish;

/// <summary>
/// Finishes what is drawn of a board (the game's board or its copy in the board editor) as a look: every material slot
/// drawn with one of the colourable parts' materials draws with that part's colour (<see cref="BoardFinishMaterials"/>),
/// and a board with the salvaged heart gets its sloshing oil bath (<see cref="HeartOilSlosh"/>) in the oil's colour.
/// </summary>
internal static class BoardFinishRenderers
{
	// The name of the oil bath's object, under the deck.
	private const string OilName = "Heart Oil";

	/// <summary>Draws each of <paramref name="renderers"/> in <paramref name="look"/>'s colours.</summary>
	internal static void Apply( IEnumerable<ModelRenderer> renderers, BoardLook look, BoardFinishMaterials shared )
	{
		foreach ( var renderer in renderers )
		{
			if ( !renderer.IsValid() || renderer.Model is null )
				continue;
			var materials = renderer.Materials;
			for ( var slot = 0; slot < materials.Count; slot++ )
			{
				var original = materials.GetOriginal( slot );
				if ( BoardFinishMaterials.PartOf( original ) is not { } part )
					continue;
				var wanted = shared.For( original, part, look[ part ] );
				if ( materials.HasOverride( slot ) ? materials.GetOverride( slot ) == wanted : wanted == original )
					continue;
				materials.SetOverride( slot, wanted );
			}
		}
	}

	/// <summary>
	/// Gives the deck its oil bath, in the board model's space (<paramref name="placement"/> takes the model's space into
	/// the deck's), if its model has the salvaged heart; the bath already there takes the look's colour.
	/// </summary>
	internal static void Oil( GameObject deck, Model model, Transform placement, BoardLook look, BoardFinishMaterials shared )
	{
		if ( !deck.IsValid() || !HasHeart( model ) )
			return;
		var holder = deck.Children.Find( child => child.Name == OilName );
		if ( !holder.IsValid() )
		{
			holder = new GameObject( deck, true, OilName )
			{
				NetworkMode = NetworkMode.Never,
				LocalTransform = placement
			};
			holder.Flags |= GameObjectFlags.NotSaved;
		}
		var oil = holder.Components.GetOrCreate<HeartOilSlosh>();
		oil.OilMaterial = shared.Oil( look );
	}

	// Whether the model is drawn with the heart's oil (only the salvaged board is).
	private static bool HasHeart( Model model )
	{
		if ( model is null )
			return false;
		foreach ( var material in model.Materials )
		{
			if ( BoardFinishMaterials.PartOf( material ) == BoardFinishPart.Oil )
				return true;
		}
		return false;
	}
}
