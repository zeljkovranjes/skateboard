namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor's look: the game behind softly blurred, darkened and a little greyed, so nothing behind the board
/// pulls at the eye or flickers, and the board alone, sharp, in the middle. A picked portrait's card stands to its right,
/// centred top to bottom: deep frosted glass with a faint sheen at the top, a large picture and name, figures set large
/// over small capital labels, tags and links as pills, all in white and greys with no accent colour. It slides in from
/// the right, each part a beat after the one before.
/// </summary>
internal static class BoardEditorStyles
{
	/// <summary>
	/// Raised whenever the look or the layout changes: an editor panel made before is made again as it next opens (a
	/// panel parses its styles once, as it is made).
	/// </summary>
	internal const int Version = 3;

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
		BoardEditor .card-slot
		{
			position: absolute;
			top: 0;
			bottom: 0;
			right: 56px;
			width: 440px;
			flex-direction: column;
			justify-content: center;
			pointer-events: none;
		}
		BoardEditor .card
		{
			flex-direction: column;
			padding: 30px;
			border-radius: 26px;
			background-color: rgba( 16, 17, 21, 0.84 );
			background-image: linear-gradient( to bottom, rgba( 255, 255, 255, 0.05 ), rgba( 255, 255, 255, 0 ) 45% );
			backdrop-filter: blur( 30px );
			border: 1px solid rgba( 255, 255, 255, 0.07 );
			box-shadow: 0px 30px 80px rgba( 0, 0, 0, 0.5 );
			color: white;
			opacity: 0;
			transform: translateX( 32px );
			transition: opacity 0.3s ease-out, transform 0.42s ease-out;
			pointer-events: none;
		}
		BoardEditor .card.shown
		{
			opacity: 1;
			transform: translateX( 0px );
			pointer-events: all;
		}
		BoardEditor .card > .head, BoardEditor .card > .about, BoardEditor .card > .stats, BoardEditor .card > .section
		{
			opacity: 0;
			transform: translateX( 14px );
			transition: opacity 0.32s ease-out, transform 0.4s ease-out;
		}
		BoardEditor .card.shown > .head, BoardEditor .card.shown > .about, BoardEditor .card.shown > .stats,
		BoardEditor .card.shown > .section
		{
			opacity: 1;
			transform: translateX( 0px );
		}
		BoardEditor .card.shown > .about { transition-delay: 0.05s; }
		BoardEditor .card.shown > .stats { transition-delay: 0.1s; }
		BoardEditor .card.shown > .section { transition-delay: 0.15s; }
		BoardEditor .card .head
		{
			flex-direction: row;
			align-items: center;
		}
		BoardEditor .card .picture
		{
			width: 84px;
			height: 84px;
			flex-shrink: 0;
			border-radius: 22px;
			overflow: hidden;
			justify-content: center;
			align-items: center;
			background-color: rgba( 255, 255, 255, 0.06 );
			border: 1px solid rgba( 255, 255, 255, 0.08 );
		}
		BoardEditor .card .initials
		{
			font-size: 30px;
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
			margin-left: 18px;
		}
		BoardEditor .card .name
		{
			font-size: 30px;
			font-weight: 700;
			letter-spacing: -0.5px;
			color: white;
		}
		BoardEditor .card .handle
		{
			margin-top: 2px;
			font-size: 15px;
			color: rgba( 255, 255, 255, 0.45 );
		}
		BoardEditor .card .about
		{
			margin-top: 22px;
			font-size: 17px;
			line-height: 1.5;
			color: rgba( 255, 255, 255, 0.82 );
		}
		BoardEditor .card .stats
		{
			flex-direction: row;
			margin-top: 22px;
			padding: 16px 0;
			border-top: 1px solid rgba( 255, 255, 255, 0.07 );
			border-bottom: 1px solid rgba( 255, 255, 255, 0.07 );
		}
		BoardEditor .card .stat
		{
			flex-direction: column;
			flex-grow: 1;
			flex-basis: 0;
			padding-right: 10px;
		}
		BoardEditor .card .value
		{
			font-size: 20px;
			font-weight: 600;
			color: white;
		}
		BoardEditor .card .caption, BoardEditor .card .heading
		{
			font-size: 11px;
			font-weight: 600;
			letter-spacing: 1.2px;
			text-transform: uppercase;
			color: rgba( 255, 255, 255, 0.4 );
		}
		BoardEditor .card .caption
		{
			margin-top: 4px;
		}
		BoardEditor .card .section
		{
			flex-direction: column;
			margin-top: 20px;
		}
		BoardEditor .card .heading
		{
			margin-bottom: 9px;
		}
		BoardEditor .card .tags, BoardEditor .card .links
		{
			flex-direction: row;
			flex-wrap: wrap;
		}
		BoardEditor .card .tag
		{
			font-size: 13px;
			padding: 6px 12px;
			margin: 0 6px 6px 0;
			border-radius: 14px;
			background-color: rgba( 255, 255, 255, 0.08 );
			color: rgba( 255, 255, 255, 0.86 );
		}
		BoardEditor .card .link
		{
			font-size: 13px;
			padding: 6px 12px;
			margin: 0 6px 6px 0;
			border-radius: 14px;
			border: 1px solid rgba( 255, 255, 255, 0.13 );
			color: rgba( 255, 255, 255, 0.72 );
		}
		BoardEditor .card .roles
		{
			flex-direction: column;
		}
		BoardEditor .card .role
		{
			margin-bottom: 4px;
			font-size: 15px;
			color: rgba( 255, 255, 255, 0.8 );
		}
		""";
}
