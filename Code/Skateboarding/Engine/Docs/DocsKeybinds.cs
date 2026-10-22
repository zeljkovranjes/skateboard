using System.Linq;
using Sandbox.UI;

namespace Skateboarding.Engine.Docs;

/// <summary>
/// The keybinds page: every keyboard binding as a table row, grouped. Change (or the key itself) waits for the key or
/// mouse button you press; Esc leaves it as it was. A key another binding has offers to swap the two. Reset puts every
/// key back.
/// </summary>
internal sealed class DocsKeybinds : Panel
{
	private string _listening;
	private int _listenedFrames;
	private string _refused;
	private (string Id, string Key, string Other)? _conflict;

	/// <summary>True while a binding waits for its new key.</summary>
	internal bool Listening => _listening is not null;

	public DocsKeybinds()
	{
		Style.FlexDirection = FlexDirection.Column;
		Build();
	}

	/// <summary>Shows the keys as they are now.</summary>
	internal void Refresh() => Build();

	/// <summary>Stops waiting for a key and forgets an unanswered swap.</summary>
	internal void Cancel()
	{
		_listening = null;
		_refused = null;
		_conflict = null;
	}

	/// <summary>
	/// A key read straight off the keyboard while a binding waits (SkateBindings.CaptureKey), taken from the frame after
	/// the click that started the wait. True if it was taken.
	/// </summary>
	internal bool Polled( string key )
	{
		if ( _listenedFrames++ < 1 || string.IsNullOrEmpty( key ) )
			return false;
		Captured( key );
		return true;
	}

	/// <summary>The key (as a binding names it) pressed while a binding waits for one.</summary>
	internal void Captured( string key )
	{
		var id = _listening;
		if ( id is null || string.IsNullOrEmpty( key ) )
			return;
		if ( key == "escape" )
		{
			_listening = null;
			_refused = null;
			Build();
			return;
		}
		// Voice chat, the scoreboard, chat and the console keep their keys: the wait goes on for another.
		if ( !SkateBindings.CanBind( key ) )
		{
			_refused = key;
			Build();
			return;
		}
		_listening = null;
		_refused = null;
		var other = SkateBindings.ConflictWith( id, key );
		if ( other is not null && other != id )
		{
			_conflict = (id, key, other);
			Build();
			return;
		}
		SkateBindings.Rebind( id, key );
		Build();
	}

	private void Listen( string id )
	{
		_conflict = null;
		_refused = null;
		_listening = id;
		_listenedFrames = 0;
		Build();
	}

	// The binding takes the key, and the binding that had it takes the key this one had (Rebind swaps them).
	private void Swap()
	{
		if ( _conflict is not { } conflict )
			return;
		_conflict = null;
		SkateBindings.Rebind( conflict.Id, conflict.Key );
		Build();
	}

	private void Build()
	{
		DeleteChildren( true );
		var bar = AddChild<Panel>( "toolbar" );
		var reset = bar.AddChild<Label>( "link" );
		reset.Text = "Reset all to defaults";
		reset.AddEventListener( "onclick", () =>
		{
			Cancel();
			SkateBindings.ResetAll();
			Build();
		} );
		bar.AddChild<Label>( "muted" ).Text = _refused is not null
			? $"{DocsKeyNames.Pretty( _refused )} is kept for the game (voice chat, scoreboard, chat and console). Press another key."
			: _listening is null
				? "Changes are saved straight away."
				: "Press the new key or mouse button. Esc keeps the old one.";
		foreach ( var group in SkateBindings.All.GroupBy( binding => binding.Group ) )
		{
			AddChild<Label>( "h3" ).Text = group.Key;
			foreach ( var binding in group )
				Row( binding );
		}
	}

	// The control, its default when it has been changed, its key, and Change (or Cancel while it waits for a key).
	private void Row( SkateBinding binding )
	{
		var key = SkateBindings.KeyOf( binding.Id );
		var listening = _listening == binding.Id;
		var id = binding.Id;
		var row = AddChild<Panel>( "tr" );
		row.AddChild<Label>( "td-title" ).Text = binding.Title;
		if ( key != binding.DefaultKey )
			row.AddChild<Label>( "td-default" ).Text = $"Default {DocsKeyNames.Pretty( binding.DefaultKey )}";
		var cell = row.AddChild<Panel>( "td-key" );
		var kbd = cell.AddChild<Label>( "kbd" );
		kbd.Text = listening ? "Press a key" : SkateBindings.KeyLabel( binding.Id );
		kbd.SetClass( "listening", listening );
		kbd.AddEventListener( "onclick", () => Listen( id ) );
		var action = row.AddChild<Panel>( "td-action" ).AddChild<Label>( "link" );
		action.Text = listening ? "Cancel" : "Change";
		action.AddEventListener( "onclick", () =>
		{
			if ( _listening == id )
			{
				Cancel();
				Build();
			}
			else
				Listen( id );
		} );
		if ( _conflict is not { } conflict || conflict.Id != binding.Id )
			return;
		var line = AddChild<Panel>( "conflict" );
		line.AddChild<Label>( "conflict-text" ).Text =
			$"{DocsKeyNames.Pretty( conflict.Key )} is already used for {SkateBindings.Get( conflict.Other )?.Title ?? conflict.Other}.";
		var swap = line.AddChild<Label>( "link" );
		swap.Text = "Swap";
		swap.AddEventListener( "onclick", Swap );
		var keep = line.AddChild<Label>( "link" );
		keep.Text = "Keep";
		keep.AddEventListener( "onclick", () =>
		{
			_conflict = null;
			Build();
		} );
	}
}
