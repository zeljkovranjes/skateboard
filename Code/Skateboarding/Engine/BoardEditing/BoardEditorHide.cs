using System.Collections.Generic;
using Sandbox;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// Keeps the player out of sight while the board editor is open: every renderer under the objects given (the player,
/// the rider carrying the board and the board itself) is tagged, and the game's main cameras leave that tag out of
/// what they draw. The tag goes again as the editor closes, so all of it shows as before.
/// </summary>
internal sealed class BoardEditorHide
{
	private const string Tag = "skate_board_editor_hidden";

	private readonly List<GameObject> _tagged = new();
	private Scene _scene;

	/// <summary>Hides every renderer under <paramref name="roots"/> from the main cameras of <paramref name="scene"/>.</summary>
	internal void Hide( Scene scene, IEnumerable<GameObject> roots )
	{
		Show();
		_scene = scene;
		foreach ( var root in roots )
		{
			if ( !root.IsValid() )
				continue;
			foreach ( var renderer in root.Components.GetAll<Renderer>( FindMode.EverythingInSelfAndDescendants ) )
			{
				var holder = renderer.GameObject;
				if ( holder.Tags.Has( Tag ) )
					continue;
				holder.Tags.Add( Tag );
				_tagged.Add( holder );
			}
		}
		Frame();
	}

	/// <summary>
	/// Leaves the tag out of every main camera, each frame the editor is open: a camera can be made or have its excluded
	/// tags set again meanwhile (a first person camera sets its own each frame).
	/// </summary>
	internal void Frame()
	{
		if ( _scene is null || !_scene.IsValid() )
			return;
		foreach ( var camera in _scene.GetAllComponents<CameraComponent>() )
		{
			if ( camera.IsMainCamera && !camera.RenderExcludeTags.Has( Tag ) )
				camera.RenderExcludeTags.Add( Tag );
		}
	}

	/// <summary>Shows everything hidden again.</summary>
	internal void Show()
	{
		foreach ( var holder in _tagged )
		{
			if ( holder.IsValid() )
				holder.Tags.Remove( Tag );
		}
		_tagged.Clear();
		if ( _scene is not null && _scene.IsValid() )
		{
			foreach ( var camera in _scene.GetAllComponents<CameraComponent>() )
				camera.RenderExcludeTags.Remove( Tag );
		}
		_scene = null;
	}
}
