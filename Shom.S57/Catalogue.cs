using Shom.ISO8211;
using S57.File;
using System.Collections.Generic;

namespace S57
{
    public class Catalogue
    {
        public DataRecord DataRecord { get; private set; }
        
        public uint RecordIdentificationNumber;
        
        public string FileName { get; private set; }

        public string fileLongName;
        public uint NavigationalPurpose;
        public uint CompilationScale;
        public double southernMostLatitude;
        public double westernMostLongitude;
        public double northernMostLatitude;
        public double easternMostLongitude;
        
        public Catalogue(DataRecord cr)
        {
            this.DataRecord = cr;
            BuildFromDataRecord();
        }

        private void BuildFromDataRecord()
        {
            // Record Identifier Field
            var catd = this.DataRecord.Fields.GetFieldByTag("CATD");
            if (catd != null)
            {
                var subFieldRow = catd.subFields.Values[0];
                var tagLookup = catd.subFields.TagIndex;
                RecordIdentificationNumber = (uint)subFieldRow.GetInt32(tagLookup.IndexOf("RCID")); //this one ist stored as integer, so implementing GetUint32 to do merely a cast will fail
                FileName = subFieldRow.GetString(tagLookup.IndexOf("FILE"));
                fileLongName = subFieldRow.GetString(tagLookup.IndexOf("LFIL"));                
                southernMostLatitude = subFieldRow.GetDouble(tagLookup.IndexOf("SLAT"));
                westernMostLongitude = subFieldRow.GetDouble(tagLookup.IndexOf("WLON"));
                northernMostLatitude = subFieldRow.GetDouble(tagLookup.IndexOf("NLAT"));
                easternMostLongitude = subFieldRow.GetDouble(tagLookup.IndexOf("ELON"));
            }   
        }        
    }
}
