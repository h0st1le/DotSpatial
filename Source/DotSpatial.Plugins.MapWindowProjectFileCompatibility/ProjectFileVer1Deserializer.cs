using System;
using System.Diagnostics;
using System.Windows.Forms;
using DotSpatial.Controls;
using DotSpatial.Data;
using DotSpatial.Symbology;

namespace DotSpatial.Plugins.MapWindowProjectFileCompatibility
{
    public class ProjectFileVer1Deserializer
    {
        private readonly IMap _map;

        public ProjectFileVer1Deserializer(IMap map)
        {
            _map = map;
        }

        private static Layer GetGridLayer(dynamic layer)
        {
            var symbolizer = new RasterSymbolizer();
            Layer mapLayer = new MapRasterLayer(layer["Path"], symbolizer);
            // DeserializeLayer not implemented.
            return mapLayer;
        }

        private static Layer GetImageLayer(dynamic layer)
        {
            Layer mapLayer = new MapImageLayer(ImageData.Open(layer["Path"]));
            // DeserializeLayer not implemented.
            return mapLayer;
        }

        private static Layer GetLineLayer(dynamic layer)
        {
            MapLineLayer lineLayer = new MapLineLayer(FeatureSet.OpenFile(layer["Path"]));
            LegacyDeserializer.DeserializeLayer(layer, lineLayer);

            return lineLayer;
        }

        private static Layer GetPointLayer(dynamic layer)
        {
            MapPointLayer pointLayer = new MapPointLayer(FeatureSet.OpenFile(layer["Path"]));
            LegacyDeserializer.DeserializeLayer(layer, pointLayer);

            return pointLayer;
        }

        private static Layer GetPolygonLayer(dynamic layer)
        {
            MapPolygonLayer polyLayer = new MapPolygonLayer(FeatureSet.OpenFile(layer["Path"]));
            LegacyDeserializer.DeserializeLayer(layer, polyLayer);

            return polyLayer;
        }

        private static void DeserializeLayers(MapGroup g, dynamic layers)
        {
            foreach (var layer in layers)
            {
                try
                {
                    LegacyLayerType typeOfLayer = (LegacyLayerType)Enum.ToObject(typeof(LegacyLayerType), Convert.ToInt32(layer["Type"]));
                    Layer mapLayer = null;

                    switch (typeOfLayer)
                    {
                        case LegacyLayerType.Grid:
                            mapLayer = GetGridLayer(layer);
                            break;
                        case LegacyLayerType.Image:
                            mapLayer = GetImageLayer(layer);
                            break;
                        case LegacyLayerType.Invalid:
                            throw new ArgumentException("The LayerType is an invalid layer type and cannot be loaded.");
                        case LegacyLayerType.LineShapefile:
                            mapLayer = GetLineLayer(layer);
                            break;
                        case LegacyLayerType.PointShapefile:
                            mapLayer = GetPointLayer(layer);
                            break;
                        case LegacyLayerType.PolygonShapefile:
                            mapLayer = GetPolygonLayer(layer);

                            break;
                        default:
                            throw new NotImplementedException("That LayerType is not supported.");
                    }
                    if (mapLayer != null)
                    {
                        LegacyDeserializer.DeserializeLayerProperties(layer, mapLayer);

                        g.Add(mapLayer);
                    }
                }
                catch (Exception exOpen)
                {
                    //TODO: provide a warning of some sort, possibly ask abort/retry/continue
                    //HACK: we should be catching a more specific exception.
                    Trace.WriteLine(exOpen.Message);
                }
            }
        }

        private void DeserializeGroups(dynamic groups)
        {
            // consider grouping these elements by Position attribute
            // the groups are serialized in the correct order in the files we have examined.
            foreach (var group in groups)
            {
                MapGroup g = new MapGroup();
                g.LegendText = group["Name"];
                g.IsExpanded = Convert.ToBoolean(group["Expanded"]);

                DeserializeLayers(g, group.Layers.Elements());
                _map.MapFrame.Layers.Add(g);
            }
        }

        public void Deserialize(dynamic xmlRoot)
        {
            _map.MapFrame.ProjectionString = xmlRoot["ProjectProjection"];

            if (!Convert.ToBoolean(xmlRoot["ViewBackColor_UseDefault"]))
            {
                var mapControl = _map as Control;
                if (mapControl != null)
                    mapControl.BackColor = LegacyDeserializer.GetColor(xmlRoot["ViewBackColor"]);

                _map.Invalidate();
            }

            _map.MapFrame.ViewExtents.MaxX = Convert.ToDouble(xmlRoot.Extents["xMax"]);
            _map.MapFrame.ViewExtents.MaxY = Convert.ToDouble(xmlRoot.Extents["yMax"]);
            _map.MapFrame.ViewExtents.MinX = Convert.ToDouble(xmlRoot.Extents["xMin"]);
            _map.MapFrame.ViewExtents.MinY = Convert.ToDouble(xmlRoot.Extents["yMin"]);

            DeserializeGroups(xmlRoot.Groups.Elements());   
        }
    }
}