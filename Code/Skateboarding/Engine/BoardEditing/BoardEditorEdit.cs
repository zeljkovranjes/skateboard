using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board's settings in Edit mode, in the editor's own look: words over the blurred game with nothing behind them, in
/// the right half once the board is pushed aside. The glow (stepping through its colours, one colour, or off), which
/// colours it steps through and how quickly; the deck's, trucks' and wheels' colours as round swatches, each with its
/// name beside the heading; then Done and Reset. It comes in from the right, each part a beat after the one before.
/// Only its look for now: what is picked is kept here and changes nothing on the board yet.
/// </summary>
internal sealed class BoardEditorEdit : Panel
{
	private static readonly string[] GlowModes = { "Cycle", "Solid", "Off" };
	// The glow's colours (the cycle the board's blue steps through), then the parts', first as the board comes.
	private static readonly (string Name, Color Colour)[] GlowColours =
	{
		("Red", Rgb( 255, 51, 51 )), ("Green", Rgb( 0, 255, 0 )), ("Yellow", Rgb( 255, 255, 0 )),
		("Blue", Rgb( 0, 0, 255 )), ("Cyan", Rgb( 0, 255, 255 )),
	};
	private static readonly (string Name, Color Colour)[] DeckColours =
	{
		("Black", Rgb( 20, 21, 24 )), ("Charcoal", Rgb( 58, 61, 67 )), ("White", Rgb( 236, 236, 236 )),
		("Red", Rgb( 200, 48, 44 )), ("Navy", Rgb( 31, 47, 92 )), ("Natural", Rgb( 199, 154, 99 )),
	};
	private static readonly (string Name, Color Colour)[] TruckColours =
	{
		("Black", Rgb( 27, 28, 31 )), ("Silver", Rgb( 191, 196, 203 )), ("White", Rgb( 240, 240, 240 )),
		("Gold", Rgb( 201, 162, 74 )), ("Red", Rgb( 184, 50, 46 )),
	};
	private static readonly (string Name, Color Colour)[] WheelColours =
	{
		("Black", Rgb( 26, 26, 28 )), ("White", Rgb( 241, 238, 230 )), ("Red", Rgb( 210, 60, 51 )),
		("Blue", Rgb( 47, 111, 224 )), ("Yellow", Rgb( 242, 194, 48 )), ("Green", Rgb( 53, 178, 107 )),
	};
	// The glow's step (milliseconds): the slowest, the quickest, and as the board comes.
	private const float SlowestStep = 400f;
	private const float QuickestStep = 40f;
	private const float StepAsComes = 100f;

	// Set by Reset: the settings are made again, as the board comes, on the next tick (not inside Reset's own click).
	private bool _remake;

	public BoardEditorEdit()
	{
		Make();
	}

	/// <summary>Called as Done is clicked.</summary>
	internal Action Done { get; set; }

	internal bool Showing => HasClass( "shown" );

	internal void Show() => SetClass( "shown", true );

	internal void Hide() => SetClass( "shown", false );

	public override void Tick()
	{
		base.Tick();
		if ( !_remake )
			return;
		_remake = false;
		Make();
	}

	// Every part, as the board comes.
	private void Make()
	{
		DeleteChildren( true );
		var head = AddChild<Panel>( "head" );
		head.AddChild<Label>( "title" ).Text = "Your board";
		head.AddChild<Label>( "subtitle" ).Text = "Glow, deck, trucks and wheels";

		var glow = Section( "Glow", 1 );
		Choices( glow.Body, GlowModes, 0 );

		var colours = Section( "Glow colours", 2 );
		Swatches( colours.Body, GlowColours, colours.Value, many: true );

		var speed = Section( "Speed", 3 );
		var slider = speed.Body.AddChild<Slider>( "slider" );
		slider.Changed = share => speed.Value.Text = $"every {Step( share ):F0} ms";
		slider.Set( (SlowestStep - StepAsComes) / (SlowestStep - QuickestStep) );

		Swatches( Section( "Deck", 4 ), DeckColours );
		Swatches( Section( "Trucks", 5 ), TruckColours );
		Swatches( Section( "Wheels", 6 ), WheelColours );

		var actions = AddChild<Panel>( "actions" );
		actions.AddClass( "beat7" );
		var done = actions.AddChild<Pickable>( "action" );
		done.AddClass( "main" );
		done.AddChild<Label>( "action-name" ).Text = "Done";
		done.AddChild<Panel>( "underline" );
		done.Picked = () => Done?.Invoke();
		var reset = actions.AddChild<Pickable>( "action" );
		reset.AddChild<Label>( "action-name" ).Text = "Reset";
		reset.AddChild<Panel>( "underline" );
		reset.Picked = () => _remake = true;
	}

	// A titled part coming in on its beat: its heading, what is picked beside it, and its controls below.
	private (Panel Body, Label Value) Section( string title, int beat )
	{
		var section = AddChild<Panel>( "section" );
		section.AddClass( $"beat{beat}" );
		var top = section.AddChild<Panel>( "top" );
		top.AddChild<Label>( "heading" ).Text = title;
		return (section, top.AddChild<Label>( "value" ));
	}

	private static void Swatches( (Panel Body, Label Value) section, (string Name, Color Colour)[] colours ) =>
		Swatches( section.Body, colours, section.Value, many: false );

	// Words to pick one of, the picked one white and underlined.
	private static void Choices( Panel parent, string[] names, int picked )
	{
		var row = parent.AddChild<Panel>( "choices" );
		var choices = new List<Pickable>();
		foreach ( var name in names )
		{
			var choice = row.AddChild<Pickable>( "choice" );
			choice.AddChild<Label>( "choice-name" ).Text = name;
			choice.AddChild<Panel>( "underline" );
			choice.Picked = () => choices.ForEach( each => each.SetClass( "picked", each == choice ) );
			choices.Add( choice );
		}
		choices[ picked ].SetClass( "picked", true );
	}

	// Round swatches, ringed in white when picked: one of them, its name beside the heading, or (many) any of them, how
	// many beside it. They come picked as the board comes: the first, or all of them.
	private static void Swatches( Panel parent, (string Name, Color Colour)[] colours, Label value, bool many )
	{
		var row = parent.AddChild<Panel>( "swatches" );
		var swatches = new List<Pickable>();
		void Tell() => value.Text = many
			? $"{swatches.Count( each => each.HasClass( "picked" ) )} of {swatches.Count}"
			: colours[ swatches.FindIndex( each => each.HasClass( "picked" ) ) ].Name;
		foreach ( var (_, colour) in colours )
		{
			var swatch = row.AddChild<Pickable>( "swatch" );
			swatch.AddChild<Panel>( "dot" ).Style.BackgroundColor = colour;
			swatch.Picked = () =>
			{
				if ( many )
					swatch.SetClass( "picked", !swatch.HasClass( "picked" ) || swatches.Count( each => each.HasClass( "picked" ) ) == 1 );
				else
					swatches.ForEach( each => each.SetClass( "picked", each == swatch ) );
				Tell();
			};
			swatch.SetClass( "picked", many || swatches.Count == 0 );
			swatches.Add( swatch );
		}
		Tell();
	}

	// The glow's step (milliseconds) for a share along the speed: slowest at the left, quickest at the right.
	private static float Step( float share ) =>
		MathF.Round( (SlowestStep + (QuickestStep - SlowestStep) * share) / 10f ) * 10f;

	private static Color Rgb( int r, int g, int b ) => new( r / 255f, g / 255f, b / 255f );

	// Something to click: a word or a swatch.
	private sealed class Pickable : Panel
	{
		internal Action Picked;

		public Pickable()
		{
		}

		protected override void OnClick( MousePanelEvent e )
		{
			base.OnClick( e );
			Picked?.Invoke();
		}
	}

	// A thin line with a round knob, set by a press anywhere along it and dragged while held.
	private sealed class Slider : Panel
	{
		internal Action<float> Changed;
		private readonly Panel _fill;
		private readonly Panel _knob;
		private bool _held;

		public Slider()
		{
			AddChild<Panel>( "rail" );
			_fill = AddChild<Panel>( "fill" );
			_knob = AddChild<Panel>( "knob" );
		}

		internal void Set( float share )
		{
			share = Math.Clamp( share, 0f, 1f );
			_fill.Style.Width = Length.Percent( share * 100f );
			_knob.Style.Left = Length.Percent( share * 100f );
			Changed?.Invoke( share );
		}

		protected override void OnMouseDown( MousePanelEvent e )
		{
			base.OnMouseDown( e );
			_held = true;
			Follow();
		}

		protected override void OnMouseUp( MousePanelEvent e )
		{
			base.OnMouseUp( e );
			_held = false;
		}

		public override void Tick()
		{
			base.Tick();
			if ( _held )
				Follow();
		}

		private void Follow()
		{
			if ( Box.Rect.Width > 0f )
				Set( MousePosition.x / Box.Rect.Width );
		}
	}
}
