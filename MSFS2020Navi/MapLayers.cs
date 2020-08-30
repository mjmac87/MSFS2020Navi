using MapControl;
using System.Collections.Generic;
using System.ComponentModel;
using System.Windows;

namespace ViewModel
{
    public class MapLayers : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private readonly Dictionary<string, UIElement> mapLayers = new Dictionary<string, UIElement>
        {
            {
                "OpenStreetMap",
                new MapTileLayer
                {
                    SourceName = "OpenStreetMap",
                    Description = "© [OpenStreetMap Contributors](http://www.openstreetmap.org/copyright)",
                    TileSource = new TileSource { UriFormat = "https://{c}.tile.openstreetmap.org/{z}/{x}/{y}.png" },
                    MaxZoomLevel = 19
                }
            }
        };

        private string currentMapLayerName = "OpenStreetMap";

        public string CurrentMapLayerName
        {
            get { return currentMapLayerName; }
            set
            {
                currentMapLayerName = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentMapLayerName)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentMapLayer)));
            }
        }

        public UIElement CurrentMapLayer
        {
            get { return mapLayers[currentMapLayerName]; }
        }


        public List<string> MapLayerNames { get; } = new List<string>
        {
            "OpenStreetMap",
        };

        public MapLayers()
        {
            //BingMapsTileLayer.ApiKey = "...";

            // Bing Maps TileLayers with tile URLs retrieved from the Imagery Metadata Service
            // (see http://msdn.microsoft.com/en-us/library/ff701716.aspx).
            // A Bing Maps API Key (see http://msdn.microsoft.com/en-us/library/ff428642.aspx) is required
            // for using these layers and must be assigned to the static BingMapsTileLayer.ApiKey property.

            if (!string.IsNullOrEmpty(BingMapsTileLayer.ApiKey))
            {
                MapLayerNames.Add("Bing Maps Road");
                MapLayerNames.Add("Bing Maps Aerial");
                MapLayerNames.Add("Bing Maps Aerial with Labels");
            }
        }
    }
}