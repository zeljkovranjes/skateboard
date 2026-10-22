namespace Skateboarding.Engine.Docs;

/// <summary>
/// The docs' look, the board editor's: the game behind softly blurred, darkened and a little greyed, and the docs over it
/// as words alone with nothing behind them, in white and greys with a soft shadow under the letters and no accent colour.
/// Down the side the pages as words under small capital group names, the open one white with a thin underline that grows
/// out from its middle, the others grey, lighter under the mouse. Beside them the open page: a small capital group name, a
/// large title, a lead, and its moves under medium headings, each how-to line set against a small capital device name,
/// keys as outlined caps, GIFs with rounded corners and a soft shadow. The side comes in from the left as the docs open,
/// and each page from the right as it is opened.
/// </summary>
internal static class DocsStyles
{
	/// <summary>
	/// Raised whenever the look or the layout changes: docs made before are made again as they next open (a panel parses
	/// its styles once, as it is made).
	/// </summary>
	internal const int Version = 3;

	/// <summary>The height of a controller button's picture in a how-to line (pixels; the width follows its shape).</summary>
	internal const float GlyphHeight = 24f;

	internal const string Sheet = """
		SkateDocs
		{
			position: absolute;
			left: 0;
			top: 0;
			right: 0;
			bottom: 0;
			display: none;
			justify-content: center;
			backdrop-filter: blur( 14px ) brightness( 0.55 ) saturate( 0.75 );
			background-color: rgba( 14, 16, 20, 0.45 );
			font-family: Inter;
			color: white;
			pointer-events: all;
		}
		SkateDocs.open
		{
			display: flex;
		}
		SkateDocs label
		{
			text-shadow: 0px 2px 14px rgba( 0, 0, 0, 0.55 );
		}
		SkateDocs .window
		{
			flex-grow: 1;
			flex-direction: row;
			max-width: 1500px;
		}

		SkateDocs .side
		{
			width: 300px;
			flex-shrink: 0;
			flex-direction: column;
			align-items: flex-start;
			padding: 88px 24px 40px 72px;
			overflow: scroll;
			opacity: 0;
			transform: translateX( -60px );
			transition: opacity 0.35s ease-out, transform 0.55s ease-out;
		}
		SkateDocs.shown .side
		{
			opacity: 1;
			transform: translateX( 0px );
		}
		SkateDocs .nav-heading
		{
			margin: 26px 0 8px;
			font-size: 12px;
			font-weight: 600;
			letter-spacing: 1.4px;
			text-transform: uppercase;
			color: rgba( 255, 255, 255, 0.4 );
		}
		SkateDocs .nav-heading.first
		{
			margin-top: 0;
		}
		SkateDocs .nav-link
		{
			flex-direction: column;
			padding: 5px 0;
			color: rgba( 255, 255, 255, 0.5 );
			cursor: pointer;
			transition: color 0.2s ease-out;
		}
		SkateDocs .nav-link:hover
		{
			color: rgba( 255, 255, 255, 0.8 );
		}
		SkateDocs .nav-link.active
		{
			color: white;
		}
		SkateDocs .nav-name
		{
			font-size: 17px;
			font-weight: 600;
		}
		SkateDocs .nav-link .underline
		{
			margin-top: 4px;
			height: 2px;
			border-radius: 1px;
			background-color: white;
			opacity: 0;
			transform: scaleX( 0 );
			transition: transform 0.3s ease-out, opacity 0.2s ease-out;
		}
		SkateDocs .nav-link.active .underline
		{
			opacity: 1;
			transform: scaleX( 1 );
		}
		SkateDocs .nav-foot
		{
			margin-top: auto;
			padding-top: 26px;
			font-size: 13px;
			color: rgba( 255, 255, 255, 0.35 );
		}

		SkateDocs .main
		{
			flex-grow: 1;
			flex-direction: column;
			overflow: scroll;
			padding: 88px 72px 96px 56px;
		}
		SkateDocs .article
		{
			flex-direction: column;
			flex-shrink: 0;
			max-width: 760px;
			opacity: 0;
			transform: translateX( 90px );
			transition: opacity 0.35s ease-out, transform 0.55s ease-out;
		}
		SkateDocs .article.shown
		{
			opacity: 1;
			transform: translateX( 0px );
		}
		SkateDocs .eyebrow
		{
			font-size: 12px;
			font-weight: 600;
			letter-spacing: 1.4px;
			text-transform: uppercase;
			color: rgba( 255, 255, 255, 0.45 );
		}
		SkateDocs .h1
		{
			margin-top: 6px;
			font-size: 48px;
			font-weight: 800;
			letter-spacing: -1px;
			color: white;
		}
		SkateDocs .lead
		{
			margin-top: 14px;
			font-size: 20px;
			line-height: 30px;
			color: rgba( 255, 255, 255, 0.82 );
		}
		SkateDocs .list
		{
			margin-top: 24px;
			flex-direction: column;
		}
		SkateDocs .li
		{
			flex-direction: row;
			align-items: flex-start;
			margin-bottom: 6px;
		}
		SkateDocs .bullet
		{
			width: 20px;
			flex-shrink: 0;
			font-size: 17px;
			line-height: 30px;
			color: rgba( 255, 255, 255, 0.35 );
		}

		SkateDocs .move
		{
			flex-direction: column;
			flex-shrink: 0;
			margin-top: 56px;
		}
		SkateDocs .h2
		{
			font-size: 28px;
			font-weight: 700;
			letter-spacing: -0.5px;
			color: white;
		}
		SkateDocs .p
		{
			margin-top: 8px;
			font-size: 18px;
			line-height: 28px;
			color: rgba( 255, 255, 255, 0.8 );
		}
		SkateDocs .dl
		{
			margin-top: 16px;
			flex-direction: column;
		}
		SkateDocs .dl-row
		{
			flex-direction: row;
			align-items: flex-start;
			padding: 3px 0;
		}
		SkateDocs .dt
		{
			width: 118px;
			flex-shrink: 0;
			font-size: 12px;
			font-weight: 600;
			letter-spacing: 1.4px;
			text-transform: uppercase;
			line-height: 30px;
			color: rgba( 255, 255, 255, 0.45 );
		}
		SkateDocs .note
		{
			margin-top: 12px;
			font-size: 16px;
			line-height: 24px;
			color: rgba( 255, 255, 255, 0.55 );
		}
		SkateDocs .figure
		{
			margin-top: 22px;
			width: 560px;
			height: 315px;
			flex-shrink: 0;
			border-radius: 18px;
			box-shadow: 0px 20px 50px rgba( 0, 0, 0, 0.45 );
			background-size: cover;
			background-position: center;
		}

		SkateDocs .flow
		{
			flex-direction: row;
			flex-wrap: wrap;
			align-items: center;
			flex-grow: 1;
			flex-shrink: 1;
			min-width: 0;
		}
		SkateDocs .word
		{
			margin-right: 5px;
			font-size: 17px;
			line-height: 30px;
			color: rgba( 255, 255, 255, 0.9 );
		}
		SkateDocs .word.tight
		{
			margin-left: -7px;
		}
		SkateDocs .word.opens
		{
			margin-right: 0;
		}
		SkateDocs .kbd
		{
			height: 26px;
			margin: 2px 6px 2px 1px;
			padding: 0 9px;
			align-items: center;
			border-radius: 7px;
			border: 1px solid rgba( 255, 255, 255, 0.4 );
			font-size: 14px;
			font-weight: 600;
			white-space: nowrap;
			color: white;
		}
		SkateDocs .kbd.unbound
		{
			border: 1px solid rgba( 255, 255, 255, 0.18 );
			color: rgba( 255, 255, 255, 0.45 );
		}
		SkateDocs .kbd.listening
		{
			border: 1px solid white;
			color: rgba( 255, 255, 255, 0.65 );
		}
		SkateDocs .glyph
		{
			height: 24px;
			margin: 3px 6px 3px 1px;
			background-size: contain;
			background-repeat: no-repeat;
			background-position: center;
			opacity: 0.95;
		}

		SkateDocs .h3
		{
			margin-top: 40px;
			margin-bottom: 6px;
			font-size: 12px;
			font-weight: 600;
			letter-spacing: 1.4px;
			text-transform: uppercase;
			color: rgba( 255, 255, 255, 0.45 );
		}
		SkateDocs .tr
		{
			flex-direction: row;
			flex-shrink: 0;
			align-items: center;
			min-height: 48px;
			padding: 6px 0;
			border-bottom: 1px solid rgba( 255, 255, 255, 0.06 );
		}
		SkateDocs .td-pad
		{
			width: 260px;
			flex-shrink: 0;
			flex-direction: row;
			align-items: center;
			padding-right: 18px;
		}
		SkateDocs .td-glyph
		{
			flex-grow: 0;
			flex-shrink: 0;
			width: 38px;
		}
		SkateDocs .td-pad-text
		{
			flex-shrink: 1;
			font-size: 16px;
			line-height: 22px;
			color: white;
		}
		SkateDocs .td-what
		{
			flex-grow: 1;
			flex-shrink: 1;
			font-size: 16px;
			line-height: 22px;
			color: rgba( 255, 255, 255, 0.7 );
		}
		SkateDocs .td-title
		{
			flex-grow: 1;
			flex-shrink: 1;
			padding-right: 18px;
			font-size: 16px;
			line-height: 22px;
			color: rgba( 255, 255, 255, 0.9 );
		}
		SkateDocs .td-default
		{
			margin-right: 16px;
			font-size: 13px;
			color: rgba( 255, 255, 255, 0.4 );
		}
		SkateDocs .td-key
		{
			width: 140px;
			flex-shrink: 0;
			flex-direction: row;
			justify-content: flex-end;
		}
		SkateDocs .td-action
		{
			width: 84px;
			flex-shrink: 0;
			flex-direction: row;
			justify-content: flex-end;
		}
		SkateDocs .link
		{
			font-size: 15px;
			font-weight: 600;
			text-decoration: underline;
			color: rgba( 255, 255, 255, 0.75 );
			cursor: pointer;
			transition: color 0.2s ease-out;
		}
		SkateDocs .link:hover
		{
			color: white;
		}
		SkateDocs .toolbar
		{
			margin-top: 24px;
			flex-direction: row;
			align-items: center;
		}
		SkateDocs .muted
		{
			margin-left: 18px;
			font-size: 15px;
			color: rgba( 255, 255, 255, 0.5 );
		}
		SkateDocs .conflict
		{
			flex-direction: row;
			flex-shrink: 0;
			align-items: center;
			padding: 10px 0 14px;
			border-bottom: 1px solid rgba( 255, 255, 255, 0.06 );
		}
		SkateDocs .conflict-text
		{
			flex-grow: 1;
			font-size: 16px;
			color: rgba( 255, 255, 255, 0.78 );
		}
		SkateDocs .conflict .link
		{
			margin-left: 20px;
		}
		""";
}
