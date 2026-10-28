using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace OptiCore.Services
{
    public sealed class L3Domain
    {
        public int SizeKb { get; init; }
        public ulong Mask { get; init; }   // logical processors sharing this L3 (processor group 0)
        public int LowestCpu => Mask == 0 ? int.MaxValue : System.Numerics.BitOperations.TrailingZeroCount(Mask);
    }

    public sealed class CpuTopology
    {
        public bool Valid { get; init; }
        public int L1DataKb { get; init; }
        public int L2Kb { get; init; }
        public List<L3Domain> L3 { get; init; } = new();   // ordered by lowest logical CPU, i.e. CCD0 first
    }

    // Reads the real cache topology from the OS instead of inferring it from the model name.
    // Matters because V-Cache layouts differ per SKU: 7950X3D / 9950X3D carry the extra cache on one CCD
    // only, while the 9950X3D2 carries it on both. A model-name table cannot keep up with new SKUs.
    public static class CpuTopologyService
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetLogicalProcessorInformationEx(int relationshipType, IntPtr buffer, ref int returnedLength);

        private const int RelationCache = 2;
        private const int RelationAll = 0xFFFF;
        private const int CacheUnified = 0;
        private const int CacheData = 2;

        public static CpuTopology Read()
        {
            IntPtr buf = IntPtr.Zero;
            try
            {
                int len = 0;
                GetLogicalProcessorInformationEx(RelationAll, IntPtr.Zero, ref len);   // fails with ERROR_INSUFFICIENT_BUFFER, fills len
                if (len <= 0) return new CpuTopology { Valid = false };

                buf = Marshal.AllocHGlobal(len);
                if (!GetLogicalProcessorInformationEx(RelationAll, buf, ref len)) return new CpuTopology { Valid = false };

                int l1d = 0, l2 = 0;
                var l3 = new List<L3Domain>();
                int off = 0;
                while (off < len)
                {
                    int relationship = Marshal.ReadInt32(buf, off);
                    int size = Marshal.ReadInt32(buf, off + 4);
                    if (size <= 0) break;

                    if (relationship == RelationCache)
                    {
                        // CACHE_RELATIONSHIP: Level@8, CacheSize@12, Type@16, GroupMask.Mask@40, GroupMask.Group@48
                        int level = Marshal.ReadByte(buf, off + 8);
                        int cacheKb = (int)((uint)Marshal.ReadInt32(buf, off + 12) / 1024);
                        int type = Marshal.ReadInt32(buf, off + 16);
                        ulong mask = (ulong)Marshal.ReadInt64(buf, off + 40);
                        int group = Marshal.ReadInt16(buf, off + 48);

                        if (level == 1 && type == CacheData && l1d == 0) l1d = cacheKb;
                        else if (level == 2 && type == CacheUnified && l2 == 0) l2 = cacheKb;
                        else if (level == 3 && type == CacheUnified && group == 0)
                            l3.Add(new L3Domain { SizeKb = cacheKb, Mask = mask });
                    }
                    off += size;
                }

                // Sanity check on the parse: a sane Zen core reports L1d >= 16 KB and L2 >= 256 KB.
                // If those are off, the struct offsets are wrong and the L3 numbers cannot be trusted.
                bool valid = l3.Count > 0 && l1d >= 16 && l2 >= 256;
                return new CpuTopology
                {
                    Valid = valid,
                    L1DataKb = l1d,
                    L2Kb = l2,
                    L3 = l3.OrderBy(d => d.LowestCpu).ToList()
                };
            }
            catch
            {
                return new CpuTopology { Valid = false };
            }
            finally
            {
                if (buf != IntPtr.Zero) Marshal.FreeHGlobal(buf);
            }
        }
    }
}
