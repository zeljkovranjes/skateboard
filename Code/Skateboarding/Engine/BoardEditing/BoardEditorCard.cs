using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// Who a picked portrait shows, as words over the blurred game with nothing behind them: their picture (the portrait's own
/// image, their Steam avatar, or their initials) beside their name and handle; a line about them; how long they have been
/// a member, how much they have posted and their team, as figures; what they work on; their roles; and their links. It
/// comes in from the right once the board is pushed aside, each part following the one before, and goes out to the right
/// as the portrait is let go. Its parts are made afresh for each person, so a card made before a reload never misses one.
/// </summary>
internal sealed class BoardEditorCard : Panel
{
	// Seconds the card is out for as it changes from one person to the next.
	private const float ChangeOver = 0.22f;

	private bool _comingBack;
	private PortraitInfo _next;
	private RealTimeSince _sinceOut;
	// Frames left before newly made parts are shown: each must be laid out once as hidden first, or it would appear in
	// place at once instead of sliding in.
	private int _revealIn;

	public BoardEditorCard()
	{
	}

	/// <summary>True while the card is shown, or coming (back) in.</summary>
	internal bool Showing => HasClass( "shown" ) || _comingBack || _revealIn > 0;

	/// <summary>Shows who <paramref name="person"/> is (sliding in, or changing over if the card is up already).</summary>
	internal void Show( PortraitInfo person )
	{
		// Changing over from someone else: the card goes out and comes back in with the new person, so the change reads.
		if ( HasClass( "shown" ) || _comingBack )
		{
			_next = person;
			_comingBack = true;
			_sinceOut = 0f;
			_revealIn = 0;
			SetClass( "shown", false );
			return;
		}
		Fill( person );
		_revealIn = 2;
	}

	internal void Hide()
	{
		_comingBack = false;
		_next = null;
		_revealIn = 0;
		SetClass( "shown", false );
	}

	public override void Tick()
	{
		base.Tick();
		if ( _revealIn > 0 && --_revealIn == 0 )
			SetClass( "shown", true );
		if ( !_comingBack || _sinceOut < ChangeOver )
			return;
		_comingBack = false;
		if ( _next is not null )
			Fill( _next );
		_next = null;
		_revealIn = 2;
	}

	// The card's parts for a person.
	private void Fill( PortraitInfo person )
	{
		DeleteChildren( true );
		var name = person.Name ?? person.Username ?? person.Id;

		var head = AddChild<Panel>( "head" );
		var picture = head.AddChild<Panel>( "picture" );
		picture.AddChild<Label>( "initials" ).Text = Initials( name );
		var texture = Picture( person );
		if ( texture is not null )
		{
			var image = picture.AddChild<Image>( "photo" );
			image.Texture = texture;
		}
		var names = head.AddChild<Panel>( "names" );
		names.AddChild<Label>( "name" ).Text = name;
		if ( person.Username is not null && person.Username != name )
			names.AddChild<Label>( "handle" ).Text = $"@{person.Username}";

		if ( person.Description is not null )
			AddChild<Label>( "about" ).Text = person.Description;

		var figures = new List<(string Value, string Label)>();
		if ( person.Tenure is not null )
			figures.Add( (Short( person.Tenure ), "Member") );
		if ( person.Posts > 0 )
			figures.Add( (person.Posts.ToString(), person.Posts == 1 ? "Post" : "Posts") );
		if ( person.MainOrganization is not null )
			figures.Add( (person.MainOrganization, "Team") );
		if ( figures.Count > 0 )
		{
			var stats = AddChild<Panel>( "stats" );
			foreach ( var (value, label) in figures )
			{
				var stat = stats.AddChild<Panel>( "stat" );
				stat.AddChild<Label>( "value" ).Text = value;
				stat.AddChild<Label>( "caption" ).Text = label;
			}
		}

		// What they work on, as one line: "programming · gameplay · 3D modelling".
		var expertise = person.Expertise?.Where( item => !string.IsNullOrWhiteSpace( item ) ).ToArray();
		if ( expertise is { Length: > 0 } )
			Section( "Works on", "tags", "tag", new[] { string.Join( "  ·  ", expertise ) } );
		Section( "Roles", "roles", "role", person.Roles );
		Section( "Find them", "links", "link", Links( person ) );
	}

	// A titled part of the card with an item for each of items, left out without any.
	private void Section( string title, string holder, string kind, IEnumerable<string> items )
	{
		var list = items?.Where( item => !string.IsNullOrWhiteSpace( item ) ).ToArray();
		if ( list is not { Length: > 0 } )
			return;
		var section = AddChild<Panel>( "section" );
		section.AddChild<Label>( "heading" ).Text = title;
		var body = section.AddChild<Panel>( holder );
		foreach ( var item in list )
			body.AddChild<Label>( kind ).Text = item;
	}

	// Their picture: the portrait's own image, else their Steam avatar, else none (the initials show).
	private static Texture Picture( PortraitInfo person )
	{
		if ( person.PictureUrl is not null )
			return Texture.Load( person.PictureUrl, false );
		return person.SteamId != 0 ? Texture.LoadAvatar( person.SteamId, 184 ) : null;
	}

	// "3 years, 2 months" as "3y 2m".
	private static string Short( string tenure ) => tenure
		.Replace( " years", "y" ).Replace( " year", "y" ).Replace( " months", "m" ).Replace( " month", "m" )
		.Replace( " days", "d" ).Replace( " day", "d" ).Replace( ",", "" );

	private static IEnumerable<string> Links( PortraitInfo person )
	{
		if ( person.SboxProfile is not null )
			yield return Plain( person.SboxProfile );
		if ( person.SteamUrl is not null )
			yield return "Steam";
		if ( person.GithubUrl is not null )
			yield return "GitHub";
	}

	// An address without its scheme or a trailing slash: sbox.game/u/name.
	private static string Plain( string address ) =>
		address.Replace( "https://", "" ).Replace( "http://", "" ).TrimEnd( '/' );

	private static string Initials( string name ) =>
		new string( name.Split( ' ', '_', '-', '.' ).Where( w => w.Length > 0 ).Take( 2 )
			.Select( w => char.ToUpperInvariant( w[ 0 ] ) ).ToArray() );
}
