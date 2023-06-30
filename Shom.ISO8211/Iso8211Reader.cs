namespace Shom.ISO8211
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using SimpleLogger;

    public class Iso8211Reader : IDisposable
    {
        private const byte UnitTerminator = 0x1F;
        private const byte FieldTerminator = 0x1E;
        private const int sizeOfRecordLeader = 24;
        private int CurrentFileOffset;
        private ArraySegment<byte> readRecord;
        private readonly byte[] FileByteArray;

        public Iso8211Reader(Stream stream)
        {
            using(var memory = new MemoryStream())
            {
                stream.CopyTo(memory);
                this.FileByteArray = memory.ToArray();
            }

            CurrentFileOffset = 0;
            DataDescriptiveRecord = ReadDataDescriptiveRecord();
        }

        public DataDescriptiveRecord DataDescriptiveRecord { get; private set; }

        private List<string> TagCollector { get; set; } = new List<string>();  // #unused??

        private DataDescriptiveRecord ReadDataDescriptiveRecord()
        {
            var rec = new DataDescriptiveRecord();
            readRecord = new ArraySegment<byte>(FileByteArray, CurrentFileOffset, sizeOfRecordLeader);
            CurrentFileOffset += sizeOfRecordLeader;
            rec.Leader = ReadRecordLeader(readRecord);
            rec.Directory = ReadRecordDirectory(rec.Leader);
            if (rec.Leader.LeaderIdentifier != 'L')
            {
                throw new Exception("Reading DDR but LeaderIdentifier is not L");
            }
            rec.Fields = ReadDataDescriptiveRecordFields(rec.Leader, rec.Directory);

            return rec;
        }

        public DataRecord ReadDataRecord()
        {
            if (FileByteArray.Length - CurrentFileOffset < sizeOfRecordLeader) //detect  stream end when readbytes returns 0 works with stream of known length (e.g. files) and unknown length
            {
                return null;
            }

            var rec = new DataRecord();
            readRecord = new ArraySegment<byte>(FileByteArray, CurrentFileOffset, sizeOfRecordLeader);
            CurrentFileOffset += sizeOfRecordLeader;
            rec.Leader = ReadRecordLeader(readRecord);
            rec.Directory = ReadRecordDirectory(rec.Leader);
            rec.Fields = ReadDataRecordFields(rec.Leader, rec.Directory);
            return rec;
        }

        public void OutToLog()
        {
            foreach(var tag in this.TagCollector)
            {
                Logger.Log($"\t{tag}");
            }
        }

        private RecordLeader ReadRecordLeader(ArraySegment<byte> readRecord)
        {
            return new RecordLeader(readRecord);
        }

        private RecordDirectory ReadRecordDirectory(RecordLeader leader)
        {
            int count = 0;
            for (int i = CurrentFileOffset; i < FileByteArray.Length; i++)
            {
                if (FileByteArray[CurrentFileOffset + count] == FieldTerminator)
                {
                    break;
                }
                count++;
            }
            ArraySegment<byte> bytes = new ArraySegment<byte>(FileByteArray, CurrentFileOffset, count);
            CurrentFileOffset += count+1;
            return new RecordDirectory(leader.EntryMap.SizeOfTagField, leader.EntryMap.SizeOfLengthField,
                           leader.EntryMap.SizeOfPositionField, bytes);
        }
        private DataDescriptiveRecordFields ReadDataDescriptiveRecordFields(RecordLeader leader, RecordDirectory directory)
        {
            var fieldArea = new DataDescriptiveRecordFields();

            foreach (DirectoryEntry entry in directory)
            {
                bool isFileControlField = false;

                int result;
                if (Int32.TryParse(entry.FieldTag, out result))
                {
                    switch (result)
                    {
                        case 0:
                            isFileControlField = true; //tag is 0..0 (e.g. '0000')
                            break;
                        case 1:
                            //Record Identifier field - this is processed as a normal DataField (or DataDescriptiveField in the DataDescriptiveRecord)
                            break;
                        case 2:
                            throw new NotImplementedException("Processing User application field");
                        case 3:
                            throw new NotImplementedException( "Processing Announcer sequence or feature identifier field");
                        case 4 - 8:
                            throw new NotImplementedException( "Processing Special field tag reserved for future standardisation - " + entry.FieldTag);
                        case 9:
                            throw new NotImplementedException("Processing Recursive tree LINKS field");
                    }
                }

                if (isFileControlField)
                {
                    //This is a special field tag - the file control field
                    //byte[] fieldControls = bufferedReader.ReadBytes(leader.FieldControlLength);
                    ArraySegment<byte> fieldControls = new ArraySegment<byte>(FileByteArray, CurrentFileOffset, leader.FieldControlLength);
                    CurrentFileOffset += leader.FieldControlLength;
                    string externalFileTitle = ReadStringToTerminator();
                    string listOfFieldTagPairs = ReadStringToTerminator();
                    var fcf = new FileControlField( entry.FieldTag, fieldControls, externalFileTitle, listOfFieldTagPairs, leader.EntryMap.SizeOfTagField );

                    fieldArea.Add(fcf);
                }
                else
                {
                   //byte[] fieldControls = bufferedReader.ReadBytes(leader.FieldControlLength);
                    ArraySegment<byte> fieldControls = new ArraySegment<byte>(FileByteArray, CurrentFileOffset, leader.FieldControlLength);
                    CurrentFileOffset += leader.FieldControlLength;
                    string dataFieldName = ReadStringToTerminator();
                    string arrayDescriptor = ReadStringToTerminator();
                    string formatControls = ReadStringToTerminator();
                    var ddf = new DataDescriptiveField(entry.FieldTag, fieldControls, dataFieldName, arrayDescriptor,
                                                       formatControls);

                    fieldArea.Add(ddf);
                }
            }

            return fieldArea;
        }

        private DataRecordFields ReadDataRecordFields(RecordLeader leader, RecordDirectory directory)
        {
            var fieldArea = new DataRecordFields();

            foreach (DirectoryEntry entry in directory)
            {
                if (DataDescriptiveRecord == null)
                {
                    throw new NotImplementedException(
                        "Processing a Data Record before the Data Descriptive Record is set");
                }

                DataDescriptiveRecordField dataDescriptiveField = null;

                foreach (DataDescriptiveRecordField item in DataDescriptiveRecord.Fields)
                {
                    if (item.Tag == entry.FieldTag)
                    {
                        dataDescriptiveField = item;
                        break;
                    }
                }

                if (dataDescriptiveField == null)
                {
                    throw new Exception("Unable to find data descriptive field");
                }

                ArraySegment<byte> temp = new ArraySegment<byte>(FileByteArray, CurrentFileOffset, entry.FieldLength);
                CurrentFileOffset += entry.FieldLength;

                var df = new DataField(entry.FieldTag, (DataDescriptiveField)dataDescriptiveField, temp);

                fieldArea.Add(df);
            }

            return fieldArea;
        }

        private string ReadStringToTerminator()
        {
            int count = 0;
            for(int i = CurrentFileOffset; i<FileByteArray.Length; i++)
            {
                if (FileByteArray[CurrentFileOffset + count] == FieldTerminator || FileByteArray[CurrentFileOffset + count] == UnitTerminator)
                {
                    break;
                }
                count++;
            }
            ArraySegment<byte> bytes = new ArraySegment<byte>(FileByteArray, CurrentFileOffset, count);
            string temp = Encoding.ASCII.GetString(bytes.Array, bytes.Offset, count);
            CurrentFileOffset += count+1;
            return temp;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                //// free managed resources
                //if (bufferedReader != null)
                //{
                //    bufferedReader.Dispose();
                //}
            }
        }

    }
}