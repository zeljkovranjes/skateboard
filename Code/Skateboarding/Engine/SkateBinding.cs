namespace Skateboarding.Engine;

/// <summary>
/// One skate control: its id (stable, for the docs and saved bindings), the group and title it is listed under, its
/// default keyboard or mouse key, what does it on a controller (as the docs describe it), and whether it is held.
/// </summary>
internal sealed record SkateBinding( string Id, string Group, string Title, string DefaultKey, string Pad, bool Hold );
