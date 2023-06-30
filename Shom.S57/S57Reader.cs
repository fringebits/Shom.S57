using System;
using System.IO;
using System.IO.Compression;
using System.Collections.Generic;
using Shom.ISO8211;
using S57.File;
using System.Diagnostics;
using System.Linq;
using SimpleLogger;
using Utilities;

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

        public void ReadCatalogue(ZipArchive archive)
        {
            var catalogEntry = archive.Entries.Single(item => item.Name.Equals("CATALOG.031"));

            using (var map = catalogEntry.Open())
            {
                using (var reader = new Iso8211Reader(map))
                {
                    BuildCatalogue(reader);
                }
            }
        }

        public ProductInfo ReadProductInfo(ZipArchive archive, string MapName)
        {
            ProductInfo result = null;

            foreach (ZipArchiveEntry entry in archive.Entries)
            {
                if (entry.Name.Equals(MapName))
                {
                    using (var map = entry.Open())
                    using (var reader = new Iso8211Reader(map))
                    {
                        result = new ProductInfo(reader);
                        break;
                    }
                }
            }

            return result;
        }

        public Cell Read(ZipArchive archive, string mapName, bool ApplyUpdates)
        {
            Logger.Log($"Read({mapName}, ApplyUpdates={ApplyUpdates}");

            mapName = Path.GetFileName(mapName);
            string basename = Path.GetFileNameWithoutExtension(mapName);

            var updatefiles = new SortedList<uint, ZipArchiveEntry>();
            foreach (var entry in archive.Entries.Where(item => item.Name.Contains(basename)))
            {
                var extension = Path.GetExtension(entry.Name);
                var index = uint.Parse(extension.Substring(1));
                updatefiles.Add(index, entry);
                Logger.Log($"\t{index} -- {entry.Name}");
            }

            // process the first map (better be 000)
            var baseentry = updatefiles.First().Value;
            BaseFile baseFile = null;
            using (var map = baseentry.Open())
            using (var reader = new Iso8211Reader(map))
            {
                baseFile = new BaseFile(reader);
                reader.OutToLog();
            }

            if (ApplyUpdates)
            {
                foreach (var entry in updatefiles.Skip(1))
                {
                    using (var stream = entry.Value.Open())
                    using (var update = new Iso8211Reader(stream))
                    {
                        var updateFile = new UpdateFile(update);
                        baseFile.ApplyUpdateFile(updateFile);
                    }
                }
            }

            var cellInfo = new Cell(baseFile);
            baseFile.BindVectorPointersOfVectors();
            baseFile.BindVectorPointersOfFeatures();
            baseFile.BuildVectorGeometry();
            baseFile.BindFeatureObjectPointers();

            return cellInfo;
        }

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
