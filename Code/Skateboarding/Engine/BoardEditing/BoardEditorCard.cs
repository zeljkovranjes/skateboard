using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The card that tells who a picked portrait shows: their avatar, name and username, a line about them, how long they
/// have been around and how much they have posted, what they work on, their roles and their public links. It glides up
/// and in beside the board as a portrait is picked, its lines following one after another, and out again as the
/// portrait is let go. Plain, quiet type on frosted glass: the board stays what the eye goes to.
/// </summary>
internal sealed class BoardEditorCard : Panel
{
	// Seconds the card is out for as it changes from one person to the next.
	private const float ChangeOver = 0.14f;

	private bool _comingBack;
	private PortraitInfo _next;
	private RealTimeSince _sinceOut;
	private readonly Image _avatar;
	private readonly Label _initials;
	private readonly Label _name;
	private readonly Label _username;
	private readonly Label _about;
	private readonly Label _facts;
	private readonly Panel _tags;
	private readonly Panel _roles;
	private readonly Panel _links;

	public BoardEditorCard()
	{
		var head = AddChild<Panel>( "head" );
		var portrait = head.AddChild<Panel>( "avatar" );
		_initials = portrait.AddChild<Label>( "initials" );
		_avatar = portrait.AddChild<Image>( "picture" );
		var names = head.AddChild<Panel>( "names" );
		_name = names.AddChild<Label>( "name" );
		_username = names.AddChild<Label>( "username" );
		_about = AddChild<Label>( "about" );
		_facts = AddChild<Label>( "facts" );
		_tags = AddChild<Panel>( "tags" );
		_roles = AddChild<Panel>( "roles" );
		_links = AddChild<Panel>( "links" );
	}

	/// <summary>True while the card is shown, or coming back in with someone new.</summary>
	internal bool Showing => HasClass( "shown" ) || _comingBack;

	/// <summary>Shows who <paramref name="person"/> is (gliding in, or changing over if the card is up already).</summary>
	internal void Show( PortraitInfo person )
	{
		// Changing over from someone else: the card starts out and comes back in with the new person, so the change reads.
		if ( Showing )
		{
			_next = person;
			_comingBack = true;
			_sinceOut = 0f;
			SetClass( "shown", false );
			return;
		}
		Fill( person );
		SetClass( "shown", true );
	}

	internal void Hide()
	{
		_comingBack = false;
		_next = null;
		SetClass( "shown", false );
	}

	public override void Tick()
	{
		base.Tick();
		if ( !_comingBack || _sinceOut < ChangeOver )
			return;
		_comingBack = false;
		if ( _next is not null )
			Fill( _next );
		_next = null;
		SetClass( "shown", true );
	}

	// The card's lines for a person.
	private void Fill( PortraitInfo person )
	{
		_name.Text = person.Name ?? person.Id;
		_username.Text = person.Username is null ? "" : $"@{person.Username}";
		_about.Text = person.Description ?? "";
		_facts.Text = Facts( person );
		_initials.Text = Initials( person.Name ?? person.Username ?? "?" );
		var picture = person.SteamId != 0 ? Texture.LoadAvatar( person.SteamId, 128 ) : null;
		_avatar.Texture = picture;
		_avatar.SetClass( "loaded", picture is not null );
		Items( _tags, "tag", person.Expertise );
		Items( _roles, "role", person.Roles );
		Items( _links, "link", Links( person ) );
	}

	private static string Facts( PortraitInfo person )
	{
		var parts = new List<string>();
		if ( person.Tenure is not null )
			parts.Add( $"Member for {person.Tenure}" );
		if ( person.Posts > 0 )
			parts.Add( person.Posts == 1 ? "1 forum post" : $"{person.Posts} forum posts" );
		if ( person.MainOrganization is not null )
			parts.Add( person.MainOrganization );
		return string.Join( "  ·  ", parts );
	}

	private static IEnumerable<string> Links( PortraitInfo person )
	{
		if ( person.SboxProfile is not null )
			yield return Plain( person.SboxProfile );
		if ( person.SteamUrl is not null )
			yield return Plain( person.SteamUrl );
		if ( person.GithubUrl is not null )
			yield return Plain( person.GithubUrl );
	}

	// An address without its scheme or a trailing slash: sbox.game/u/name.
	private static string Plain( string address ) =>
		address.Replace( "https://", "" ).Replace( "http://", "" ).TrimEnd( '/' );

	private static string Initials( string name ) =>
		new string( name.Split( ' ', '_', '-', '.' ).Where( w => w.Length > 0 ).Take( 2 )
			.Select( w => char.ToUpperInvariant( w[ 0 ] ) ).ToArray() );

	private static void Items( Panel holder, string kind, IEnumerable<string> items )
	{
		holder.DeleteChildren( true );
		var any = false;
		foreach ( var item in items ?? Enumerable.Empty<string>() )
		{
			holder.AddChild<Label>( kind ).Text = item;
			any = true;
		}
		holder.SetClass( "empty", !any );
	}
}
