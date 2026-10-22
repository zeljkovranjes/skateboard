using Sandbox.UI;
using Skateboarding.Core.Sound;

namespace Skateboarding.Engine.Docs;

/// <summary>
/// The sound page: how loud the boards are, all their sounds together and the rolling, tricks and grinds on their own,
/// each a slider from silent to half again as loud as made. A change is heard at once and saved as the slider is let go;
/// Reset puts every volume back.
/// </summary>
internal sealed class DocsSound : Panel
{
	public DocsSound()
	{
		Style.FlexDirection = FlexDirection.Column;
		Build();
	}

	private void Build()
	{
		DeleteChildren( true );
		var bar = AddChild<Panel>( "toolbar" );
		var reset = bar.AddChild<Label>( "link" );
		reset.Text = "Reset to defaults";
		reset.AddEventListener( "onclick", () =>
		{
			SkateSoundSettings.Reset();
			Build();
		} );
		bar.AddChild<Label>( "muted" ).Text = "Changes are heard at once and saved straight away.";
		var mix = SkateSoundSettings.Mix;
		AddChild<Label>( "h3" ).Text = "Volume";
		Row( "All board sounds", "Every sound the boards make", mix.All, ( m, v ) => m with { All = v } );
		Row( "Rolling", "Wheels, pushes, powerslides and braking", mix.Rolling, ( m, v ) => m with { Rolling = v } );
		Row( "Tricks", "Pops, flips, catches and landings", mix.Tricks, ( m, v ) => m with { Tricks = v } );
		Row( "Grinds", "Grinds, slides and rail hits", mix.Grinds, ( m, v ) => m with { Grinds = v } );
	}

	// A volume's name and what it covers, its slider and its value as a percentage.
	private void Row( string title, string covers, float volume, System.Func<SkateSoundMix, float, SkateSoundMix> set )
	{
		var row = AddChild<Panel>( "tr" );
		var name = row.AddChild<Panel>( "td-volume" );
		name.AddChild<Label>( "td-title" ).Text = title;
		name.AddChild<Label>( "td-covers" ).Text = covers;
		var cell = row.AddChild<Panel>( "td-slider" );
		var value = row.AddChild<Label>( "td-value" );
		value.Text = Percent( volume );
		var slider = new DocsSlider( volume, SkateSoundMix.Loudest ) { Parent = cell };
		slider.Changed = v =>
		{
			SkateSoundSettings.Set( set( SkateSoundSettings.Mix, v ) );
			value.Text = Percent( v );
		};
		slider.Released = SkateSoundSettings.Save;
	}

	private static string Percent( float volume ) => $"{MathF.Round( volume * 100f )}%";
}
