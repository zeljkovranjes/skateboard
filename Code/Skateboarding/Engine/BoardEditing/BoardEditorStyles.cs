namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor's look: the game behind softly blurred, darkened and a little greyed, so nothing behind the board
/// pulls at the eye or flickers, and the board alone, sharp, in the middle. A picked portrait's card sits beside it:
/// frosted glass, rounded, quiet greys and plain type with no accent colour, gliding in with its lines following on.
/// </summary>
internal static class BoardEditorStyles
{
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
		BoardEditor .view
		{
			position: absolute;
			left: 0;
			top: 0;
			right: 0;
			bottom: 0;
			pointer-events: none;
		}
		BoardEditor.pointing
		{
			cursor: pointer;
		}
		BoardEditor .card
		{
			position: absolute;
			right: 64px;
			top: 16%;
			width: 360px;
			flex-direction: column;
			padding: 26px 26px 22px;
			border-radius: 20px;
			background-color: rgba( 22, 24, 30, 0.72 );
			backdrop-filter: blur( 24px );
			border: 1px solid rgba( 255, 255, 255, 0.08 );
			box-shadow: 0px 24px 60px rgba( 0, 0, 0, 0.45 );
			color: white;
			opacity: 0;
			transform: translateY( 18px ) scale( 0.97 );
			transition: opacity 0.28s ease-out, transform 0.36s ease-out;
			pointer-events: none;
		}
		BoardEditor .card.shown
		{
			opacity: 1;
			transform: translateY( 0px ) scale( 1 );
			pointer-events: all;
		}
		BoardEditor .card > *
		{
			opacity: 0;
			transform: translateY( 8px );
			transition: opacity 0.3s ease-out, transform 0.34s ease-out;
		}
		BoardEditor .card.shown > *
		{
			opacity: 1;
			transform: translateY( 0px );
		}
		BoardEditor .card.shown > .about { transition-delay: 0.05s; }
		BoardEditor .card.shown > .facts { transition-delay: 0.09s; }
		BoardEditor .card.shown > .tags { transition-delay: 0.13s; }
		BoardEditor .card.shown > .roles { transition-delay: 0.17s; }
		BoardEditor .card.shown > .links { transition-delay: 0.21s; }
		BoardEditor .card .head
		{
			flex-direction: row;
			align-items: center;
		}
		BoardEditor .card .avatar
		{
			width: 56px;
			height: 56px;
			flex-shrink: 0;
			border-radius: 28px;
			overflow: hidden;
			justify-content: center;
			align-items: center;
			background-color: rgba( 255, 255, 255, 0.08 );
		}
		BoardEditor .card .initials
		{
			font-size: 20px;
			font-weight: 600;
			color: rgba( 255, 255, 255, 0.7 );
		}
		BoardEditor .card .picture
		{
			position: absolute;
			left: 0;
			top: 0;
			width: 100%;
			height: 100%;
			opacity: 0;
			transition: opacity 0.4s ease-out;
		}
		BoardEditor .card .picture.loaded
		{
			opacity: 1;
		}
		BoardEditor .card .names
		{
			flex-direction: column;
			margin-left: 14px;
		}
		BoardEditor .card .name
		{
			font-size: 22px;
			font-weight: 600;
			color: rgba( 255, 255, 255, 0.95 );
		}
		BoardEditor .card .username
		{
			font-size: 13px;
			margin-top: 2px;
			color: rgba( 255, 255, 255, 0.5 );
		}
		BoardEditor .card .about
		{
			margin-top: 18px;
			font-size: 14px;
			line-height: 1.45;
			color: rgba( 255, 255, 255, 0.8 );
		}
		BoardEditor .card .facts
		{
			margin-top: 12px;
			font-size: 12px;
			color: rgba( 255, 255, 255, 0.5 );
		}
		BoardEditor .card .tags
		{
			flex-direction: row;
			flex-wrap: wrap;
			margin-top: 14px;
		}
		BoardEditor .card .tag
		{
			font-size: 12px;
			padding: 4px 10px;
			margin: 0 6px 6px 0;
			border-radius: 12px;
			background-color: rgba( 255, 255, 255, 0.07 );
			color: rgba( 255, 255, 255, 0.82 );
		}
		BoardEditor .card .roles
		{
			flex-direction: column;
			margin-top: 8px;
		}
		BoardEditor .card .role
		{
			font-size: 13px;
			margin-top: 4px;
			color: rgba( 255, 255, 255, 0.72 );
		}
		BoardEditor .card .links
		{
			flex-direction: column;
			margin-top: 16px;
			padding-top: 12px;
			border-top: 1px solid rgba( 255, 255, 255, 0.06 );
		}
		BoardEditor .card .link
		{
			font-size: 12px;
			margin-top: 3px;
			color: rgba( 255, 255, 255, 0.45 );
		}
		BoardEditor .card .empty
		{
			display: none;
		}
		""";
}
