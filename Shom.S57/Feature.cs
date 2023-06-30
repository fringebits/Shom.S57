using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Shom.ISO8211;
using S57.File;
using System.Linq;
using SimpleLogger;

namespace S57
{
    // Feature Record
    //  0001 ISO/IEC 8211 Record Identifier
    //      FRID Feature Record Identifier
    //          FOID Feature Object Identifier
    //          <R> - ATTF Attributes
    //          <R> - NATF National Attributes
    //          FFPC Feature Record To Feature Object Pointer Control
    //          <R> - FFPT Feature Record To Feature Object Pointer
    //          FSPC Feature Record To Spatial Record Pointer Control
    //          <R> - FSPT Feature to Spatial Record Pointer
    public class Feature : Utilities.IOutToLog
    {
        public Feature(NAMEkey _namekey, DataRecord _FeatureRecord)
        {
            NameKey = _namekey;
            FeatureRecord = _FeatureRecord;
            var fspt = _FeatureRecord.Fields.GetFieldByTag("FSPT");
            if (fspt != null)
            {
                enhVectorPtrs = new VectorRecordPointer(fspt.subFields);
            }

            var ffpt = _FeatureRecord.Fields.GetFieldByTag("FFPT");
            if (ffpt != null)
            {
                enhFeaturePtrs = new FeatureObjectPointer(ffpt.subFields);
            }

            // FRID : Feature Record Identifier
            var frid = _FeatureRecord.Fields.GetFieldByTag("FRID");
            if (frid != null)
            {
                Primitive = (GeometricPrimitive)frid.subFields.GetUInt32(0, "PRIM");
                Group = frid.subFields.GetUInt32(0, "GRUP");
                ObjectCode = (S57Obj)frid.subFields.GetUInt32(0, "OBJL");
            }

            // FOID : Feature Object Identifier
            var foid = _FeatureRecord.Fields.GetFieldByTag("FOID");
            if (foid != null)
            {
                var subFieldRow = foid.subFields.Values[0];
                var tagLookup = foid.subFields.TagIndex;
                var agen = subFieldRow.GetUInt32(tagLookup.IndexOf("AGEN"));
                var fidn = subFieldRow.GetUInt32(tagLookup.IndexOf("FIDN"));
                var fids = subFieldRow.GetUInt32(tagLookup.IndexOf("FIDS"));
                this.LongName = new LongName(agen, fidn, fids);
            }

            // ATTF : Attributes
            var attr = _FeatureRecord.Fields.GetFieldByTag("ATTF");
            if (attr != null)
            {
                Attributes = Vector.GetAttributes(attr);
            }

            // NATF : National attributes NATF.
            var natf = _FeatureRecord.Fields.GetFieldByTag("NATF");
            if (natf != null)
            {
                var natfAttr = Vector.GetAttributes(natf);
                if (Attributes != null)
                {
                    foreach (var entry in natfAttr)
                    {
                        Attributes.Add(entry.Key, entry.Value);
                    }
                }
                else
                {
                    Attributes = natfAttr;
                }
            }
        }

        // FRID : Feature Record Identifier Field
        public NAMEkey NameKey { get; private set; }
        public DataRecord FeatureRecord { get; private set; }
        public Dictionary<S57Att, string> Attributes { get; private set; }     // ATTF Attributes
        public VectorRecordPointer enhVectorPtrs { get; private set; }
        public FeatureObjectPointer enhFeaturePtrs { get; private set; }
        public GeometricPrimitive Primitive { get; private set; }            // PRIM        // FOID : 
        public uint Group { get; private set; }                              // GRUP     
        public S57Obj ObjectCode { get; private set; }                         // OBJL
        public LongName LongName { get; private set; }                           // FOID Object Identifier Field

        // OBJNAM
        public string Name => GetAttribute(S57Att.OBJNAM);

        public Geometry Geometry => GetGeometry(true); 

        public void OutToLog(int depth)
        {
            Logger.Log($"Feature Name={Name}, NameKey={NameKey}, LongName={LongName}");
            if (Attributes != null)
            {
                Logger.Log($"\tAttributes: ({Attributes.Count})");
                foreach(var pair in Attributes)
                {
                    Logger.Log($"\t\t{pair.Key} = {pair.Value}");
                }
            }

            if (this.Geometry != null)
            {
                this.Geometry.OutToLog(depth+1);
            }

        }

        public List<Dictionary<S57Att, string>> GetSpacialAttributes()
        {
            //note: returning all QUAPOS Value of all spacial components making up e.g. an area is wastefull: 
            //a procedure such as QUAPNT02 has conditions leading to an early out, which is more effective. 
            //Therefore consider implementing the loops below directly in procedures such as QUAPOS: 
            //would avoid allocation of a new List of Dictionaries and such wastefull exhaustive accumulation
            List<Dictionary<S57Att, string>> temp = new List<Dictionary<S57Att, string>>();
            if (Primitive == GeometricPrimitive.Point)
            {
                if (enhVectorPtrs.VectorList[0] == null || enhVectorPtrs.VectorList[0].Attributes == null)
                {
                    return null;
                }

                temp.Add(enhVectorPtrs.VectorList[0].Attributes);
                return temp;
            }
            else if (Primitive == GeometricPrimitive.Line || Primitive == GeometricPrimitive.Area)
            {
                for (int i = 0; i < enhVectorPtrs.VectorList.Count; i++)
                {
                    if (enhVectorPtrs.VectorList[i] == null)
                    {
                        continue;
                    }

                    ////note: only the contributing line and area features appear to have QUAPOS set. the contributing points have Attributes=null
                    //for (int j = 0; j < enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList.Count; j++)
                    //{
                    //    if (enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[j] == null) break;
                    //    if (enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[j].Attributes == null) continue;
                    //    temp.Add(enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[j].Attributes); 
                    //}

                    if (enhVectorPtrs.VectorList[i] == null) 
                    {
                        continue;
                    }

                    if (enhVectorPtrs.VectorList[i].Attributes == null)
                    {
                        continue;
                    }

                    temp.Add(enhVectorPtrs.VectorList[i].Attributes);
                }
                return temp;
            }
            return null; //return value when feature is neither point, line or area
        }

        private Geometry GetGeometry(bool returnFlatPolygon)
        {
            int count;
            switch (Primitive)
            {
                case GeometricPrimitive.Point:
                    {
                        if (enhVectorPtrs.Values[0] == null || enhVectorPtrs.VectorList[0] == null) return null;
                        return enhVectorPtrs.VectorList[0].Geometry;
                    }
                case GeometricPrimitive.Line:
                    {
                        //initialize StartNode Pointer for later check how to build geometry
                        //VectorRecordPointer StartNode = new VectorRecordPointer();
                        Vector StartNode;
                        //initialize Contour to accumulate lines
                        Line Contour = new Line();
                        for (int i = 0; i < enhVectorPtrs.Values.Count; i++)
                        {
                            //first, check if vector exist, and if it is supposed to be visible 
                            //(to improve: masked points should still be added for correct topology, just not rendered later)
                            int mask = enhVectorPtrs.TagIndex.IndexOf("MASK");
                            int ornt = enhVectorPtrs.TagIndex.IndexOf("ORNT");
                            if (enhVectorPtrs.VectorList[i] == null || enhVectorPtrs.Values[i].GetUInt32(mask) == (uint)Masking.Mask) break;

                            //next, check if edge needs to be reversed for the intended usage 
                            //Do this on a copy to not mess up original record wich might be used elsewhere
                            var edge = new List<Point>((enhVectorPtrs.VectorList[i].Geometry as Line).Points);
                            if (enhVectorPtrs.Values[i].GetUInt32(ornt) == (uint)Orientation.Reverse)
                            {
                                edge.Reverse();
                                //note: Reversing is not reversing the VectorRecordPointers, see S57 Spec 3.1 section 3.20 (i.e. index [0] is now end node instead of start node)
                                //therefore assign private StartNode pointer for some later checks 
                                StartNode = enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[1];
                            }
                            else
                            {
                                StartNode = enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[0];
                            }

                            //now check if Contour has already accumulated points, if not just add current edge
                            count = Contour.Points.Count;
                            if (count > 0)
                            {
                                //now check if existing contour should be extended
                                if (Contour.Points[count - 1] == StartNode.Geometry as Point)
                                {
                                    Contour.Points.AddRange(edge.GetRange(1, edge.Count - 1));
                                }
                            }
                            else
                            {
                                //add current edge points to new contour
                                Contour.Points.AddRange(edge);
                            }
                        }
                        //done, finish up and return
                        return Contour;
                    }
                case GeometricPrimitive.Area:
                    {
                        if (returnFlatPolygon)
                        {
                            //initialize StartNode Pointer for check how to build geometry
                            Vector StartNode;
                            int currentContourStartIndex=0;
                            
                            //initialize Contour to accumulate boundaries
                            FlatPolygon Contour = new FlatPolygon();
                            for (int i = 0; i < enhVectorPtrs.Values.Count; i++)
                            {
                                //first, check if vector exist, and if it is supposed to be visible
                                //(to improve: masked points should still be added for correct topology, just not rendered later)
                                int mask = enhVectorPtrs.TagIndex.IndexOf("MASK");
                                int ornt = enhVectorPtrs.TagIndex.IndexOf("ORNT");
                                if (enhVectorPtrs.VectorList[i] == null || enhVectorPtrs.Values[i].GetUInt32(mask) == (uint)Masking.Mask) break;

                                //next, check if edge needs to be reversed for the intended usage 
                                //Do this on a copy to not mess up original record wich might be used elsewhere
                                var edge = new List<Point>((enhVectorPtrs.VectorList[i].Geometry as Line).Points);
                                if (enhVectorPtrs.Values[i].GetUInt32(ornt) == (uint)Orientation.Reverse)
                                {
                                    edge.Reverse();
                                    //note: Reversing is not reversing the VectorRecordPointers, see S57 Spec 3.1 section 3.20 (i.e. index [0] is now end node instead of start node)
                                    //therefore assign private StartNode pointer for some later checks 
                                    StartNode = enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[1];
                                }
                                else
                                {
                                    StartNode = enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[0];
                                }

                                //now check if Contour has already accumulated points, if not just add current edge
                                count = Contour.Points.Count;
                                if (count > 0)
                                {
                                    //now check if existing contour should be extended, or if also a holeIndex indicating start of next interior boundery should be added
                                    if (Contour.Points[count - 1] == StartNode.Geometry as Point)
                                    {
                                        Contour.Points.AddRange(edge.GetRange(1, edge.Count - 1));
                                    }
                                    else
                                    {
                                        //verify that polygone is closed last point in list equals first
                                        if (Contour.Points[count - 1] == Contour.Points[currentContourStartIndex])
                                        {
                                            Contour.HolesIndices.Add(count);
                                            currentContourStartIndex=count;
                                            //start accumulatating hole points with current edge
                                            Contour.Points.AddRange(edge);
                                        }
                                        //else
                                        //{
                                        //    Debug.WriteLine("Panic: current polygon is not complete, and current egde is not extending it");
                                        //}
                                    }
                                }
                                else
                                {
                                    //add current edge points to new contour
                                    Contour.Points.AddRange(edge);
                                }
                            }
                            return Contour;
                        }
                        else
                        {
                            //initialize PolygonSet to accumulate 1 exterior and (if available) repeated interior boundaries
                            PolygonSet ContourSet = new PolygonSet();
                            //initialize StartNode Pointer for check how to build geometry
                            Vector StartNode;
                            //initialize Contour to accumulate boundaries
                            Area Contour = new Area();
                            for (int i = 0; i < enhVectorPtrs.Values.Count; i++)
                            {
                                //first, check if vector exist, and if it is supposed to be visible
                                //(to improve: masked points should still be added for correct topology, just not rendered later)
                                int mask = enhVectorPtrs.TagIndex.IndexOf("MASK");
                                int ornt = enhVectorPtrs.TagIndex.IndexOf("ORNT");
                                if (enhVectorPtrs.VectorList[i] == null || enhVectorPtrs.Values[i].GetUInt32(mask) == (uint)Masking.Mask) break;

                                //next, check if edge needs to be reversed for the intended usage 
                                //Do this on a copy to not mess up original record wich might be used elsewhere
                                var edge = new List<Point>((enhVectorPtrs.VectorList[i].Geometry as Line).Points);
                                if (enhVectorPtrs.Values[i].GetUInt32(ornt) == (uint)Orientation.Reverse)
                                {
                                    edge.Reverse();
                                    //note: Reversing is not reversing the VectorRecordPointers, see S57 Spec 3.1 section 3.20 (i.e. index [0] is now end node instead of start node)
                                    //therefore assign private StartNode pointer for some later checks 
                                    StartNode = enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[1];
                                }
                                else
                                {
                                    StartNode = enhVectorPtrs.VectorList[i].enhVectorPtrs.VectorList[0];
                                }

                                //now check if Contour has already accumulated points, if not just add current edge
                                count = Contour.Points.Count;
                                if (count > 0)
                                {
                                    //now check if existing contour should be extended, or if a new one for the next interior boundery should be started 
                                    if (Contour.Points[count - 1] == StartNode.Geometry as Point)
                                    {
                                        Contour.Points.AddRange(edge.GetRange(1, edge.Count - 1));
                                    }
                                    else
                                    {
                                        //done, add current polygon boundery to ContourSet 
                                        //verify that polygone is closed last point in list equals first
                                        if (Contour.Points[count - 1] == Contour.Points[0])
                                        {
                                            ContourSet.Areas.Add(Contour);
                                        }
                                        //else
                                        //{
                                        //    //Debug.WriteLine("Panic: current polygon is not complete, and current egde is not extending it");
                                        //}
                                        //initialize new contour to accumulate next boundery, add current edge to it
                                        Contour = new Area();
                                        Contour.Points.AddRange(edge);
                                    }
                                }
                                else
                                {
                                    //add current edge points to new contour
                                    Contour.Points.AddRange(edge);
                                }
                            }
                            //done, finish up and return
                            ContourSet.Areas.Add(Contour);
                            return ContourSet;
                        }
                    }
            }

            return null;
        }
        public List<SoundingData> ExtractSoundings()
        {
            return enhVectorPtrs.VectorList[0].SoundingList;
        }

        protected string GetAttribute(S57Att attr, string fallback = null)
        {
            if (Attributes != null && Attributes.TryGetValue(attr, out var result))
            {
                return result;
            }

            return fallback;
        }
    }
}
