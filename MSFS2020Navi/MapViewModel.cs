using MapControl;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ViewModel
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        protected void RaisePropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class PointItem : ViewModelBase
    {
        private string name;
        public string Name
        {
            get { return name; }
            set
            {
                name = value;
                RaisePropertyChanged(nameof(Name));
            }
        }

        private Location location;
        public Location Location
        {
            get { return location; }
            set
            {
                location = value;
                RaisePropertyChanged(nameof(Location));
            }
        }
    }

    public class Polyline
    {
        public LocationCollection Locations { get; set; }
    }

    public class MapViewModel : ViewModelBase
    {
        private Location mapCenter = new Location(52.330155, -0.182885);
        public Location MapCenter
        {
            get { return mapCenter; }
            set
            {
                mapCenter = value;
                RaisePropertyChanged(nameof(MapCenter));
            }
        }

        public ObservableCollection<PointItem> Points { get; } = new ObservableCollection<PointItem>();
        public ObservableCollection<PointItem> Pushpins { get; } = new ObservableCollection<PointItem>();
        public ObservableCollection<Polyline> Polylines { get; } = new ObservableCollection<Polyline>();

        public MapLayers MapLayers { get; } = new MapLayers();

        public MapViewModel()
        {
           
        }
    }
}