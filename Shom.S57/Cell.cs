using System;
using S57.File;
using Shom.ISO8211; //this namespace is the current home of the extension methods

namespace S57
{
    public class Cell
    {
        public Cell(BaseFile baseFile)
        {
            this.BaseFile = baseFile;
        }

        public BaseFile BaseFile { get; private set; }

        public string Name => "Unknown";

        public string Description => "None";

        public BoundingBox BoundingBox
        {
            get {
                var box = new BoundingBox();
                var mapCovers = BaseFile.GetFeaturesOfClass(S57Obj.M_COVR);
                foreach (var mapCover in mapCovers)
                {
                    var geom = mapCover.GetGeometry(false);
                    if (geom is Area)
                    {
                        var area = geom as Area;
                        foreach (var point in area.points)
                        {
                            if (point.X < box.westLongitude)
                            {
                                box.westLongitude = point.X;
                            }
                            else if (point.X > box.eastLongitude)
                            {
                                box.eastLongitude = point.X;
                            }
                            if (point.Y > box.northLatitude)
                            {
                                box.northLatitude = point.Y;
                            }
                            else if (point.Y < box.southLatitude)
                            {
                                box.southLatitude = point.Y;
                            }
                        }
                    }
                }
                return box;
            }
        }

        private string GetDataSetGeneralInformationRecord(string field, int subFieldRow, string subFieldTag)
        {
            return this.BaseFile.DataSetGeneralInformationRecord.Fields
                .GetFieldByTag(field)
                .subFields.GetString(subFieldRow, subFieldTag);
        }

        private Int32 GetDataSetGeneralInformationRecordAsInt(string field, int subFieldRow, string subFieldTag)
        {
            var rec = GetDataSetGeneralInformationRecord(field, subFieldRow, subFieldTag);
            return Int32.Parse(rec);
        }

        public int EditionNumber => GetDataSetGeneralInformationRecordAsInt("DSID", 0 ,"EDTN"); 

        public int UpdateNumber => GetDataSetGeneralInformationRecordAsInt("DSID", 0, "UPDN");

        public uint IntendedUsage
        {
            get
            {
 
                return BaseFile.DataSetGeneralInformationRecord.Fields.GetFieldByTag("DSID").subFields.GetUInt32(0, "INTU");
            }
        }

        public string DataSetName
        {
            get
            {
                
                return BaseFile.DataSetGeneralInformationRecord.Fields.GetFieldByTag("DSID").subFields.GetString(0, "DSNM"); 
            }
        }

        public DateTime UpdateApplicationDate
        {
            get
            {
                return ConvertToDateTime(BaseFile.DataSetGeneralInformationRecord.Fields.GetFieldByTag("DSID").subFields.GetString(0, "UDAT"));
            }
        }

        public DateTime IssueDate
        {
            get
            {
                return ConvertToDateTime(BaseFile.DataSetGeneralInformationRecord.Fields.GetFieldByTag("DSID").subFields.GetString(0, "ISDT"));
            }
        }

        public Agency ProducingAgency
        {
            get
            {                
                return new Agency(BaseFile.DataSetGeneralInformationRecord.Fields.GetFieldByTag("DSID").subFields.GetUInt32(0, "AGEN"));
            }
        }

        public string Comment
        {
            get
            {
                return BaseFile.DataSetGeneralInformationRecord.Fields.GetFieldByTag("DSID").subFields.GetString(0, "COMT");
            }
        }

        public Datum VerticalDatum
        {
            get
            {
                return new Datum(BaseFile.DataSetGeographicReferenceRecord.Fields.GetFieldByTag("DSPM").subFields.GetUInt32(0, "VDAT"));
            }
        }

        public Datum SoundingDatum
        {
            get
            {
                return new Datum(BaseFile.DataSetGeographicReferenceRecord.Fields.GetFieldByTag("DSPM").subFields.GetUInt32(0, "SDAT"));
            }
        }

        private DateTime ConvertToDateTime(string date)
        {
            return new DateTime(Int32.Parse(date.Substring(0, 4)), 
                Int32.Parse(date.Substring(4, 2)), 
                Int32.Parse(date.Substring(6, 2)));
        }

        public Feature GetFeature(int index)
        {
            return null;
        }

        public Vector GetVector(int index)
        {
            return null;
        }

    }
}