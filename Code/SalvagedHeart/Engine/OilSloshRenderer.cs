using System;
using Sandbox;
using SalvagedHeart.Core;

namespace SalvagedHeart.Engine;

/// <summary>
/// Draws the oil's volume (<see cref="OilVolumeMesh"/>) as one scene object whose vertex buffer is filled again each
/// frame: no new buffers, no allocations.
/// </summary>
internal sealed class OilSloshRenderer : IDisposable
{
	// Texture coordinates per metre (the oil's material has no texture worth tiling; any steady mapping does).
	private const float TexturePerMetre = 10f;

	private readonly Vertex[] _vertices = new Vertex[ OilVolumeMesh.MostVertices ];
	private readonly Mesh _mesh;
	private readonly SceneObject _object;

	public OilSloshRenderer( SceneWorld world, Material material, float unitsPerMeter )
	{
		_mesh = new Mesh( material );
		_mesh.CreateVertexBuffer<Vertex>( _vertices.Length, _vertices.AsSpan() );
		_mesh.Bounds = new BBox( Convert( OilVolumeMesh.Minimum ) * unitsPerMeter, Convert( OilVolumeMesh.Maximum ) * unitsPerMeter );
		_mesh.SetVertexRange( 0, 0 );
		var model = Model.Builder.AddMesh( _mesh ).Create();
		_object = new SceneObject( world, model );
	}

	/// <summary>Draws <paramref name="volume"/> (metres, the board model's space) where <paramref name="transform"/> puts it.</summary>
	public void Update( OilVolumeMesh volume, Transform transform, float unitsPerMeter )
	{
		for ( var i = 0; i < volume.VertexCount; i++ )
		{
			var position = Convert( volume.Positions[ i ] );
			var normal = Convert( volume.Normals[ i ] );
			var reference = MathF.Abs( normal.z ) < 0.9f ? Vector3.Up : Vector3.Forward;
			var tangent = Vector3.Cross( reference, normal ).Normal;
			_vertices[ i ] = new Vertex( position * unitsPerMeter, normal, tangent,
				new Vector2( position.x * TexturePerMetre, position.y * TexturePerMetre ) );
		}
		_mesh.SetVertexBufferData<Vertex>( _vertices.AsSpan( 0, volume.VertexCount ) );
		_mesh.SetVertexRange( 0, volume.VertexCount );
		_object.Transform = transform;
	}

	/// <summary>Moves the oil drawn last to where <paramref name="transform"/> puts it now.</summary>
	public void Place( Transform transform )
	{
		_object.Transform = transform;
	}

	/// <summary>Draws the oil under these tags (its board's), so whatever leaves the board out of view leaves the oil out.</summary>
	public void Tag( ITagSet tags )
	{
		_object.Tags.SetFrom( tags );
	}

	private static Vector3 Convert( System.Numerics.Vector3 value ) => new( value.X, value.Y, value.Z );

	public void Dispose()
	{
		_object.Delete();
	}
}
