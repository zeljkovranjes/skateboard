namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor's look: the game behind softly blurred, darkened and a little greyed, so nothing behind the board
/// pulls at the eye or flickers, and the board alone, sharp, in the middle. Who a picked portrait shows stands in the
/// right half, centred top to bottom, as words alone with nothing behind them: a large name, figures set large over small
/// capital labels, all in white and greys with a soft shadow under the letters and no accent colour. It comes in from the
/// right, each part a beat after the one before, and goes out the same way. The modes stand along the top in the middle,
/// words alone in the same type: the one in use white over a thin underline that grows out from its middle, the other
/// grey, lighter under the mouse. The board's settings (Edit) take the same place and come in the same way, in the same
/// type: words to pick underlined like the modes, round swatches ringed in white when picked, a thin line with a round
/// knob for the speed.
/// </summary>
internal static class BoardEditorStyles
{
	/// <summary>
	/// Raised whenever the look or the layout changes: an editor panel made before is made again as it next opens (a
	/// panel parses its styles once, as it is made).
	/// </summary>
	internal const int Version = 7;

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
		BoardEditor .card-slot, BoardEditor .edit-slot
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
		BoardEditor .edit
		{
			flex-direction: column;
			max-width: 560px;
			color: white;
			opacity: 0;
			transform: translateX( 140px );
			transition: opacity 0.35s ease-out, transform 0.6s ease-out;
			pointer-events: none;
		}
		BoardEditor .edit.shown
		{
			opacity: 1;
			transform: translateX( 0px );
			pointer-events: all;
		}
		BoardEditor .edit label
		{
			text-shadow: 0px 2px 18px rgba( 0, 0, 0, 0.6 );
		}
		BoardEditor .edit > .head, BoardEditor .edit > .section, BoardEditor .edit > .actions
		{
			opacity: 0;
			transform: translateX( 70px );
			transition: opacity 0.4s ease-out, transform 0.6s ease-out;
		}
		BoardEditor .edit.shown > .head, BoardEditor .edit.shown > .section, BoardEditor .edit.shown > .actions
		{
			opacity: 1;
			transform: translateX( 0px );
		}
		BoardEditor .edit.shown > .beat1 { transition-delay: 0.04s; }
		BoardEditor .edit.shown > .beat2 { transition-delay: 0.08s; }
		BoardEditor .edit.shown > .beat3 { transition-delay: 0.12s; }
		BoardEditor .edit.shown > .beat4 { transition-delay: 0.16s; }
		BoardEditor .edit.shown > .beat5 { transition-delay: 0.2s; }
		BoardEditor .edit.shown > .beat6 { transition-delay: 0.24s; }
		BoardEditor .edit.shown > .beat7 { transition-delay: 0.28s; }
		BoardEditor .edit .head
		{
			flex-direction: column;
		}
		BoardEditor .edit .title
		{
			font-size: 48px;
			font-weight: 800;
			letter-spacing: -1px;
			color: white;
		}
		BoardEditor .edit .subtitle
		{
			font-size: 18px;
			color: rgba( 255, 255, 255, 0.5 );
		}
		BoardEditor .edit .section
		{
			flex-direction: column;
			margin-top: 24px;
		}
		BoardEditor .edit .top
		{
			flex-direction: row;
			align-items: center;
			margin-bottom: 10px;
		}
		BoardEditor .edit .heading
		{
			font-size: 12px;
			font-weight: 600;
			letter-spacing: 1.4px;
			text-transform: uppercase;
			color: rgba( 255, 255, 255, 0.45 );
		}
		BoardEditor .edit .value
		{
			margin-left: 12px;
			font-size: 14px;
			color: rgba( 255, 255, 255, 0.72 );
		}
		BoardEditor .edit .choices, BoardEditor .edit .actions
		{
			flex-direction: row;
		}
		BoardEditor .edit .choice, BoardEditor .edit .action
		{
			flex-direction: column;
			align-items: center;
			margin-right: 26px;
			color: rgba( 255, 255, 255, 0.45 );
			cursor: pointer;
			transition: color 0.2s ease-out;
		}
		BoardEditor .edit .choice:hover, BoardEditor .edit .action:hover
		{
			color: rgba( 255, 255, 255, 0.75 );
		}
		BoardEditor .edit .choice.picked, BoardEditor .edit .action.main
		{
			color: white;
		}
		BoardEditor .edit .choice-name, BoardEditor .edit .action-name
		{
			font-size: 19px;
			font-weight: 600;
		}
		BoardEditor .edit .underline
		{
			margin-top: 6px;
			width: 0%;
			height: 2px;
			border-radius: 1px;
			background-color: white;
			transition: width 0.3s ease-out;
		}
		BoardEditor .edit .choice.picked .underline, BoardEditor .edit .action.main .underline,
		BoardEditor .edit .action:hover .underline
		{
			width: 100%;
		}
		BoardEditor .edit .actions
		{
			margin-top: 34px;
		}
		BoardEditor .edit .swatches
		{
			flex-direction: row;
			flex-wrap: wrap;
		}
		BoardEditor .edit .swatch
		{
			width: 36px;
			height: 36px;
			margin-right: 8px;
			padding: 4px;
			border-radius: 18px;
			border: 2px solid rgba( 255, 255, 255, 0 );
			cursor: pointer;
			transition: transform 0.2s ease-out;
		}
		BoardEditor .edit .swatch:hover
		{
			transform: scale( 1.1 );
		}
		BoardEditor .edit .swatch.picked
		{
			border: 2px solid white;
		}
		BoardEditor .edit .dot
		{
			flex-grow: 1;
			border-radius: 14px;
			box-shadow: 0px 2px 10px rgba( 0, 0, 0, 0.35 );
		}
		BoardEditor .edit .slider
		{
			position: relative;
			width: 260px;
			height: 24px;
			cursor: pointer;
		}
		BoardEditor .edit .rail, BoardEditor .edit .fill
		{
			position: absolute;
			left: 0;
			top: 11px;
			height: 2px;
			border-radius: 1px;
		}
		BoardEditor .edit .rail
		{
			right: 0;
			background-color: rgba( 255, 255, 255, 0.22 );
		}
		BoardEditor .edit .fill
		{
			background-color: white;
		}
		BoardEditor .edit .knob
		{
			position: absolute;
			top: 5px;
			width: 14px;
			height: 14px;
			margin-left: -7px;
			border-radius: 7px;
			background-color: white;
			box-shadow: 0px 2px 8px rgba( 0, 0, 0, 0.4 );
			transition: transform 0.15s ease-out;
		}
		BoardEditor .edit .slider:hover .knob
		{
			transform: scale( 1.2 );
		}
		""";
}
