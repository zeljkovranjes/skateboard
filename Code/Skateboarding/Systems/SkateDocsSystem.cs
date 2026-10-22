using Sandbox;
using Skateboarding.Engine;
using Skateboarding.Engine.Docs;

namespace Skateboarding.Systems;

/// <summary>
/// Opens the skateboard docs (<see cref="SkateDocs"/>) with the docs key, F1 unless rebound, or the controller's docs
/// button, on foot or on the board. The docs live on a screen panel of their own, made the first time they open, above
/// the game's HUD and the board's controls card. Once open they have the keyboard and close themselves.
/// </summary>
public sealed class SkateDocsSystem : GameObjectSystem<SkateDocsSystem>
{
	private GameObject _screen;
	private SkateDocs _docs;
	private bool _opening;

	/// <summary>Watches for the docs key every frame.</summary>
	public SkateDocsSystem( Scene scene ) : base( scene ) => Listen( Stage.StartUpdate, -9000, Watch, "SkateDocs" );

	/// <summary>Opens the docs (as the docs key does): this frame, or as soon as their screen panel is up.</summary>
	internal void Open() => _opening = true;

	private void Watch()
	{
		if ( Scene.IsEditor || Application.IsDedicatedServer )
			return;
		// The board editor has the keys while it is open.
		if ( Engine.BoardEditing.BoardEditor.IsOpen )
			return;
		if ( SkateDocs.IsOpen )
		{
			_docs?.Frame();
			return;
		}
		_opening |= SkateBindings.Toggled( "docs" );
		if ( !_opening )
			return;
		// The screen panel makes its root a frame after it is created: the docs open as soon as there is one.
		if ( Docs() is not { } docs )
			return;
		_opening = false;
		docs.Open();
	}

	private SkateDocs Docs()
	{
		if ( !_screen.IsValid() )
		{
			_screen = Scene.CreateObject( true );
			_screen.Name = "Skate docs";
			_screen.NetworkMode = NetworkMode.Never;
			_screen.Flags |= GameObjectFlags.NotSaved;
			var screen = _screen.Components.Create<ScreenPanel>();
			screen.ZIndex = 300;
			screen.ScaleStrategy = ScreenPanel.AutoScale.FollowDesktopScaling;
		}
		if ( _docs is null || !_docs.IsValid )
		{
			var root = _screen.Components.Get<ScreenPanel>()?.GetPanel();
			if ( root is null )
				return null;
			_docs = new SkateDocs { Parent = root };
		}
		return _docs;
	}
}
