using System.Collections.Generic;
using Sandbox;
using SalvagedHeart.Components;

namespace Skateboarding.Engine.Finish;

/// <summary>
/// Finishes what is drawn of a board (the game's board or its copy in the board editor) as a look: every material slot
/// drawn with one of the colourable parts' materials draws with that part's colour (<see cref="BoardFinishMaterials"/>),
/// and a board with the salvaged heart gets its sloshing oil bath (<see cref="HeartOilSlosh"/>) in the oil's colour and
/// its beating heart (heart_pump.vmdl, the heart's moving parts, which the board model leaves out) pumping on a loop.
/// </summary>
internal static class BoardFinishRenderers
{
	// The names of the oil bath's and the beating heart's objects, under the deck.
	private const string OilName = "Heart Oil";
	internal const string PumpName = "Heart Pump";

	// The beating heart's model and the loop it beats to (a strong beat, a smaller one, recovery: 0.83 s).
	private const string PumpModel = "models/skateboard_game/heart_pump.vmdl";
	private const string PumpBeat = "pump";

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
	/// Gives the deck its heart, in the board model's space (<paramref name="placement"/> takes the model's space into the
	/// deck's), if its model has the salvaged heart: the oil bath, which takes the look's colour, and the beating heart.
	/// </summary>
	internal static void Heart( GameObject deck, Model model, Transform placement, BoardLook look, BoardFinishMaterials shared )
	{
		if ( !deck.IsValid() || !HasHeart( model ) )
			return;
		var oil = Holder( deck, OilName, placement ).Components.GetOrCreate<HeartOilSlosh>();
		oil.OilMaterial = shared.Oil( look );
		var pump = Holder( deck, PumpName, placement ).Components.GetOrCreate<SkinnedModelRenderer>();
		if ( pump.Model is null || pump.Model.ResourcePath != PumpModel )
		{
			pump.UseAnimGraph = false;
			pump.Model = Model.Load( PumpModel );
		}
		if ( pump.Sequence.Name != PumpBeat )
			pump.Sequence.Name = PumpBeat;
		pump.Sequence.Looping = true;
	}

	// The deck's object of that name (made at the model's place on the deck if it has none): drawn on each client, never
	// networked or saved.
	private static GameObject Holder( GameObject deck, string name, Transform placement )
	{
		var holder = deck.Children.Find( child => child.Name == name );
		if ( holder.IsValid() )
			return holder;
		holder = new GameObject( deck, true, name )
		{
			NetworkMode = NetworkMode.Never,
			LocalTransform = placement
		};
		holder.Flags |= GameObjectFlags.NotSaved;
		return holder;
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
