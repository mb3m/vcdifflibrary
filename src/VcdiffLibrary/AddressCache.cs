using System;

namespace VcdiffLibrary
{
	/// <summary>
	/// Cache used for encoding/decoding addresses.
	/// </summary>
	/// <remarks>
	/// <para>
	/// This code is based on (is a copy of) CodeTable from Miscellaneous Utility Library
	/// (http://www.yoda.arachsys.com/csharp/miscutil/) written by Jon Skeet and Marc Gravell.
	/// </para>
	/// </remarks>
	internal sealed class AddressCache
	{
		/// <summary>
		/// The address was encoded by itself as an integer.
		/// </summary>
		internal const byte VCD_SELF = 0;

		/// <summary>
		/// The address was encoded as the integer value "here - addr".
		/// </summary>
		internal const byte VCD_HERE = 1;

		/// <summary>
		/// Array of slots containing an address used for encoding address nearby to previously encoded addresses.
		/// </summary>
		private readonly int[] near;

		/// <summary>
		/// Size of near array.
		/// </summary>
		private readonly int nearSize;

		/// <summary>
		/// Array of multiple of 256 slots, each containing an address.
		/// </summary>
		private readonly int[] same;

		/// <summary>
		/// Size of same array.
		/// </summary>
		private readonly int sameSize;

		/// <summary>
		/// Index of the next near slot to use.
		/// </summary>
		private int nextNearSlot;

		private byte[] addresses;

		private int position;

		/// <summary>
		/// Initialize a new instance of <see cref="AddressCache"/> with the specified sizes for the near cache and the 
		/// same cache arrays.
		/// </summary>
		/// <param name="nearSize">
		/// Size of the near cache array.
		/// By default the size should be 4.
		/// </param>
		/// <param name="sameSize">
		/// Size of the same cache array, as a multiple of 256 slots.
		/// By default the size should be 3.
		/// </param>
		public AddressCache(int nearSize, int sameSize)
		{
			this.nearSize = nearSize;
			this.sameSize = sameSize;
			near = new int[nearSize];
			same = new int[sameSize*256];
		}

		/// <summary>
		/// Initialize the cache with the provided <paramref name="newAddresses"/>.
		/// </summary>
		public void Init(byte[] newAddresses)
		{
			nextNearSlot = 0;
			Array.Clear(near, 0, near.Length);
			Array.Clear(same, 0, same.Length);

			addresses = newAddresses;
			position = 0;
		}

		/// <summary>
		/// Decode the address to use, based on the current target position <paramref name="here"/>
		/// and the instruction mode <paramref name="mode"/>.
		/// </summary>
		/// <remarks>
		/// This method implements the section 5.2 and 5.3 of the RFC.
		/// </remarks>
		/// <param name="here">Address of the current location in the target data.</param>
		/// <param name="mode">Address mode, as described in the 5.3 section of the RFC.</param>
		/// <returns>Address decoded.</returns>
		public int DecodeAddress(int here, byte mode)
		{
			int address;
			if (mode == VCD_SELF)
			{
				// Self Mode : This mode has value 0.
				// The address was encoded by itself as an integer.
				address = IOUtils.ReadBigEndian7BitEncodedInt(addresses, ref position);
			}
			else if (mode == VCD_HERE)
			{
				// Here Mode : This mode has value 1.
				// The address was encoded as the integer value "here - addr".
				address = here - IOUtils.ReadBigEndian7BitEncodedInt(addresses, ref position);
			}
			else if (mode - 2 < nearSize)
			{
				// Near Mode : The "near modes" are in the range [2,s_near+1]
				// Let m be the mode of the address encoding.
				// The address was encoded as the integer value "addr - near[m-2]".
				address = near[mode - 2] + IOUtils.ReadBigEndian7BitEncodedInt(addresses, ref position);
			}
			else
			{
				// Same Mode : The "same modes" are in the range [s_near+2,s_near+s_same+1].
				// Let m be the mode of the encoding.
				// The address was encoded as a single byte b such that "addr == same[(m - (s_near+2))*256 + b]".
				var m = mode - (2 + nearSize);
				address = same[(m*256) + addresses[position]];
			}

			// Update
			if (nearSize > 0)
			{
				near[nextNearSlot] = address;
				nextNearSlot = (nextNearSlot + 1)%nearSize;
			}

			if (sameSize > 0)
			{
				same[address%(sameSize*256)] = address;
			}

			return address;
		}
	}
}