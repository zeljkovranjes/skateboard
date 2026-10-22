using System.Collections.Generic;
using System.Linq;
using Sandbox;
using Skateboarding.Components;

namespace Skateboarding.Systems;

/// <summary>
/// Smoke off the heel of a braking shoe: while a rider drags a foot over the ground to slow down at speed, puffs kick up
/// behind the heel of that shoe, more the faster the board goes, and grow, drift up and fade where they were left.
/// Every client shows every rider's from how it animates that rider, so nothing is sent over the network. The puff is
/// drawn in code; the addon ships no texture for it.
/// </summary>
public sealed class BrakeSmoke : GameObjectSystem<BrakeSmoke>
{
	// Board speed (m/s) from which a dragging foot smokes, and at which it smokes most.
	private const float Faintest = 2.5f;
	private const float Fullest = 7f;

	// Puffs a second at the most.
	private const float MostRate = 90f;

	// Highest (inches) an ankle stands over the ground under it for its shoe to be dragging.
	private const float Dragging = 7f;

	// How far (inches) behind the ankle the heel of the shoe is, and how far over the ground a puff starts.
	private const float HeelBack = 2.5f;
	private const float PuffRise = 2f;

	// Share of the shoe's own motion a puff starts with: dust dragged along with the heel before the air slows it.
	private const float Dragged = 0.35f;

	private readonly Dictionary<SkateRider, (ParticleEffect Effect, float Owed, Vector3 Heel)> _smoke = new();
	private static Sprite _puff;

	/// <summary>Watches the scene's riders for braking.</summary>
	public BrakeSmoke( Scene scene ) : base( scene ) => Listen( Stage.FinishUpdate, 20, Update, "BrakeSmoke" );

	private void Update()
	{
		if ( Application.IsDedicatedServer )
			return;
		foreach ( var rider in Scene.GetAllComponents<SkateRider>() )
		{
			var strength = Strength( rider, out var heel, out var away );
			if ( strength <= 0f && !_smoke.ContainsKey( rider ) )
				continue;
			var (effect, owed, last) = _smoke.TryGetValue( rider, out var known ) && known.Effect.IsValid() ? known : (Create(), 0f, heel);
			// The heel's own motion (the rider rolling on), from where it was last frame while it smoked.
			var moving = strength > 0f && Time.Delta > 0f && last != default ? (heel - last) / Time.Delta : Vector3.Zero;
			if ( moving.Length > 600f )
				moving = Vector3.Zero;
			owed += MostRate * strength * Time.Delta;
			for ( ; owed >= 1f; owed -= 1f )
			{
				// Somewhere along the way the heel came this frame, so a fast drag leaves an even trail, not a clump a frame.
				var spread = new Vector3( Random.Shared.Float( -1f, 1f ), Random.Shared.Float( -1f, 1f ), 0f );
				var share = last != default && moving != Vector3.Zero ? Random.Shared.Float() : 1f;
				var along = Vector3.Lerp( last != default ? last : heel, heel, share );
				// The puff's place in this frame's spawn, as far along the heel's way as it is.
				var puff = effect.Emit( along + spread * 1.5f, share );
				// Dragged along with the heel for a moment, kicked back off it and up a little; the rider rolls on and leaves
				// it behind.
				puff.Velocity = moving.WithZ( 0f ) * Dragged + away * Random.Shared.Float( 6f, 18f )
					+ Vector3.Up * Random.Shared.Float( 14f, 28f ) + spread * 8f;
			}
			_smoke[ rider ] = (effect, strength > 0f ? owed : 0f, strength > 0f ? heel : default);
		}
		foreach ( var gone in _smoke.Keys.Where( rider => !rider.IsValid() ).ToList() )
		{
			_smoke[ gone ].Effect?.GameObject?.Destroy();
			_smoke.Remove( gone );
		}
	}

	// How hard a rider's dragging shoe smokes (0 to 1), where its heel meets the ground, and the way back off the heel.
	private float Strength( SkateRider rider, out Vector3 heel, out Vector3 away )
	{
		heel = default;
		away = default;
		if ( rider.Mode != SkateRiderMode.Riding || rider.Board is not { } board || rider.AnimationState?.Contains( ".Brake." ) != true )
			return 0f;
		var speed = board.State.Speed;
		if ( !(speed > Faintest) )
			return 0f;
		var body = rider.VisualObject?.Components.Get<SkinnedModelRenderer>( FindMode.EverythingInSelf );
		if ( body is null || !Lowest( body, out var ankle, out var ball ) )
			return 0f;
		var ground = Scene.Trace.Ray( ankle, ankle + Vector3.Down * 24f )
			.IgnoreGameObjectHierarchy( rider.GameObject ).IgnoreGameObjectHierarchy( board.GameObject ).Run();
		if ( !ground.Hit || ankle.z - ground.HitPosition.z > Dragging )
			return 0f;
		away = (ankle - ball).WithZ( 0f ).Normal;
		heel = ground.HitPosition + away * HeelBack + Vector3.Up * PuffRise;
		var share = MathX.Clamp( (speed - Faintest) / (Fullest - Faintest), 0f, 1f );
		return share * share * (3f - 2f * share);
	}

	// The lower of the two feet (the one down on the ground): its ankle and the ball of its foot.
	private static bool Lowest( SkinnedModelRenderer body, out Vector3 ankle, out Vector3 ball )
	{
		ankle = ball = default;
		var found = false;
		foreach ( var side in new[] { "L", "R" } )
		{
			if ( !body.TryGetBoneTransform( $"ankle_{side}", out var a ) || !body.TryGetBoneTransform( $"ball_{side}", out var b ) )
				continue;
			if ( found && a.Position.z >= ankle.z )
				continue;
			ankle = a.Position;
			ball = b.Position;
			found = true;
		}
		return found;
	}

	// A smoke emitter of its own (never networked, never saved): soft puffs, white off the shoe and greying as they go,
	// that grow, rise and fade over about a second, slowed by the air.
	private ParticleEffect Create()
	{
		var holder = Scene.CreateObject( true );
		holder.Name = "Brake Smoke";
		holder.NetworkMode = NetworkMode.Never;
		holder.Flags |= GameObjectFlags.NotSaved;
		var effect = holder.Components.Create<ParticleEffect>();
		effect.MaxParticles = 256;
		effect.Lifetime = new ParticleFloat( 0.6f, 1.1f );
		effect.ApplyShape = true;
		effect.InitialScale = new ParticleFloat( 6f, 10f );
		effect.Scale = OverLife( (0f, 0.7f), (0.3f, 1.5f), (1f, 2.6f) );
		effect.ApplyAlpha = true;
		effect.Alpha = OverLife( (0f, 0f), (0.1f, 0.3f), (0.45f, 0.17f), (1f, 0f) );
		effect.ApplyColor = true;
		effect.Tint = Color.White;
		effect.Gradient = Greying();
		effect.ApplyRotation = true;
		effect.InitialRoll = new ParticleFloat( 0f, 360f );
		effect.Damping = 2f;
		effect.Force = true;
		effect.ForceDirection = Vector3.Up * 24f;
		var sprites = holder.Components.Create<ParticleSpriteRenderer>();
		sprites.Sprite = Puff();
		sprites.Lighting = true;
		sprites.Shadows = false;
		sprites.DepthFeather = 1.5f;
		return effect;
	}

	// White off the shoe, a warm light grey as it spreads, a darker grey as it thins out.
	private static ParticleGradient Greying()
	{
		var colours = new Gradient();
		colours.AddColor( 0f, new Color( 0.97f, 0.96f, 0.94f ) );
		colours.AddColor( 0.35f, new Color( 0.8f, 0.79f, 0.76f ) );
		colours.AddColor( 1f, new Color( 0.56f, 0.56f, 0.56f ) );
		return new ParticleGradient
		{
			Type = ParticleGradient.ValueType.Gradient,
			Evaluation = ParticleGradient.EvaluationType.Life,
			GradientA = colours,
		};
	}

	private static ParticleFloat OverLife( params (float Time, float Value)[] points ) => new()
	{
		Type = ParticleFloat.ValueType.Curve,
		Evaluation = ParticleFloat.EvaluationType.Life,
		CurveA = new Curve( points.Select( point => new Curve.Frame( point.Time, point.Value ) ).ToArray() ),
	};

	// A soft puff with a ragged edge, white (coloured by the effect), drawn once.
	private static Sprite Puff()
	{
		if ( _puff is not null )
			return _puff;
		const int size = 64;
		var data = new byte[ size * size * 4 ];
		var random = new System.Random( 7 );
		var lumps = Enumerable.Range( 0, 6 )
			.Select( _ => (X: (float)random.NextDouble() * 0.5f - 0.25f, Y: (float)random.NextDouble() * 0.5f - 0.25f) ).ToArray();
		for ( var y = 0; y < size; y++ )
		{
			for ( var x = 0; x < size; x++ )
			{
				var u = (x + 0.5f) / size * 2f - 1f;
				var v = (y + 0.5f) / size * 2f - 1f;
				// A few overlapping soft blobs make it uneven, like a puff rather than a disc, and wisps of noise break its
				// edge; it fades out to nothing well inside the square.
				var density = 0f;
				foreach ( var lump in lumps )
					density += MathF.Exp( -((u - lump.X) * (u - lump.X) + (v - lump.Y) * (v - lump.Y)) / 0.12f );
				var wisps = 0.6f * Noise( x / 10f, y / 10f ) + 0.4f * Noise( x / 5f + 17f, y / 5f + 31f );
				var edge = MathF.Max( 0f, 1f - (u * u + v * v) * (0.75f + 0.5f * wisps) );
				var alpha = (1f - MathF.Exp( -density * 0.5f )) * edge * (0.45f + 0.55f * wisps);
				var i = (y * size + x) * 4;
				data[ i ] = data[ i + 1 ] = data[ i + 2 ] = 255;
				data[ i + 3 ] = (byte)MathX.Clamp( alpha * 255f, 0f, 255f );
			}
		}
		_puff = Sprite.FromTexture( Texture.Create( size, size ).WithData( data ).Finish() );
		return _puff;
	}

	// Smooth value noise (0 to 1) over a grid of fixed random values.
	private static float Noise( float x, float y )
	{
		var x0 = (int)MathF.Floor( x );
		var y0 = (int)MathF.Floor( y );
		var fx = x - x0;
		var fy = y - y0;
		fx = fx * fx * (3f - 2f * fx);
		fy = fy * fy * (3f - 2f * fy);
		var top = MathX.Lerp( Corner( x0, y0 ), Corner( x0 + 1, y0 ), fx );
		var bottom = MathX.Lerp( Corner( x0, y0 + 1 ), Corner( x0 + 1, y0 + 1 ), fx );
		return MathX.Lerp( top, bottom, fy );
	}

	private static float Corner( int x, int y )
	{
		var hash = (uint)(x * 374761393 + y * 668265263);
		hash = (hash ^ (hash >> 13)) * 1274126177u;
		return ((hash ^ (hash >> 16)) & 0xffff) / 65535f;
	}
}
