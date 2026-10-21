using Sandbox;

namespace Skateboarding.Engine;

/// <summary>
/// Reads the addon's animation files as they ship packed (tools/pack_poses.py), back to their original bytes: raw pose
/// containers arranged and packed ("SMZ1", the original length, the header length, then an LZ4 frame), or any file as
/// one plain LZ4 frame (the graph, the attribute bank). A file that is neither is read as it is.
/// </summary>
internal static class PackedPoses
{
	internal static byte[] Read( string path )
	{
		var data = FileSystem.Mounted.ReadAllBytes( path ).ToArray();
		if ( data.Length >= 12 && data[ 0 ] == 'S' && data[ 1 ] == 'M' && data[ 2 ] == 'Z' && data[ 3 ] == '1' )
			return UnpackPoses( data );
		// The LZ4 frame format's own magic number, 0x184D2204, little-endian.
		if ( data.Length >= 4 && data[ 0 ] == 0x04 && data[ 1 ] == 0x22 && data[ 2 ] == 0x4D && data[ 3 ] == 0x18 )
			return Sandbox.Compression.LZ4.DecompressFrame( data );
		return data;
	}

	internal static string ReadText( string path ) => System.Text.Encoding.UTF8.GetString( Read( path ) );

	private static byte[] UnpackPoses( byte[] data )
	{
		var length = data[ 4 ] | data[ 5 ] << 8 | data[ 6 ] << 16 | data[ 7 ] << 24;
		var headers = data[ 8 ] | data[ 9 ] << 8 | data[ 10 ] << 16 | data[ 11 ] << 24;
		var payload = Sandbox.Compression.LZ4.DecompressFrame( new System.ReadOnlySpan<byte>( data, 12, data.Length - 12 ) );
		return PackedPoseLayout.Restore( payload, headers, length );
	}
}
