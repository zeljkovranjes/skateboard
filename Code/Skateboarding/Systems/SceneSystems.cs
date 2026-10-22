using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Skateboarding.Components;

namespace Skateboarding.Systems;

/// <summary>
/// The addon's scene systems, for a scene that started before the addon's code was loaded. The engine makes a scene's
/// systems as the scene starts, from the code loaded then; the skateboard spawned from Sandbox's spawn menu brings its
/// package's code into a game already running, whose scene never gets them (the rider found no animation library, and
/// the sounds, the board's finish, the docs and the ride keys never ran). <see cref="Ensure"/> makes the ones a scene
/// lacks, and <see cref="Get{T}"/> finds a system either way: the scene's own, or one made here.
/// </summary>
internal static class SceneSystems
{
	private static readonly Dictionary<Scene, Dictionary<Type, GameObjectSystem>> Made = new();

	/// <summary>The scene's system of this type, made first if the scene has none.</summary>
	internal static T Get<T>( Scene scene ) where T : GameObjectSystem
	{
		if ( scene is null )
			return null;
		if ( scene.GetSystem<T>() is { } system )
			return system;
		Ensure( scene );
		return Made.TryGetValue( scene, out var made ) && made.TryGetValue( typeof( T ), out var own ) ? own as T : null;
	}

	/// <summary>Makes each of the addon's systems the scene lacks.</summary>
	internal static void Ensure( Scene scene )
	{
		if ( !scene.IsValid() || Made.ContainsKey( scene ) )
			return;
		Forget();
		var made = new Dictionary<Type, GameObjectSystem>();
		Made[ scene ] = made;
		using ( scene.Push() )
		{
			Add( scene, made, s => new GrindRails( s ) );
			Add( scene, made, s => new RiderAnimationLibrary( s ) );
			Add( scene, made, s => new RiderLegReader( s ) );
			Add( scene, made, s => new RideKeys( s ) );
			Add( scene, made, s => new BoardRecall( s ) );
			Add( scene, made, s => new BoardFinishSystem( s ) );
			Add( scene, made, s => new BoardEditorSystem( s ) );
			Add( scene, made, s => new SkateDocsSystem( s ) );
			Add( scene, made, s => new SkateSounds( s ) );
			Add( scene, made, s => new BrakeSmoke( s ) );
		}
	}

	private static void Add<T>( Scene scene, Dictionary<Type, GameObjectSystem> made, Func<Scene, T> create ) where T : GameObjectSystem
	{
		if ( scene.GetSystem<T>() is null )
			made[ typeof( T ) ] = create( scene );
	}

	// Lets go of the systems made for scenes that have since gone.
	private static void Forget()
	{
		foreach ( var scene in Made.Keys.Where( scene => !scene.IsValid() ).ToArray() )
		{
			foreach ( var system in Made[ scene ].Values )
				system.Dispose();
			Made.Remove( scene );
		}
	}
}
