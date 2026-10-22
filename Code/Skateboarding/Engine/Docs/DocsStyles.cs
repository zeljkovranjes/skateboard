namespace Skateboarding.Engine.Docs;

/// <summary>
/// The docs' look: a quiet documentation page. Neutral greys on a dark, slightly see-through window; a plain side list
/// whose open page is marked by a thin rule; a reading column of medium headings, muted leads and body text in short
/// lines; keys as outlined key caps. The blue accent marks only the open page and the links.
/// </summary>
internal static class DocsStyles
{
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
			align-items: center;
			background-color: rgba( 0, 0, 0, 0.55 );
			font-family: Inter;
			pointer-events: all;
		}
		SkateDocs.open
		{
			display: flex;
		}
		SkateDocs .window
		{
			width: 92%;
			height: 88%;
			max-width: 1320px;
			max-height: 880px;
			flex-direction: row;
			border-radius: 6px;
			overflow: hidden;
			background-color: rgba( 21, 22, 25, 0.97 );
			border: 1px solid rgba( 255, 255, 255, 0.08 );
		}

		SkateDocs .side
		{
			width: 232px;
			flex-shrink: 0;
			flex-direction: column;
			padding: 30px 0 18px 22px;
			border-right: 1px solid rgba( 255, 255, 255, 0.06 );
			overflow: scroll;
		}
		SkateDocs .nav-heading
		{
			margin: 22px 0 6px;
			font-size: 12px;
			font-weight: 500;
			color: rgba( 255, 255, 255, 0.38 );
		}
		SkateDocs .nav-heading.first
		{
			margin-top: 0;
		}
		SkateDocs .nav-link
		{
			padding: 5px 12px 5px 12px;
			border-left: 2px solid rgba( 255, 255, 255, 0.06 );
			font-size: 14px;
			line-height: 20px;
			color: rgba( 255, 255, 255, 0.55 );
			cursor: pointer;
		}
		SkateDocs .nav-link:hover
		{
			color: rgba( 255, 255, 255, 0.85 );
		}
		SkateDocs .nav-link.active
		{
			border-left: 2px solid rgba( 77, 144, 252, 1 );
			color: rgba( 255, 255, 255, 0.96 );
			font-weight: 500;
		}
		SkateDocs .nav-foot
		{
			margin-top: auto;
			padding-top: 18px;
			font-size: 11.5px;
			color: rgba( 255, 255, 255, 0.28 );
		}

		SkateDocs .main
		{
			flex-grow: 1;
			flex-direction: column;
			overflow: scroll;
			padding: 44px 56px 72px;
		}
		SkateDocs .article
		{
			flex-direction: column;
			flex-shrink: 0;
			max-width: 640px;
		}
		SkateDocs .h1
		{
			font-size: 28px;
			font-weight: 600;
			color: rgba( 255, 255, 255, 0.95 );
		}
		SkateDocs .lead
		{
			margin-top: 10px;
			font-size: 16px;
			line-height: 24px;
			color: rgba( 255, 255, 255, 0.58 );
		}
		SkateDocs .list
		{
			margin-top: 22px;
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
			width: 18px;
			flex-shrink: 0;
			font-size: 15px;
			line-height: 26px;
			color: rgba( 255, 255, 255, 0.35 );
		}

		SkateDocs .move
		{
			flex-direction: column;
			flex-shrink: 0;
			margin-top: 30px;
			padding-top: 30px;
			border-top: 1px solid rgba( 255, 255, 255, 0.06 );
		}
		SkateDocs .h2
		{
			font-size: 18px;
			font-weight: 600;
			color: rgba( 255, 255, 255, 0.94 );
		}
		SkateDocs .p
		{
			margin-top: 6px;
			font-size: 15px;
			line-height: 23px;
			color: rgba( 255, 255, 255, 0.7 );
		}
		SkateDocs .dl
		{
			margin-top: 14px;
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
			width: 92px;
			flex-shrink: 0;
			font-size: 14px;
			line-height: 26px;
			color: rgba( 255, 255, 255, 0.42 );
		}
		SkateDocs .note
		{
			margin-top: 10px;
			font-size: 14px;
			line-height: 21px;
			color: rgba( 255, 255, 255, 0.48 );
		}
		SkateDocs .figure
		{
			margin-top: 18px;
			width: 480px;
			height: 270px;
			flex-shrink: 0;
			border-radius: 4px;
			border: 1px solid rgba( 255, 255, 255, 0.1 );
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
			margin-right: 4px;
			font-size: 15px;
			line-height: 26px;
			color: rgba( 255, 255, 255, 0.86 );
		}
		SkateDocs .word.tight
		{
			margin-left: -6px;
		}
		SkateDocs .word.opens
		{
			margin-right: 0;
		}
		SkateDocs .kbd
		{
			height: 21px;
			margin: 2px 5px 2px 1px;
			padding: 0 6px;
			align-items: center;
			border-radius: 4px;
			border: 1px solid rgba( 255, 255, 255, 0.22 );
			background-color: rgba( 255, 255, 255, 0.04 );
			font-size: 12.5px;
			font-weight: 500;
			white-space: nowrap;
			color: rgba( 255, 255, 255, 0.92 );
		}
		SkateDocs .kbd.unbound
		{
			color: rgba( 255, 255, 255, 0.4 );
		}
		SkateDocs .kbd.listening
		{
			border: 1px solid rgba( 77, 144, 252, 0.9 );
			color: rgba( 255, 255, 255, 0.6 );
		}
		SkateDocs .glyph
		{
			height: 20px;
			margin: 3px 5px 3px 1px;
			background-size: contain;
			background-repeat: no-repeat;
			background-position: center;
			opacity: 0.92;
		}

		SkateDocs .h3
		{
			margin-top: 34px;
			margin-bottom: 4px;
			font-size: 15px;
			font-weight: 600;
			color: rgba( 255, 255, 255, 0.9 );
		}
		SkateDocs .tr
		{
			flex-direction: row;
			flex-shrink: 0;
			align-items: center;
			min-height: 42px;
			padding: 6px 0;
			border-bottom: 1px solid rgba( 255, 255, 255, 0.05 );
		}
		SkateDocs .td-pad
		{
			width: 236px;
			flex-shrink: 0;
			flex-direction: row;
			align-items: center;
			padding-right: 16px;
		}
		SkateDocs .td-glyph
		{
			flex-grow: 0;
			flex-shrink: 0;
			width: 34px;
		}
		SkateDocs .td-pad-text
		{
			flex-shrink: 1;
			font-size: 14px;
			line-height: 20px;
			color: rgba( 255, 255, 255, 0.9 );
		}
		SkateDocs .td-what
		{
			flex-grow: 1;
			flex-shrink: 1;
			font-size: 14px;
			line-height: 20px;
			color: rgba( 255, 255, 255, 0.62 );
		}
		SkateDocs .td-title
		{
			flex-grow: 1;
			flex-shrink: 1;
			padding-right: 16px;
			font-size: 14px;
			line-height: 20px;
			color: rgba( 255, 255, 255, 0.86 );
		}
		SkateDocs .td-default
		{
			margin-right: 14px;
			font-size: 12.5px;
			color: rgba( 255, 255, 255, 0.34 );
		}
		SkateDocs .td-key
		{
			width: 120px;
			flex-shrink: 0;
			flex-direction: row;
			justify-content: flex-end;
		}
		SkateDocs .td-action
		{
			width: 70px;
			flex-shrink: 0;
			flex-direction: row;
			justify-content: flex-end;
		}
		SkateDocs .link
		{
			font-size: 13.5px;
			color: rgba( 120, 170, 255, 1 );
			cursor: pointer;
		}
		SkateDocs .link:hover
		{
			color: rgba( 170, 205, 255, 1 );
		}
		SkateDocs .toolbar
		{
			margin-top: 18px;
			flex-direction: row;
			align-items: center;
		}
		SkateDocs .muted
		{
			margin-left: 14px;
			font-size: 13.5px;
			color: rgba( 255, 255, 255, 0.42 );
		}
		SkateDocs .conflict
		{
			flex-direction: row;
			flex-shrink: 0;
			align-items: center;
			padding: 8px 0 12px;
			border-bottom: 1px solid rgba( 255, 255, 255, 0.05 );
		}
		SkateDocs .conflict-text
		{
			flex-grow: 1;
			font-size: 14px;
			color: rgba( 255, 255, 255, 0.72 );
		}
		SkateDocs .conflict .link
		{
			margin-left: 18px;
		}
		""";
}
