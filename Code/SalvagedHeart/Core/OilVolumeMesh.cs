using System;

namespace SalvagedHeart.Core;

using Vector3 = System.Numerics.Vector3;

/// <summary>
/// The oil in the heart's case: the case's box clipped by the oil's surface at the height that holds the asked fill,
/// as triangles (at most 48). The box is the case's inside in the board model's space (metres); clipping a box works
/// at any tilt, the board upside down included. Arrays are reused, so building it allocates nothing.
/// </summary>
internal sealed class OilVolumeMesh
{
	/// <summary>The case's inside, in the board model's space (metres).</summary>
	public static readonly Vector3 Minimum = new( -0.0479f, -0.0299f, 0.0242f );
	public static readonly Vector3 Maximum = new( 0.0479f, 0.0299f, 0.0592f );

	// Most triangle corners the clipped box gives: seven faces of at most twelve points each.
	public const int MostVertices = 144;

	// Halvings of the surface's height in the search for the fill.
	private const int FillSearchSteps = 19;

	// The box's six faces, as corner indices, wound outward.
	private static readonly int[][] Faces =
	{
		new[] { 0, 3, 2, 1 },
		new[] { 4, 5, 6, 7 },
		new[] { 0, 1, 5, 4 },
		new[] { 1, 2, 6, 5 },
		new[] { 2, 3, 7, 6 },
		new[] { 3, 0, 4, 7 }
	};

	private readonly Vector3[] _corners = new Vector3[ 8 ];
	private readonly Vector3[][] _faces = new Vector3[ 7 ][];
	private readonly int[] _counts = new int[ 7 ];
	private readonly Vector3[] _cap = new Vector3[ 12 ];
	private readonly float[] _angles = new float[ 12 ];
	private int _capCount;

	public Vector3[] Positions { get; } = new Vector3[ MostVertices ];

	public Vector3[] Normals { get; } = new Vector3[ MostVertices ];

	public int VertexCount { get; private set; }

	public float Volume { get; private set; }

	public float BoxVolume => (Maximum.X - Minimum.X) * (Maximum.Y - Minimum.Y) * (Maximum.Z - Minimum.Z);

	public OilVolumeMesh()
	{
		for ( var i = 0; i < 7; i++ )
			_faces[ i ] = new Vector3[ 12 ];
		_corners[ 0 ] = new Vector3( Minimum.X, Minimum.Y, Minimum.Z );
		_corners[ 1 ] = new Vector3( Maximum.X, Minimum.Y, Minimum.Z );
		_corners[ 2 ] = new Vector3( Maximum.X, Maximum.Y, Minimum.Z );
		_corners[ 3 ] = new Vector3( Minimum.X, Maximum.Y, Minimum.Z );
		_corners[ 4 ] = new Vector3( Minimum.X, Minimum.Y, Maximum.Z );
		_corners[ 5 ] = new Vector3( Maximum.X, Minimum.Y, Maximum.Z );
		_corners[ 6 ] = new Vector3( Maximum.X, Maximum.Y, Maximum.Z );
		_corners[ 7 ] = new Vector3( Minimum.X, Maximum.Y, Maximum.Z );
	}

	/// <summary>The oil under a surface with this normal, holding <paramref name="fill"/> of the box's volume.</summary>
	public void Build( Vector3 normal, float fill )
	{
		normal = Vector3.Normalize( normal );
		var low = float.MaxValue;
		var high = float.MinValue;
		foreach ( var corner in _corners )
		{
			var height = Vector3.Dot( normal, corner );
			low = MathF.Min( low, height );
			high = MathF.Max( high, height );
		}
		var target = BoxVolume * Math.Clamp( fill, 0.01f, 0.99f );
		for ( var i = 0; i < FillSearchSteps; i++ )
		{
			var middle = (low + high) * 0.5f;
			Clip( normal, middle );
			if ( MeasureVolume() < target )
				low = middle;
			else
				high = middle;
		}
		Clip( normal, (low + high) * 0.5f );
		Volume = MeasureVolume();
		VertexCount = 0;
		for ( var face = 0; face < 7; face++ )
		{
			for ( var j = 1; j < _counts[ face ] - 1; j++ )
			{
				var a = _faces[ face ][ 0 ];
				var b = _faces[ face ][ j ];
				var c = _faces[ face ][ j + 1 ];
				var cross = Vector3.Cross( b - a, c - a );
				if ( cross.LengthSquared() < 1e-16f )
					continue;
				var n = Vector3.Normalize( cross );
				Add( a, n );
				Add( b, n );
				Add( c, n );
			}
		}
	}

	private void Add( Vector3 position, Vector3 normal )
	{
		Positions[ VertexCount ] = position;
		Normals[ VertexCount ] = normal;
		VertexCount++;
	}

	// The box's faces kept below the plane (normal · p = offset), and the cap where the plane cuts the box.
	private void Clip( Vector3 normal, float offset )
	{
		Array.Clear( _counts );
		_capCount = 0;
		for ( var face = 0; face < 6; face++ )
		{
			var indices = Faces[ face ];
			for ( var i = 0; i < 4; i++ )
			{
				var a = _corners[ indices[ i ] ];
				var b = _corners[ indices[ (i + 1) % 4 ] ];
				var da = Vector3.Dot( normal, a ) - offset;
				var db = Vector3.Dot( normal, b ) - offset;
				if ( da <= 0 )
					_faces[ face ][ _counts[ face ]++ ] = a;
				if ( (da < 0 && db > 0) || (da > 0 && db < 0) )
				{
					var point = a + (b - a) * (da / (da - db));
					_faces[ face ][ _counts[ face ]++ ] = point;
					AddCapPoint( point );
				}
				else if ( MathF.Abs( da ) < 1e-8f )
				{
					AddCapPoint( a );
				}
			}
		}
		if ( _capCount < 3 )
			return;
		OrderCap( normal );
		_counts[ 6 ] = _capCount;
		for ( var i = 0; i < _capCount; i++ )
			_faces[ 6 ][ i ] = _cap[ i ];
	}

	// The cap's points in order around its middle (an insertion sort by angle; there are at most twelve).
	private void OrderCap( Vector3 normal )
	{
		var center = Vector3.Zero;
		for ( var i = 0; i < _capCount; i++ )
			center += _cap[ i ];
		center /= _capCount;
		var u = Vector3.Normalize( Vector3.Cross( normal, MathF.Abs( normal.X ) < 0.8f ? Vector3.UnitX : Vector3.UnitY ) );
		var v = Vector3.Cross( normal, u );
		for ( var i = 0; i < _capCount; i++ )
		{
			var delta = _cap[ i ] - center;
			_angles[ i ] = MathF.Atan2( Vector3.Dot( delta, v ), Vector3.Dot( delta, u ) );
		}
		for ( var i = 1; i < _capCount; i++ )
		{
			var angle = _angles[ i ];
			var point = _cap[ i ];
			var j = i - 1;
			while ( j >= 0 && _angles[ j ] > angle )
			{
				_angles[ j + 1 ] = _angles[ j ];
				_cap[ j + 1 ] = _cap[ j ];
				j--;
			}
			_angles[ j + 1 ] = angle;
			_cap[ j + 1 ] = point;
		}
	}

	private void AddCapPoint( Vector3 point )
	{
		for ( var i = 0; i < _capCount; i++ )
		{
			if ( Vector3.DistanceSquared( point, _cap[ i ] ) < 1e-14f )
				return;
		}
		_cap[ _capCount++ ] = point;
	}

	private float MeasureVolume()
	{
		var origin = (Minimum + Maximum) * 0.5f;
		var volume = 0f;
		for ( var face = 0; face < 7; face++ )
		{
			for ( var j = 1; j < _counts[ face ] - 1; j++ )
			{
				volume += Vector3.Dot( _faces[ face ][ 0 ] - origin,
					Vector3.Cross( _faces[ face ][ j ] - origin, _faces[ face ][ j + 1 ] - origin ) ) / 6;
			}
		}
		return MathF.Abs( volume );
	}
}
