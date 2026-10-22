namespace Skateboarding.Engine.BoardEditing;

/// <summary>
/// Who a community portrait on the board shows, as the board editor's card tells it: name and s&amp;box username, a line
/// about them, how long they have been around and how much they have posted, what they work on, their organisations and
/// roles in them, their picture (the portrait's own image, or their Steam avatar) and their public links. Anything not
/// known is null or empty.
/// </summary>
internal sealed record PortraitInfo(
	string Id,
	string Name,
	string Username,
	string Description,
	string Tenure,
	int Posts,
	string[] Expertise,
	string MainOrganization,
	string[] Roles,
	long SteamId,
	string PictureUrl,
	string SboxProfile,
	string SteamUrl,
	string GithubUrl );
