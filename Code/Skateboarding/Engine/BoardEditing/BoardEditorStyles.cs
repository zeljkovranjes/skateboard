namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor's look: the game behind softly blurred, darkened and a little greyed, so nothing behind the board
/// pulls at the eye or flickers, and the board alone, sharp, in the middle. Who a picked portrait shows stands in the
/// right half, centred top to bottom, as words alone with nothing behind them: a large name, figures set large over small
/// capital labels, all in white and greys with a soft shadow under the letters and no accent colour. It comes in from the
/// right, each part a beat after the one before, and goes out the same way. The modes stand along the top in the middle,
/// words alone in the same type: the one in use white over a thin underline that grows out from its middle, the other
/// grey, lighter under the mouse.
/// </summary>
internal static class BoardEditorStyles
{
	/// <summary>
	/// Raised whenever the look or the layout changes: an editor panel made before is made again as it next opens (a
	/// panel parses its styles once, as it is made).
	/// </summary>
	internal const int Version = 5;

	internal const string Sheet = """
		BoardEditor
		{
			position: absolute;
			left: 0;
			top: 0;
			right: 0;
			bottom: 0;
			display: none;
			flex-direction: column;
			backdrop-filter: blur( 14px ) brightness( 0.6 ) saturate( 0.75 );
			background-color: rgba( 14, 16, 20, 0.35 );
			font-family: Inter;
			pointer-events: all;
		}
		BoardEditor.open
		{
			display: flex;
		}
		BoardEditor.pointing
		{
			cursor: pointer;
		}
		BoardEditor .view
		{
			position: absolute;
			left: 0;
			top: 0;
			right: 0;
			bottom: 0;
			pointer-events: none;
		}
		BoardEditor .modes
		{
			position: absolute;
			top: 30px;
			left: 0;
			right: 0;
			flex-direction: row;
			justify-content: center;
			pointer-events: none;
		}
		BoardEditor .mode
		{
			flex-direction: column;
			align-items: center;
			margin: 0px 20px;
			padding: 6px 2px;
			color: rgba( 255, 255, 255, 0.45 );
			cursor: pointer;
			pointer-events: all;
			transition: color 0.2s ease-out;
		}
		BoardEditor .mode:hover
		{
			color: rgba( 255, 255, 255, 0.75 );
		}
		BoardEditor .mode.active
		{
			color: white;
		}
		BoardEditor .mode-name
		{
			font-size: 20px;
			font-weight: 600;
			letter-spacing: 0.2px;
			text-shadow: 0px 2px 14px rgba( 0, 0, 0, 0.6 );
		}
		BoardEditor .mode .underline
		{
			margin-top: 7px;
			width: 0%;
			height: 2px;
			border-radius: 1px;
			background-color: white;
			transition: width 0.3s ease-out;
		}
		BoardEditor .mode.active .underline
		{
			width: 100%;
		}
		BoardEditor .card-slot
		{
			position: absolute;
			top: 0;
			bottom: 0;
			left: 54%;
			right: 64px;
			flex-direction: column;
			justify-content: center;
			pointer-events: none;
		}
		BoardEditor .card
		{
			flex-direction: column;
			max-width: 560px;
			color: white;
			opacity: 0;
			transform: translateX( 140px );
			transition: opacity 0.35s ease-out, transform 0.6s ease-out;
			pointer-events: none;
		}
		BoardEditor .card.shown
		{
			opacity: 1;
			transform: translateX( 0px );
			pointer-events: all;
		}
		BoardEditor .card label
		{
			text-shadow: 0px 2px 18px rgba( 0, 0, 0, 0.6 );
		}
		BoardEditor .card > .head, BoardEditor .card > .about, BoardEditor .card > .stats, BoardEditor .card > .section
		{
			opacity: 0;
			transform: translateX( 70px );
			transition: opacity 0.4s ease-out, transform 0.6s ease-out;
		}
		BoardEditor .card.shown > .head, BoardEditor .card.shown > .about, BoardEditor .card.shown > .stats,
		BoardEditor .card.shown > .section
		{
			opacity: 1;
			transform: translateX( 0px );
		}
		BoardEditor .card.shown > .about { transition-delay: 0.06s; }
		BoardEditor .card.shown > .stats { transition-delay: 0.12s; }
		BoardEditor .card.shown > .section { transition-delay: 0.18s; }
		BoardEditor .card .head
		{
			flex-direction: row;
			align-items: center;
		}
		BoardEditor .card .picture
		{
			width: 92px;
			height: 92px;
			flex-shrink: 0;
			border-radius: 26px;
			overflow: hidden;
			justify-content: center;
			align-items: center;
			background-color: rgba( 255, 255, 255, 0.06 );
			box-shadow: 0px 10px 30px rgba( 0, 0, 0, 0.35 );
		}
		BoardEditor .card .initials
		{
			font-size: 34px;
			font-weight: 700;
			color: rgba( 255, 255, 255, 0.5 );
		}
		BoardEditor .card .photo
		{
			position: absolute;
			left: 0;
			top: 0;
			width: 100%;
			height: 100%;
		}
		BoardEditor .card .names
		{
			flex-direction: column;
			flex-shrink: 1;
			margin-left: 22px;
		}
		BoardEditor .card .name
		{
			font-size: 48px;
			font-weight: 800;
			letter-spacing: -1px;
			color: white;
		}
		BoardEditor .card .handle
		{
			font-size: 18px;
			color: rgba( 255, 255, 255, 0.5 );
		}
		BoardEditor .card .about
		{
			margin-top: 26px;
			font-size: 21px;
			line-height: 1.45;
			color: rgba( 255, 255, 255, 0.86 );
		}
		BoardEditor .card .stats
		{
			flex-direction: row;
			margin-top: 30px;
		}
		BoardEditor .card .stat
		{
			flex-direction: column;
			margin-right: 48px;
		}
		BoardEditor .card .value
		{
			font-size: 34px;
			font-weight: 700;
			letter-spacing: -0.5px;
			color: white;
		}
		BoardEditor .card .caption, BoardEditor .card .heading
		{
			font-size: 12px;
			font-weight: 600;
			letter-spacing: 1.4px;
			text-transform: uppercase;
			color: rgba( 255, 255, 255, 0.45 );
		}
		BoardEditor .card .section
		{
			flex-direction: column;
			margin-top: 28px;
		}
		BoardEditor .card .heading
		{
			margin-bottom: 8px;
		}
		BoardEditor .card .tags, BoardEditor .card .roles
		{
			flex-direction: column;
		}
		BoardEditor .card .tag
		{
			font-size: 19px;
			color: rgba( 255, 255, 255, 0.88 );
		}
		BoardEditor .card .role
		{
			margin-bottom: 3px;
			font-size: 19px;
			color: rgba( 255, 255, 255, 0.82 );
		}
		BoardEditor .card .links
		{
			flex-direction: row;
			flex-wrap: wrap;
		}
		BoardEditor .card .link
		{
			margin-right: 26px;
			font-size: 18px;
			font-weight: 600;
			color: rgba( 255, 255, 255, 0.72 );
		}
		""";
}
