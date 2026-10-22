namespace Skateboarding.Engine.Docs;

/// <summary>
/// One page of the docs: its place in the side nav (its group and title), a lead line, an optional box of things to
/// know first, and its moves.
/// </summary>
internal sealed record DocsPage( string Id, string Group, string Title, string Lead, string[] Basics, DocsMove[] Moves,
	DocsPageKind Kind = DocsPageKind.Moves );
