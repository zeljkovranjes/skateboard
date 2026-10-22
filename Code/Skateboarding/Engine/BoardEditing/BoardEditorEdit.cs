using System;
using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;
using Skateboarding.Engine.Finish;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board's colours in Edit mode, in the editor's own look: words over the blurred game with nothing behind them, in
/// the right half once the board is pushed aside. Each colourable part (<see cref="BoardFinishPalette"/>) has a row of
/// round swatches, the one in use ringed in white with its name beside the part's heading; then Done and Reset (back to
/// the board as delivered). Every pick is told at once (<see cref="Changed"/>): the editor's board, the board itself
/// (seen by everyone) and the player's saved board take it. It comes in from the right, each part a beat after the one
/// before.
/// </summary>
internal sealed class BoardEditorEdit : Panel
{
	private readonly Dictionary<BoardFinishPart, List<Pickable>> _swatches = new();
	private readonly Dictionary<BoardFinishPart, Label> _values = new();
	private BoardLook _look;

	public BoardEditorEdit()
	{
		Make();
	}

	/// <summary>Called as Done is clicked.</summary>
	internal Action Done { get; set; }

	/// <summary>Called with the look as each colour is picked (Reset included).</summary>
	internal Action<BoardLook> Changed { get; set; }

	/// <summary>The look the swatches show; setting it moves the rings without telling <see cref="Changed"/>.</summary>
	internal BoardLook Look
	{
		get => _look;
		set
		{
			_look = value;
			Mark();
		}
	}

	internal bool Showing => HasClass( "shown" );

	internal void Show() => SetClass( "shown", true );

	internal void Hide() => SetClass( "shown", false );

	// Every part: the heading, a row of swatches per colourable part, Done and Reset.
	private void Make()
	{
		var head = AddChild<Panel>( "head" );
		head.AddChild<Label>( "title" ).Text = "Your board";
		head.AddChild<Label>( "subtitle" ).Text = "Everyone sees it; it is kept for next time";
		var beat = 1;
		foreach ( var part in BoardFinishPalette.Parts )
			Swatches( part, beat++ );
		var actions = AddChild<Panel>( "actions" );
		actions.AddClass( $"beat{beat}" );
		AddAction( actions, "Done", true, () => Done?.Invoke() );
		AddAction( actions, "Reset", false, () => Pick( BoardLook.Delivered ) );
		Mark();
	}

	// A part's heading with its colour's name beside it, and its row of swatches.
	private void Swatches( BoardFinishPart part, int beat )
	{
		var section = AddChild<Panel>( "section" );
		section.AddClass( $"beat{beat}" );
		var top = section.AddChild<Panel>( "top" );
		top.AddChild<Label>( "heading" ).Text = BoardFinishPalette.Title( part );
		_values[ part ] = top.AddChild<Label>( "value" );
		var row = section.AddChild<Panel>( "swatches" );
		var swatches = new List<Pickable>();
		var colours = BoardFinishPalette.Of( part );
		for ( var choice = 0; choice < colours.Length; choice++ )
		{
			var swatch = row.AddChild<Pickable>( "swatch" );
			swatch.AddChild<Panel>( "dot" ).Style.BackgroundColor = colours[ choice ].Swatch;
			var picked = choice;
			swatch.Picked = () => Pick( _look.With( part, picked ) );
			swatches.Add( swatch );
		}
		_swatches[ part ] = swatches;
	}

	private static void AddAction( Panel actions, string name, bool main, Action picked )
	{
		var action = actions.AddChild<Pickable>( "action" );
		action.SetClass( "main", main );
		action.AddChild<Label>( "action-name" ).Text = name;
		action.AddChild<Panel>( "underline" );
		action.Picked = picked;
	}

	private void Pick( BoardLook look )
	{
		Look = look;
		Changed?.Invoke( look );
	}

	// Rings the swatch in use for each part, with its colour's name beside the heading.
	private void Mark()
	{
		foreach ( var (part, swatches) in _swatches )
		{
			var choice = _look[ part ];
			for ( var at = 0; at < swatches.Count; at++ )
				swatches[ at ].SetClass( "picked", at == choice );
			if ( _values.TryGetValue( part, out var value ) )
				value.Text = BoardFinishPalette.Of( part )[ choice ].Name;
		}
	}

	// Something to click: a swatch or a word.
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
}
