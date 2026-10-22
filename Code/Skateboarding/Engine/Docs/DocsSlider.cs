using Sandbox;
using Sandbox.UI;

namespace Skateboarding.Engine.Docs;

/// <summary>
/// A slider in the board editor's look: a thin grey rail, white up to the value, and a white knob. Pressed anywhere along
/// it, the knob jumps there and follows the mouse while the button is held.
/// </summary>
internal sealed class DocsSlider : Panel
{
	// Its width (pixels, as the .slider rule in DocsStyles has it) and the steps a value snaps to.
	private const float Width = 260f;
	private const float Step = 0.05f;

	private readonly Panel _fill;
	private readonly Panel _knob;
	private readonly float _most;

	/// <summary>The value, 0 to the most it was made with.</summary>
	internal float Value { get; private set; }

	/// <summary>Raised as the value changes under the mouse.</summary>
	internal System.Action<float> Changed { get; set; }

	/// <summary>Raised as the mouse button is let go.</summary>
	internal System.Action Released { get; set; }

	internal DocsSlider( float value, float most )
	{
		AddClass( "slider" );
		AddChild<Panel>( "rail" );
		_fill = AddChild<Panel>( "fill" );
		_knob = AddChild<Panel>( "knob" );
		_most = most;
		Show( value );
	}

	protected override void OnMouseDown( MousePanelEvent e )
	{
		base.OnMouseDown( e );
		if ( e.MouseButton == MouseButtons.Left )
			Follow();
	}

	// Held down (the panel is active while its press lasts, wherever the mouse goes), the knob follows the mouse.
	protected override void OnMouseMove( MousePanelEvent e )
	{
		base.OnMouseMove( e );
		if ( HasActive && e.MouseButton != MouseButtons.Middle )
			Follow();
	}

	protected override void OnMouseUp( MousePanelEvent e )
	{
		base.OnMouseUp( e );
		if ( e.MouseButton == MouseButtons.Left )
			Released?.Invoke();
	}

	// The value under the mouse, snapped to a step.
	private void Follow()
	{
		if ( Box.Right <= Box.Left )
			return;
		var share = Math.Clamp( (Mouse.Position.x - Box.Left) / (Box.Right - Box.Left), 0f, 1f );
		var value = MathF.Round( share * _most / Step ) * Step;
		if ( MathF.Abs( value - Value ) < Step * 0.5f )
			return;
		Show( value );
		Changed?.Invoke( value );
	}

	private void Show( float value )
	{
		Value = Math.Clamp( value, 0f, _most );
		var along = Value / _most * Width;
		_fill.Style.Width = Length.Pixels( along );
		_knob.Style.Left = Length.Pixels( along );
	}
}
