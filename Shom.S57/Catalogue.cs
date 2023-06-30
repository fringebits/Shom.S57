using Shom.ISO8211;
using S57.File;
using System.Collections.Generic;
using MapCore;
using Utilities;
using SimpleLogger;

namespace S57
{
    public class Catalogue : IOutToLog
    {
        public DataRecord DataRecord { get; private set; }
        
        public uint RecordIdentificationNumber { get; private set; }

        public string FileName { get; private set; }

        public string FileLongName { get; private set; }

        public uint NavigationalPurpose { get; private set; }

        public uint CompilationScale { get; private set; }

        public BoundingBox BoundingBox { get; private set; }
        
        public Catalogue(DataRecord cr)
        {
            this.DataRecord = cr;
            BuildFromDataRecord();
        }

        public void OutToLog(int depth)
        {
            Logger.Log($"Catalogue {this.FileName}");
            Logger.Log($"{this.BoundingBox}");
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
                FileLongName = subFieldRow.GetString(tagLookup.IndexOf("LFIL"));

                this.BoundingBox = new BoundingBox(
                    new Location(subFieldRow.GetDouble(tagLookup.IndexOf("SLAT")), subFieldRow.GetDouble(tagLookup.IndexOf("WLON"))),
                    new Location(subFieldRow.GetDouble(tagLookup.IndexOf("NLAT")), subFieldRow.GetDouble(tagLookup.IndexOf("ELON"))));
            }   
        }
    }
}
