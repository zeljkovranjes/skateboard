using System.Linq;
using Sandbox.UI;

namespace Skateboarding.Engine.Docs;

/// <summary>
/// The controller page: the whole pad as a table, the sticks first and then every control, grouped as the bindings group
/// them: what does it on the pad, with its glyph, and what it does. Read from the bindings, so it is the pad as it is.
/// The trick keys are the keyboard's; on a pad those tricks are the right stick's, as the sticks' rows say.
/// </summary>
internal static class DocsControllerMap
{
	internal static void Add( Panel page )
	{
		page.AddChild<Label>( "h3" ).Text = "Sticks";
		Row( page, "{ls}", "Left stick", "Steer. With a grab held in the air: up for a front flip, down for a back flip" );
		Row( page, "{rs}", "Right stick", "Pull back, then flick: forward for an ollie, up-left for a kickflip, up-right for a heelflip, left for a shove-it" );
		Row( page, "{rs}", "Right stick, part way", "Held part way back or forward: a manual, or on a rail a 5-0 or nosegrind. Pointed as you grab: which grab" );
		foreach ( var group in SkateBindings.All.Where( binding => !string.IsNullOrWhiteSpace( binding.Pad ) && binding.Group != "Trick keys" )
			.GroupBy( binding => binding.Group ) )
		{
			page.AddChild<Label>( "h3" ).Text = group.Key;
			foreach ( var binding in group )
				Row( page, "{pad:" + binding.Id + "}", binding.Pad, binding.Title );
		}
	}

	// The glyph and how it is done on the pad, then what it does.
	private static void Row( Panel page, string glyph, string how, string what )
	{
		var row = page.AddChild<Panel>( "tr" );
		var pad = row.AddChild<Panel>( "td-pad" );
		DocsInputLine.Add( pad, glyph, "td-glyph" );
		pad.AddChild<Label>( "td-pad-text" ).Text = how;
		row.AddChild<Label>( "td-what" ).Text = what;
	}
}
