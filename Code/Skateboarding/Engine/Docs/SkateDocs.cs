using System.Collections.Generic;
using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine.Docs;

/// <summary>
/// The skateboard docs, over the game blurred as the board editor is: the pages listed down the side as words, the open
/// one white and underlined, and the open page beside them. While they are open they hold the input focus, so they have
/// the mouse and the keyboard, and the skate controls are blocked on both devices (SkateBindings.Blocked), so neither the
/// player nor the board moves. The docs key or button (SkateBindings "docs") or Esc closes them; Esc is taken from the
/// game, so it never also opens the game's menu. The side comes in from the left as they open, and each page from the
/// right as it is opened.
/// </summary>
internal sealed class SkateDocs : Panel
{
	// Frames a newly shown part waits before it is told to come in: it must be laid out once as hidden first, or it would
	// appear in place at once instead of sliding in.
	private const int RevealFrames = 2;

	/// <summary>True while the docs are open on this client.</summary>
	internal static bool IsOpen { get; private set; }

	private readonly Panel _side;
	private readonly Panel _main;
	private readonly Dictionary<string, Panel> _links = new();
	private readonly DocsPage[] _pages = DocsContent.Pages;
	private Label _foot;
	private DocsPage _page;
	private DocsKeybinds _keybinds;
	private bool _justOpened;
	private Panel _article;
	private int _sideIn;
	private int _articleIn;

	/// <summary>The look the panel was made with (<see cref="DocsStyles.Version"/>; 0 for one made before it was kept).</summary>
	internal int Version { get; private set; }

	internal SkateDocs()
	{
		Version = DocsStyles.Version;
		StyleSheet.Parse( DocsStyles.Sheet );
		AcceptsFocus = true;
		var window = AddChild<Panel>( "window" );
		_side = window.AddChild<Panel>( "side" );
		_main = window.AddChild<Panel>( "main" );
		BuildSide();
		Show( _pages[ 0 ] );
		SkateBindings.Changed += OnBindingsChanged;
	}

	internal void Open()
	{
		SetClass( "open", true );
		IsOpen = true;
		_justOpened = true;
		SkateBindings.Blocked = true;
		Show( _page ?? _pages[ 0 ] );
		_sideIn = RevealFrames;
		Focus();
	}

	internal void Close()
	{
		_keybinds?.Cancel();
		_sideIn = 0;
		SetClass( "shown", false );
		SetClass( "open", false );
		IsOpen = false;
		SkateBindings.Blocked = false;
		Blur();
	}

	/// <summary>
	/// The open docs' input, at the start of each frame (SkateDocsSystem), where keys read as every control reads them:
	/// Esc, the docs key or button, and a key for a binding that waits for one. Keys also come straight to the window
	/// while it has the focus (OnButtonTyped).
	/// </summary>
	internal void Frame()
	{
		if ( !IsOpen )
			return;
		// Clicking about can take the focus away, and with it the keyboard: it is taken back at once.
		if ( !HasFocus )
			Focus();
		// Esc is the docs' while they are open: it ends a rebind's wait, or closes them.
		if ( Input.EscapePressed )
		{
			Input.EscapePressed = false;
			if ( _keybinds is { Listening: true } )
				_keybinds.Captured( "escape" );
			else
				Close();
			return;
		}
		if ( _keybinds is { Listening: true } && _keybinds.Polled( SkateBindings.CaptureKey() ) )
			return;
		// The press that opened the docs must not close them again.
		if ( _justOpened )
		{
			_justOpened = false;
			return;
		}
		if ( SkateBindings.Toggled( "docs" ) )
			Close();
	}

	// The side and the open page told to come in, once each has been laid out as hidden.
	public override void Tick()
	{
		base.Tick();
		if ( _sideIn > 0 && --_sideIn == 0 )
			SetClass( "shown", true );
		if ( _articleIn > 0 && --_articleIn == 0 )
			_article?.SetClass( "shown", true );
	}

	public override void OnDeleted()
	{
		SkateBindings.Changed -= OnBindingsChanged;
		if ( IsOpen )
			SkateBindings.Blocked = false;
		IsOpen = false;
		base.OnDeleted();
	}

	public override void OnButtonTyped( ButtonEvent e )
	{
		if ( !e.Pressed )
			return;
		var button = DocsKeyNames.FromButton( e.Button );
		if ( _keybinds is { Listening: true } )
		{
			_keybinds.Captured( button );
			return;
		}
		if ( button == "escape" || !_justOpened && button == SkateBindings.KeyOf( "docs" ) )
			Close();
	}

	// Mouse buttons for a binding waiting for its key (keys come through OnButtonTyped). The click that started the wait
	// was let go before the wait began, so it is not taken.
	public override void OnButtonEvent( ButtonEvent e )
	{
		if ( e.Pressed && _keybinds is { Listening: true } && e.Button.StartsWith( "mouse" ) )
		{
			_keybinds.Captured( DocsKeyNames.FromButton( e.Button ) );
			return;
		}
		base.OnButtonEvent( e );
	}

	// A key changed: the open page is drawn again with the keys as they now are, where it was scrolled to.
	private void OnBindingsChanged()
	{
		Foot();
		if ( _page?.Kind == DocsPageKind.Keybinds )
			_keybinds?.Refresh();
		else if ( _page is not null )
			Show( _page, true );
	}

	// The pages down the side as words, under their groups' names, each with an underline shown while it is open.
	private void BuildSide()
	{
		string group = null;
		foreach ( var page in _pages )
		{
			if ( page.Group != group )
			{
				var heading = _side.AddChild<Label>( "nav-heading" );
				heading.Text = page.Group;
				heading.SetClass( "first", group is null );
				group = page.Group;
			}
			var link = _side.AddChild<Panel>( "nav-link" );
			link.AddChild<Label>( "nav-name" ).Text = page.Title;
			link.AddChild<Panel>( "underline" );
			var shown = page;
			link.AddEventListener( "onclick", () => Show( shown ) );
			_links[ page.Id ] = link;
		}
		_foot = _side.AddChild<Label>( "nav-foot" );
		Foot();
	}

	// How to close the docs, with the docs key as it is bound.
	private void Foot()
	{
		if ( _foot is not null )
			_foot.Text = $"{SkateBindings.KeyLabel( "docs" )} or Esc to close";
	}

	private void Show( DocsPage page, bool keepScroll = false )
	{
		var scroll = _main.ScrollOffset;
		_page = page;
		foreach ( var (id, link) in _links )
			link.SetClass( "active", id == page.Id );
		_main.DeleteChildren( true );
		_main.ScrollOffset = keepScroll ? scroll : Vector2.Zero;
		_keybinds = null;
		var article = _main.AddChild<Panel>( "article" );
		_article = article;
		// A page opened comes in from the right; one drawn again where it was (a key changed) is there at once.
		if ( keepScroll )
		{
			article.SetClass( "shown", true );
			_articleIn = 0;
		}
		else
			_articleIn = RevealFrames;
		article.AddChild<Label>( "eyebrow" ).Text = page.Group;
		article.AddChild<Label>( "h1" ).Text = page.Title;
		article.AddChild<Label>( "lead" ).Text = page.Lead;
		if ( page.Basics is { Length: > 0 } )
		{
			var list = article.AddChild<Panel>( "list" );
			foreach ( var line in page.Basics )
			{
				var item = list.AddChild<Panel>( "li" );
				item.AddChild<Label>( "bullet" ).Text = "•";
				DocsInputLine.Add( item, line, "li-text" );
			}
		}
		switch ( page.Kind )
		{
			case DocsPageKind.Controller:
				DocsControllerMap.Add( article );
				break;
			case DocsPageKind.Keybinds:
				_keybinds = article.AddChild<DocsKeybinds>();
				break;
			default:
				DocsMoves.Add( article, page.Moves );
				break;
		}
	}
}
