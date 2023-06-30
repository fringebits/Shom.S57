using System;
using System.Linq;

namespace S57
{
    public struct LongName : IEquatable<LongName>
    {
        public uint ProducingAgency { get; private set; }
        public uint FeatureIdentificationNumber { get; private set; }
        public uint FeatureIdentificationSubdivision { get; private set; }
        public LongName(byte[] bytes)
        {
            if (bytes.Length != 8)
            {
                throw new ArgumentException("Expected byte array with 8 bytes");
            }

            var values = bytes.Select(b => (uint)b).ToArray();

            this.ProducingAgency = values[0] | values[1] << 8;

            this.FeatureIdentificationNumber = values[2] | values[3] << 8 | values[4] << 16 | values[5] << 24;

            this.FeatureIdentificationSubdivision = values[6] | values[7] << 8;

            var pa = (uint)(bytes[0] + (bytes[1] * 256));
            var fin = (uint)(bytes[2] + (bytes[3] * 256) + (bytes[4] * 65536) + (bytes[5] * 16777216));
            var fis = (uint)(bytes[6] + (bytes[7] * 256));

            if (pa != ProducingAgency || fin != FeatureIdentificationNumber || fis != FeatureIdentificationSubdivision)
            {
                throw new Exception("Bad calculations.");
            }
        }

        public LongName(uint agen, uint fidn, uint fids)
        {
            ProducingAgency = agen;
            FeatureIdentificationNumber = fidn;
            FeatureIdentificationSubdivision = fids;
        }

        public override string ToString()
        {
            return $"{ProducingAgency}-{FeatureIdentificationNumber}-{FeatureIdentificationSubdivision}";
        }

        public bool Equals(LongName other)
        {
            return this.ProducingAgency == other.ProducingAgency && 
                this.FeatureIdentificationNumber == other.FeatureIdentificationNumber &&
                this.FeatureIdentificationSubdivision == other.FeatureIdentificationSubdivision;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
            {
                return false;
            }
            return obj is LongName && Equals((LongName)obj);
        }

        public override int GetHashCode()
        {
            int hash = 17;
            hash = hash * 29 + ProducingAgency.GetHashCode();
            hash = hash * 29 + FeatureIdentificationNumber.GetHashCode();
            hash = hash * 29 + FeatureIdentificationSubdivision.GetHashCode();
            return hash;
        }

    }
    //public class LongName
    //{
    //    public LongName(byte[] bytes)
    //    {
    //        if (bytes.Length != 8)
    //        {
    //            throw new ArgumentException("Expected byte array with 8 items");
    //        }

    //        ProducingAgency = + (uint)(bytes[1] << 8)
    //                          + (uint)(bytes[0]);

    //        FeatureIdentificationNumber = (uint)(bytes[5] << 24)
    //                                    + (uint)(bytes[4] << 16)
    //                                    + (uint)(bytes[3] << 8)
    //                                    + (uint)(bytes[2]);

    //        FeatureIdentificationSubdivision = + (uint)(bytes[7] << 8)
    //                                           + (uint)(bytes[6]);
    //    }

    //    public LongName(uint agen, uint fidn, uint fids)
    //    {
    //        ProducingAgency = agen;
    //        FeatureIdentificationNumber = fidn;
    //        FeatureIdentificationSubdivision = fids;
    //    }

    //    public override string ToString()
    //    {
    //        return string.Format("{0}-{1}-{2}", ProducingAgency, FeatureIdentificationNumber, FeatureIdentificationSubdivision);
    //    }

    //    public string ToCarisString()
    //    {
    //        if (ProducingAgency == 170)
    //        {
    //            return string.Format("FR {0} {1}", FeatureIdentificationNumber, FeatureIdentificationSubdivision);
    //        }
    //        else
    //        {
    //            return string.Format("{0} {1} {2}", ProducingAgency, FeatureIdentificationNumber, FeatureIdentificationSubdivision);
    //        }
    //    }

    //    public uint ProducingAgency { get; private set; }

    //    public uint FeatureIdentificationNumber { get; private set; }

    //    public uint FeatureIdentificationSubdivision { get; private set; }
    //}
}
