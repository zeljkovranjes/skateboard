using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine;

/// <summary>
/// The board's controls, shown the way Sandbox shows a tool's (its tool info panel, top left, where the player's own tool
/// info would be: that tool is put away while the skater stands in): the board's name, a line about it and its keys, one
/// card for riding and one for carrying it, keyboard or controller as the player is using. It shows as the player gets
/// on or picks the board up and fades after a few seconds, and comes back whenever the skater stands still.
/// </summary>
internal sealed class SkateControlsCard : Panel
{
	// Seconds the card shows as a ride or a carry begins; slowest (m/s) the skater counts as still, and seconds still
	// before the card comes back.
	private const float ShowFor = 7f;
	private const float StillSpeed = 0.4f;
	private const float StillShow = 1.5f;

	// Sandbox's tool info panel (UI/ToolInfo/ToolInfoPanel.razor.scss) and its theme: Inter, the HUD accent, the dark
	// gradient fading right and the scrolling grid.
	private const string Styles = """
		SkateControlsCard
		{
			position: absolute;
			left: 0;
			top: 30px;
			width: 840px;
			flex-direction: column;
			gap: 2px;
			font-family: Inter;
			pointer-events: none;
			opacity: 0;
			transition: opacity 0.15s ease;
		}
		SkateControlsCard.visible
		{
			opacity: 1;
		}
		SkateControlsCard .header
		{
			position: relative;
			flex-direction: column;
			padding: 17px 52px 20px;
			background-image: linear-gradient( to right, rgba( 38, 38, 38, 0.5 ), rgba( 38, 38, 38, 0 ) );
		}
		SkateControlsCard .accent
		{
			position: absolute;
			left: 0;
			top: 0;
			bottom: 0;
			width: 280px;
			background-image: linear-gradient( to right, rgba( 77, 144, 252, 0.6 ), rgba( 77, 144, 252, 0 ) );
		}
		SkateControlsCard .grid-mask
		{
			position: absolute;
			left: 0;
			top: 0;
			right: 0;
			bottom: 0;
			overflow: hidden;
			opacity: 0.15;
			mask-image: linear-gradient( to right, black, transparent );
			mask-size: 100% 100%;
			mask-repeat: no-repeat;
			mask-mode: alpha;
		}
		SkateControlsCard .grid
		{
			position: absolute;
			left: -60px;
			top: -60px;
			right: 0;
			bottom: 0;
			background-image: url( /UI/tool_grid.png );
			background-size: 60px 60px;
			background-repeat: repeat;
			animation: skate-card-grid 3s linear infinite;
		}
		SkateControlsCard .title
		{
			font-size: 48px;
			font-weight: 800;
			line-height: 48px;
			color: white;
		}
		SkateControlsCard .description
		{
			max-width: 736px;
			margin-top: 2px;
			font-size: 16px;
			font-weight: 600;
			line-height: 18px;
			color: rgba( 255, 255, 255, 0.75 );
		}
		SkateControlsCard .hints
		{
			flex-direction: row;
			align-items: center;
			gap: 50px;
			padding: 6px 52px;
			background-image: linear-gradient( to right, rgba( 38, 38, 38, 0.5 ), rgba( 38, 38, 38, 0 ) );
			align-self: flex-start;
			min-width: 100%;
		}
		SkateControlsCard .hint
		{
			flex-direction: row;
			align-items: center;
			flex-shrink: 0;
			gap: 14px;
		}
		SkateControlsCard .keys
		{
			flex-direction: row;
			align-items: center;
			gap: 4px;
		}
		SkateControlsCard .key
		{
			height: 32px;
			min-width: 32px;
			background-size: contain;
			background-repeat: no-repeat;
			background-position: center;
		}
		SkateControlsCard .text
		{
			font-size: 15px;
			font-weight: 700;
			white-space: nowrap;
			text-transform: uppercase;
			color: rgba( 255, 255, 255, 0.92 );
		}
		@keyframes skate-card-grid
		{
			from { transform: translate( 0px, 0px ); }
			to { transform: translate( 25px, 25px ); }
		}
		""";

	private enum Card { None, Riding, Carrying }

	// A key: an action's glyph, or a controller stick's.
	private readonly record struct Key( string Action, InputAnalog? Stick = null );

	private Card _card;
	private bool _controller;
	private float _shownFor;
	private float _still;

	internal SkateControlsCard()
	{
		StyleSheet.Parse( Styles );
	}

	/// <summary>Follows the skater: riding or carrying the board (any other mode shows nothing) and how fast it goes.</summary>
	internal void Tick( SkateRiderMode mode, float speed )
	{
		var card = mode switch
		{
			SkateRiderMode.Riding => Card.Riding,
			SkateRiderMode.Carrying => Card.Carrying,
			_ => Card.None
		};
		if ( card != _card )
			_shownFor = card == Card.None ? 0f : ShowFor;
		if ( card != _card || Input.UsingController != _controller )
		{
			_card = card;
			_controller = Input.UsingController;
			Build();
		}
		_shownFor -= Time.Delta;
		_still = speed < StillSpeed ? _still + Time.Delta : 0f;
		SetClass( "visible", _card != Card.None && (_shownFor > 0f || _still > StillShow) );
	}

	private void Build()
	{
		DeleteChildren( true );
		if ( _card == Card.None )
			return;
		var header = AddChild<Panel>( "header" );
		header.AddChild<Panel>( "accent" );
		header.AddChild<Panel>( "grid-mask" ).AddChild<Panel>( "grid" );
		header.AddChild<Label>( "title" ).Text = "Skateboard";
		header.AddChild<Label>( "description" ).Text = _card == Card.Riding
			? "Push off, ollie, flip, grab and grind. Land it clean or you bail."
			: "Board in hand. Throw it down to ride on, or set it down.";
		foreach ( var row in Rows() )
		{
			var hints = AddChild<Panel>( "hints" );
			foreach ( var (keys, text) in row )
			{
				var hint = hints.AddChild<Panel>( "hint" );
				var glyphs = hint.AddChild<Panel>( "keys" );
				foreach ( var key in keys )
					Glyph( glyphs, key );
				hint.AddChild<Label>( "text" ).Text = text;
			}
		}
	}

	// The card's rows of keys and what they do, on the keyboard or the controller (SandboxActions).
	private (Key[] Keys, string Text)[][] Rows()
	{
		if ( _card == Card.Carrying )
		{
			return new[]
			{
				new[]
				{
					(new[] { new Key( SandboxActions.Ollie ) }, "Jump"),
					(new[] { new Key( SandboxActions.GetOff ) }, "Ride on"),
					(new[] { new Key( SandboxActions.GetOff ) }, "Hold to drop"),
					(new[] { new Key( SandboxActions.Catch ) }, "Throw")
				}
			};
		}
		if ( _controller )
		{
			return new[]
			{
				new[]
				{
					(new[] { new Key( null, InputAnalog.LeftStickX ) }, "Steer"),
					(new[] { new Key( SandboxActions.PadPush ) }, "Push"),
					(new[] { new Key( SandboxActions.PadBrake ) }, "Brake"),
					(new[] { new Key( SandboxActions.PadSlide ) }, "Slide"),
					(new[] { new Key( SandboxActions.PadPump ) }, "Pump")
				},
				new[]
				{
					(new[] { new Key( null, InputAnalog.RightStickX ) }, "Ollie and flip"),
					(new[] { new Key( SandboxActions.Catch ) }, "Catch"),
					(new[] { new Key( SandboxActions.GetOff ) }, "Get off")
				}
			};
		}
		return new[]
		{
			new[]
			{
				(new[] { new Key( SandboxActions.Push ) }, "Push"),
				(new[] { new Key( SandboxActions.Brake ) }, "Brake"),
				(new[] { new Key( "Left" ), new Key( "Right" ) }, "Steer"),
				(new[] { new Key( SandboxActions.Ollie ) }, "Ollie"),
				(new[] { new Key( SandboxActions.Slide ) }, "Slide")
			},
			new[]
			{
				(new[] { new Key( SandboxActions.FlipLeft ), new Key( SandboxActions.FlipRight ) }, "Flip"),
				(new[] { new Key( SandboxActions.Flick ) }, "Flick tricks"),
				(new[] { new Key( SandboxActions.GrabFront ), new Key( SandboxActions.GrabBack ) }, "Grab"),
				(new[] { new Key( SandboxActions.GetOff ) }, "Get off")
			}
		};
	}

	// One key's glyph, as wide as its texture is for the key's height (long key names are wider).
	private static void Glyph( Panel parent, Key key )
	{
		var texture = key.Stick is { } stick
			? Input.GetGlyph( stick, InputGlyphSize.Medium, false )
			: Input.GetGlyph( key.Action, InputGlyphSize.Medium, false );
		var glyph = parent.AddChild<Image>( "key" );
		glyph.Texture = texture;
		if ( texture is not null && texture.Height > 0 )
			glyph.Style.Width = Length.Pixels( 32f * texture.Width / texture.Height );
	}
}
