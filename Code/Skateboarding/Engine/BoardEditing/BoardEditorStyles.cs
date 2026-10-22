namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// The board editor's look: the game behind softly blurred, darkened and a little greyed, so nothing behind the board
/// pulls at the eye or flickers; the board alone, sharp, in the middle; a short line of hints at the bottom in the docs'
/// quiet greys, keys as outlined key caps.
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
			justify-content: flex-end;
			align-items: center;
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
		BoardEditor .title
		{
			position: absolute;
			left: 40px;
			top: 34px;
			font-size: 15px;
			font-weight: 500;
			color: rgba( 255, 255, 255, 0.72 );
		}
		BoardEditor .bar
		{
			flex-direction: column;
			align-items: center;
			margin-bottom: 34px;
			padding: 10px 18px;
			border-radius: 6px;
			background-color: rgba( 21, 22, 25, 0.72 );
			border: 1px solid rgba( 255, 255, 255, 0.06 );
		}
		BoardEditor .hint
		{
			font-size: 13px;
			color: rgba( 255, 255, 255, 0.62 );
			margin: 2px 0;
		}
		""";
}
