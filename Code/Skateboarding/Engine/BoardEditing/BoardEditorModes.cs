using System;
using System.Collections.Generic;
using Sandbox.UI;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor's modes along the top, as words alone: Inspect (turn the board over and pick its portraits) and Edit
/// (the board's settings, <see cref="BoardEditorEdit"/>). The one in use is white and underlined, the underline growing
/// out from its middle as it is picked; the other is grey until the mouse is over it.
/// </summary>
internal sealed class BoardEditorModes : Panel
{
	/// <summary>The modes, in order along the top.</summary>
	internal static readonly string[] Names = { "Inspect", "Edit" };

	internal const int Inspect = 0;
	internal const int Edit = 1;

	private readonly List<Panel> _tabs = new();

	public BoardEditorModes()
	{
		foreach ( var name in Names )
		{
			var tab = AddChild<Tab>( "mode" );
			tab.Modes = this;
			tab.Mode = _tabs.Count;
			tab.AddChild<Label>( "mode-name" ).Text = name;
			tab.AddChild<Panel>( "underline" );
			_tabs.Add( tab );
		}
		Select( Inspect, false );
	}

	/// <summary>The mode in use, by its place in <see cref="Names"/>.</summary>
	internal int Selected { get; private set; }

	/// <summary>Called with the mode put in use, as it changes.</summary>
	internal Action<int> Changed { get; set; }

	/// <summary>
	/// Puts <paramref name="mode"/> in use (by its place in <see cref="Names"/>), telling <see cref="Changed"/> if it
	/// changes and <paramref name="tell"/>.
	/// </summary>
	internal void Select( int mode, bool tell = true )
	{
		var changed = mode != Selected;
		Selected = mode;
		for ( var at = 0; at < _tabs.Count; at++ )
			_tabs[ at ].SetClass( "active", at == mode );
		if ( changed && tell )
			Changed?.Invoke( mode );
	}

	// A mode's words; a click puts it in use.
	private sealed class Tab : Panel
	{
		internal BoardEditorModes Modes;
		internal int Mode;

		public Tab()
		{
		}

		protected override void OnClick( MousePanelEvent e )
		{
			base.OnClick( e );
			Modes?.Select( Mode );
		}
	}
}
