namespace S57
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using MapCore;
    using S57.File;
    using Shom.ISO8211; //this namespace is the current home of the extension methods
    using SimpleLogger;

    public class Cell : Utilities.IOutToLog
    {
        public Cell(BaseFile baseFile)
        {
            this.BaseFile = baseFile;
        }

        public Dictionary<LongName, Feature> Features => BaseFile.Features;

        private BaseFile BaseFile { get; set; }

        public string Name => this.DataSetName;

        public string Description => this.Comment;

        public BoundingBox BoundingBox
        {
            get {
                BoundingBox box = null;
                var mapCovers = BaseFile.GetFeaturesOfClass(S57Obj.M_COVR);
                foreach (var mapCover in mapCovers)
                {
                    var area = mapCover.Geometry as Area;
                    if (area != null)
                    {
                        box = BoundingBox.Union(box, area.Points.Select(p => new Location(p.Y, p.X)));
                    }
                }
                return box;
            }
        }

        public int EditionNumber => GetRecordAsInt("DSID", 0 ,"EDTN"); 

        public int UpdateNumber => GetRecordAsInt("DSID", 0, "UPDN");

        public uint IntendedUsage => GetRecordAsUInt("DSID", 0, "INTU");

        public string DataSetName => GetRecordAsString("DSID", 0, "DSNM");

        public DateTime UpdateApplicationDate => GetRecordAsDate("DSID", 0, "UDAT");

        public DateTime IssueDate => GetRecordAsDate("DSID", 0, "ISDT");

        public Agency ProducingAgency => new Agency(GetRecordAsUInt("DSID", 0, "AGEN"));

        public string Comment => GetRecordAsString("DSID", 0, "COMT");

        public Datum VerticalDatum => new Datum(GetRecordAsUInt("DSID", 0, "VDAT"));

        public Datum SoundingDatum => new Datum(GetRecordAsUInt("DSID", 0, "SDAT"));

        public override string ToString()
        {
            return $"{this.Name}, {this.Description}, {this.IssueDate}, {this.BoundingBox}";
        }

        public void OutToLog(int depth = 0)
        {
            Logger.Log($"CELL - {this}");
            Logger.Log($"\t{Comment}");
            foreach(var feature in this.Features)
            {
                feature.Value.OutToLog(depth+1);
            }
        }

        private string GetRecordAsString(string field, int subFieldRow, string subFieldTag)
        {
            return this.BaseFile.DataSetGeneralInformationRecord.Fields
                .GetFieldByTag(field)
                .subFields.GetString(subFieldRow, subFieldTag);
        }

        private Int32 GetRecordAsInt(string field, int subFieldRow, string subFieldTag)
        {
            return this.BaseFile.DataSetGeneralInformationRecord.Fields
                .GetFieldByTag(field)
                .subFields.GetInt32(subFieldRow, subFieldTag);
            //var rec = GetDataSetGeneralInformationRecord(field, subFieldRow, subFieldTag);
            //return Int32.Parse(rec);
        }

        private UInt32 GetRecordAsUInt(string field, int subFieldRow, string subFieldTag)
        {
            return this.BaseFile.DataSetGeneralInformationRecord.Fields
                .GetFieldByTag(field)
                .subFields.GetUInt32(subFieldRow, subFieldTag);
        }

        private DateTime GetRecordAsDate(string field, int subFieldRow, string subFieldTag)
        {
            var rec = GetRecordAsString(field, subFieldRow, subFieldTag);

            // #todo: parse the date (with error handling)
            return new DateTime(Int32.Parse(rec.Substring(0, 4)),
                Int32.Parse(rec.Substring(4, 2)),
                Int32.Parse(rec.Substring(6, 2)));
        }
    }
}