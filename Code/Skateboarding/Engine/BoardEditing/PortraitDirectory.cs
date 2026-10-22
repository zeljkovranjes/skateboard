using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Sandbox;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The community portraits' people, read once from the portrait layer's data (portraits.json beside its prefab) and
/// found by portrait id, which is also the name of each portrait's object in the prefab. Without the portrait layer in
/// the game there are none, and the board editor shows the board without them.
/// </summary>
internal static class PortraitDirectory
{
	/// <summary>The portrait layer's prefab: one object per portrait, named by its id, each with its own model.</summary>
	internal const string PrefabPath = "skater/community_portraits/community_portraits.prefab";

	private const string DataPath = "skater/community_portraits/portraits.json";

	// Made on first use (a reload keeps a static's value, so this is never a readonly field set once), and read again
	// when Version changes: people read by an earlier version keep its shape.
	private const int Version = 2;
	private static int _version;
	private static Dictionary<string, PortraitInfo> _byId;

	/// <summary>The person a portrait id shows, or null.</summary>
	internal static PortraitInfo Find( string id )
	{
		Load();
		return id is not null && _byId.TryGetValue( id, out var info ) ? info : null;
	}

	private static void Load()
	{
		if ( _byId is not null && _version == Version )
			return;
		_version = Version;
		_byId = new Dictionary<string, PortraitInfo>( StringComparer.OrdinalIgnoreCase );
		try
		{
			if ( !FileSystem.Mounted.FileExists( DataPath ) )
				return;
			if ( Json.ParseToJsonObject( FileSystem.Mounted.ReadAllText( DataPath ) )?[ "portraits" ] is not JsonArray portraits )
				return;
			foreach ( var node in portraits )
			{
				if ( node is JsonObject portrait && Read( portrait ) is { Id: not null } info )
					_byId[ info.Id ] = info;
			}
		}
		catch ( Exception )
		{
			// Unreadable data leaves the portraits without cards; the board editor still works.
		}
	}

	private static PortraitInfo Read( JsonObject p ) => new(
		Text( p, "portrait_id" ),
		Text( p, "display_name" ) ?? Text( p, "sbox_username" ),
		Text( p, "sbox_username" ),
		Text( p, "description" ),
		Text( p, "sbox_member_duration" ),
		p[ "sbox_forum_posts" ] is JsonValue posts && posts.TryGetValue<int>( out var count ) ? count : 0,
		Texts( p[ "expertise" ] ),
		Text( p, "main_organization" ),
		Roles( p[ "memberships" ] ),
		long.TryParse( Text( p, "steam_id64" ), out var steam ) ? steam : 0,
		Text( p, "portrait_url" ),
		Text( p, "sbox_profile" ),
		Text( p, "steam_url" ),
		Text( p, "github_url" ) );

	private static string Text( JsonObject o, string key ) =>
		o[ key ] is JsonValue value && value.TryGetValue<string>( out var text ) && !string.IsNullOrWhiteSpace( text )
			? text.Trim()
			: null;

	private static string[] Texts( JsonNode node ) => node is JsonArray list
		? list.OfType<JsonValue>().Select( v => v.TryGetValue<string>( out var t ) ? t?.Trim() : null )
			.Where( t => !string.IsNullOrEmpty( t ) ).ToArray()
		: Array.Empty<string>();

	// "Owner of notpointless": a role and the organisation, its name the last part of its address.
	private static string[] Roles( JsonNode node ) => node is JsonArray list
		? list.OfType<JsonObject>()
			.Select( m => (Role: Text( m, "role" ), Organization: Text( m, "organization" )) )
			.Where( m => m.Role is not null && m.Organization is not null )
			.Select( m => $"{m.Role} of {m.Organization.TrimEnd( '/' ).Split( '/' ).Last()}" )
			.Distinct().ToArray()
		: Array.Empty<string>();
}
