using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine;

/// <summary>
/// The trick keys while riding, small in the bottom left corner where the game's hotbar and health were (put away for the
/// ride), out of the way of the skater: each key's glyph, as the player has it bound, and its trick. Only on keyboard and
/// mouse; a controller flicks its tricks with the right stick.
/// </summary>
internal sealed class SkateTrickKeys : Panel
{
	// The controls card's look (SkateControlsCard), smaller: Inter, the HUD accent down the left and the dark gradient
	// fading right.
	private const string Styles = """
		SkateTrickKeys
		{
			position: absolute;
			left: 0;
			bottom: 56px;
			flex-direction: column;
			gap: 3px;
			padding: 10px 56px 12px 22px;
			font-family: Inter;
			pointer-events: none;
			background-image: linear-gradient( to right, rgba( 38, 38, 38, 0.5 ), rgba( 38, 38, 38, 0 ) );
			opacity: 0;
			transition: opacity 0.2s ease;
		}
		SkateTrickKeys.visible
		{
			opacity: 0.9;
		}
		SkateTrickKeys .accent
		{
			position: absolute;
			left: 0;
			top: 0;
			bottom: 0;
			width: 3px;
			background-color: rgba( 77, 144, 252, 0.9 );
		}
		SkateTrickKeys .heading
		{
			margin-bottom: 3px;
			font-size: 12px;
			font-weight: 800;
			text-transform: uppercase;
			color: rgba( 77, 144, 252, 1 );
		}
		SkateTrickKeys .row
		{
			flex-direction: row;
			align-items: center;
			gap: 10px;
		}
		SkateTrickKeys .key
		{
			height: 24px;
			min-width: 24px;
			background-size: contain;
			background-repeat: no-repeat;
			background-position: center;
		}
		SkateTrickKeys .text
		{
			font-size: 13px;
			font-weight: 700;
			white-space: nowrap;
			text-transform: uppercase;
			color: rgba( 255, 255, 255, 0.9 );
		}
		""";

	private bool _built;

	internal SkateTrickKeys()
	{
		StyleSheet.Parse( Styles );
	}

	/// <summary>Shows the keys while this client's player rides on keyboard and mouse.</summary>
	internal void Tick( bool riding )
	{
		var show = riding && !Input.UsingController;
		if ( show && !_built )
			Build();
		SetClass( "visible", show );
	}

	private void Build()
	{
		_built = true;
		AddChild<Panel>( "accent" );
		AddChild<Label>( "heading" ).Text = "Tricks: hold, let go to pop";
		for ( var i = 0; i < SandboxActions.TrickKeys.Length; i++ )
		{
			var row = AddChild<Panel>( "row" );
			var texture = Input.GetGlyph( SandboxActions.TrickKeys[ i ], InputGlyphSize.Small, false );
			var glyph = row.AddChild<Image>( "key" );
			glyph.Texture = texture;
			if ( texture is not null && texture.Height > 0 )
				glyph.Style.Width = Length.Pixels( 24f * texture.Width / texture.Height );
			row.AddChild<Label>( "text" ).Text = TrickButtons.Names[ i ];
		}
	}
}
