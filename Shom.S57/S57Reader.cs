using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Shom.ISO8211;
using S57.File;
using System.Diagnostics;
using System.Linq;
using SimpleLogger;

namespace S57
{
    public class S57Reader
    {
        public S57Reader()
        { }

        //public static int mapIndex = 0;

        //public Cell cellInfo;
        //public BaseFile baseFile;
        //public UpdateFile updateFile;
        public CatalogueFile CatalogueFile { get; private set; }
        //public ProductInfo ProductInfo { get; private set; }

        public Dictionary<uint, Catalogue> ExchangeSetFiles = new Dictionary<uint, Catalogue>();
        public Dictionary<uint, Catalogue> BaseFiles = new Dictionary<uint, Catalogue>();

        byte[] fileByteArray;

        public void ReadCatalogue(ZipArchive archive)
        {
            var catalogEntry = archive.Entries.Single(item => item.Name.Equals("CATALOG.031"));

            using (var map = catalogEntry.Open())
            {
                var count = catalogEntry.Length;
                byte[] fileByteArray = new byte[count]; //consider re-using same byte array for next file to minimize new allocations
                var memoryStream = new MemoryStream(fileByteArray);
                map.CopyTo(memoryStream);
                memoryStream.Dispose();
                using (var reader = new Iso8211Reader(fileByteArray))
                {
                    BuildCatalogue(reader);

                    foreach (var bla in reader.tagcollector)
                        Console.WriteLine(bla);
                }
            }
        }

        public ProductInfo ReadProductInfo(ZipArchive archive, string MapName)
        {
            Stream S57map = null;
            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.Name.Equals(MapName))
                {
                    S57map = entry.Open();
                    int count = (int)entry.Length;
                    if (fileByteArray == null)
                        fileByteArray = new byte[count];
                    else
                    {
                        Array.Clear(fileByteArray, 0, fileByteArray.Length);
                        Array.Resize(ref fileByteArray, count);
                    }
                    MemoryStream memoryStream = new MemoryStream(fileByteArray);
                    S57map.CopyTo(memoryStream);
                    memoryStream.Dispose();

                    using (var reader = new Iso8211Reader(fileByteArray))
                    {
                        return new ProductInfo(reader);
                    }
                }
            }

            return null;
        }

        public Cell Read(ZipArchive archive, string mapName, bool ApplyUpdates)
        {
            Logger.Log($"Read({archive}, {mapName}, {ApplyUpdates}");
            mapName = Path.GetFileName(mapName);
            string basename = Path.GetFileNameWithoutExtension(mapName);

            var updatefiles = new SortedList<uint, ZipArchiveEntry>();
            foreach (var entry in archive.Entries.Where(item => item.Name.Contains(basename)))
            {
                var extension = Path.GetExtension(entry.Name);
                var index = uint.Parse(extension.Substring(1));
                updatefiles.Add(index, entry);
            }

            // process the first map (better be 000)
            var baseentry = updatefiles.First().Value;
            var S57map = baseentry.Open();
            int count = (int)baseentry.Length;
            if (fileByteArray == null)
                fileByteArray = new byte[count];
            else
            {
                Array.Clear(fileByteArray,0, fileByteArray.Length);
                Array.Resize(ref fileByteArray, count);
            }
            MemoryStream memoryStream = new MemoryStream(fileByteArray);
            S57map.CopyTo(memoryStream);
            memoryStream.Dispose();

            BaseFile baseFile = null;
            using (var reader = new Iso8211Reader(fileByteArray))
            {
                baseFile = new BaseFile(reader);
                foreach (var bla in reader.tagcollector)
                {
                    Console.WriteLine(bla);
                }
            }
            S57map.Dispose();

            if (ApplyUpdates)
            {
                foreach (var entry in updatefiles.Skip(1))
                {
                    var S57update = entry.Value.Open();
                    count = (int)entry.Value.Length;
                    Array.Clear(fileByteArray, 0, fileByteArray.Length);
                    Array.Resize(ref fileByteArray, count);
                    memoryStream = new MemoryStream(fileByteArray);
                    S57update.CopyTo(memoryStream);
                    memoryStream.Dispose();
                    using (var updatereader = new Iso8211Reader(fileByteArray))
                    {
                        var updateFile = new UpdateFile(updatereader);
                        baseFile.ApplyUpdateFile(updateFile);
                    }
                    S57update.Dispose();
                }
            }

            var cellInfo = new Cell(baseFile);
            baseFile.BindVectorPointersOfVectors();
            baseFile.BindVectorPointersOfFeatures();
            baseFile.BuildVectorGeometry();
            baseFile.BindFeatureObjectPointers();

            return cellInfo;
        }

        //public void Read(System.IO.Stream stream)
        //{
        //    //Stopwatch timer = new Stopwatch();
        //    //timer.Start();
        //    using (var reader = new Iso8211Reader(stream))
        //    {
        //        baseFile = new BaseFile(reader);
        //    }
        //    //timer.Stop();
        //    //Console.WriteLine(((double)(timer.Elapsed.TotalMilliseconds)).ToString("0.00 ms"));
        //    cellInfo = new Cell(baseFile);
        //    //timer.Start();
        //    baseFile.BindVectorPointersOfVectors();
        //    baseFile.BindVectorPointersOfFeatures();
        //    baseFile.BuildVectorGeometry();
        //    baseFile.BindFeatureObjectPointers();
        //    //timer.Stop();
        //    //Console.WriteLine(((double)(timer.Elapsed.TotalMilliseconds)).ToString("0.00 ms"));
        //}    

        private void BuildCatalogue(Iso8211Reader reader)
        {
            CatalogueFile = new CatalogueFile(reader);

            foreach (var cr in CatalogueFile.CatalogueRecords)
            {
                Catalogue catalog = new Catalogue(cr);
                uint key = catalog.RecordIdentificationNumber;
                if (!ExchangeSetFiles.ContainsKey(key) || !BaseFiles.ContainsKey(key))
                {
                    if (catalog.FileName.EndsWith(".000"))
                    {
                        BaseFiles.Add(key, catalog);
                    }
                    else
                    {
                        ExchangeSetFiles.Add(key, catalog);
                    }
                }
            }
        }
    }
}
