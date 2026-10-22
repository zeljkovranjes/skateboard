using System;
using System.Linq;
using Sandbox;

namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor's scene: a copy of the carried board's drawn models (each renderer with its model, material group,
/// material overrides, body groups and tint, placed as it is on the board) on a turntable in the middle, lit by a key, a
/// fill and a rim light, and seen by a camera that clears to nothing, so the blurred game shows all round the board. The
/// copy is centred on the turntable, and the camera stands far enough back to see all of it. The lights and the camera
/// stay put while the turntable turns, so the board turns under the same light, as a board turned in the hands does.
/// </summary>
internal sealed class BoardEditorStage
{
	// The camera's field of view (degrees): long, as for a product shot, so a board seen close is not stretched.
	private const float FieldOfView = 30f;
	// Room round the board in the view, as a share of what just fits it.
	private const float Margin = 1.3f;

	private readonly Scene _scene;
	private readonly GameObject _turntable;
	private readonly GameObject _camera;
	private GameObject _copy;

	internal BoardEditorStage( Scene scene )
	{
		_scene = scene;
		using ( scene.Push() )
		{
			_camera = scene.CreateObject();
			_camera.Name = "Camera";
			var camera = _camera.Components.Create<CameraComponent>();
			camera.BackgroundColor = Color.Transparent;
			camera.FieldOfView = FieldOfView;
			camera.ZNear = 1f;
			camera.ZFar = 4000f;
			_turntable = scene.CreateObject();
			_turntable.Name = "Turntable";
			// Key light from above and to the right of the camera, a cool fill low from the left, a warm rim from behind.
			Light( Rotation.From( 50f, 25f, 0f ), new Color( 1f, 0.97f, 0.92f ) * 2.2f );
			Light( Rotation.From( 15f, -70f, 0f ), new Color( 0.75f, 0.82f, 1f ) * 0.7f );
			Light( Rotation.From( 30f, 200f, 0f ), new Color( 1f, 0.9f, 0.8f ) * 1.1f );
			var ambient = scene.CreateObject();
			ambient.Name = "Ambient";
			ambient.Components.Create<AmbientLight>().Color = new Color( 0.32f, 0.34f, 0.38f );
		}
	}

	/// <summary>How far back (inches) the camera stands to see the whole board with room round it.</summary>
	internal float FitDistance { get; private set; } = 60f;

	/// <summary>The camera's field of view (degrees).</summary>
	internal static float Fov => FieldOfView;

	/// <summary>
	/// Puts a copy of what is drawn of <paramref name="board"/> on the turntable, in place of any earlier copy, centred on
	/// it, and works out how far back the camera stands to see it all.
	/// </summary>
	internal void Build( GameObject board )
	{
		using ( _scene.Push() )
		{
			if ( _copy.IsValid() )
				_copy.Destroy();
			_copy = new GameObject( _turntable, true, "Board" );
			// The parts are placed as they are about the drawn deck (or the board, without one), at full size.
			var deck = board.Children.FirstOrDefault( child => child.Name == "Generated Skateboard" ) ?? board;
			var origin = deck.WorldTransform.WithScale( 1f );
			var bounds = default( BBox );
			var any = false;
			foreach ( var source in board.Components.GetAll<ModelRenderer>( FindMode.EnabledInSelfAndDescendants ) )
			{
				if ( source.Model is null )
					continue;
				var part = new GameObject( _copy, true, source.GameObject.Name )
				{
					LocalTransform = origin.ToLocal( source.WorldTransform )
				};
				Copy( source, part );
				var box = source.Model.Bounds.Transform( part.LocalTransform );
				bounds = any ? bounds.AddBBox( box ) : box;
				any = true;
			}
			var centre = any ? bounds.Center : Vector3.Zero;
			_copy.LocalPosition = -centre;
			var radius = (any ? bounds.Size.Length : 34f) * 0.5f;
			FitDistance = radius / MathF.Tan( FieldOfView * 0.5f * MathF.PI / 180f ) * Margin;
		}
	}

	/// <summary>
	/// Turns the board (degrees about the world's up, then about the view's right), moves it across the view (inches;
	/// right and up) and stands the camera <paramref name="distance"/> inches back from it.
	/// </summary>
	internal void Show( float yaw, float pitch, float distance, Vector2 shift )
	{
		_turntable.WorldRotation = Rotation.FromAxis( Vector3.Right, pitch ) * Rotation.FromAxis( Vector3.Up, yaw );
		_turntable.WorldPosition = new Vector3( 0f, -shift.x, shift.y );
		_camera.WorldPosition = new Vector3( -distance, 0f, 0f );
		_camera.WorldRotation = Rotation.Identity;
	}

	// A light shining along the rotation's forward.
	private void Light( Rotation rotation, Color color )
	{
		var light = _scene.CreateObject();
		light.Name = "Light";
		light.WorldRotation = rotation;
		var directional = light.Components.Create<DirectionalLight>();
		directional.LightColor = color;
		directional.Shadows = false;
	}

	// A renderer copied onto the part: its model and everything that changes how it is drawn.
	private static ModelRenderer Copy( ModelRenderer source, GameObject part )
	{
		ModelRenderer copy;
		if ( source is SkinnedModelRenderer )
		{
			var skinned = part.Components.Create<SkinnedModelRenderer>();
			skinned.UseAnimGraph = false;
			copy = skinned;
		}
		else
		{
			copy = part.Components.Create<ModelRenderer>();
		}
		copy.Model = source.Model;
		copy.MaterialGroup = source.MaterialGroup;
		copy.BodyGroups = source.BodyGroups;
		copy.Tint = source.Tint;
		copy.MaterialOverride = source.MaterialOverride;
		for ( var slot = 0; slot < source.Materials.Count; slot++ )
		{
			if ( source.Materials.HasOverride( slot ) )
				copy.Materials.SetOverride( slot, source.Materials.GetOverride( slot ) );
		}
		return copy;
	}
}
