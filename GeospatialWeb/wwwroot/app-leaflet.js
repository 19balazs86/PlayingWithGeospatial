class MapWrapper
{
    constructor()
    {
        this.map = L.map('map').setView([48.8584, 2.2945], 15); // France

        this.map.on('moveend', this.onMoveEnd_Map.bind(this));
        this.map.on('click',   this.onClick_Map.bind(this));

        this.memoryMarker    = this.createMemorymarker();
        this.poiMarkersLayer = this.initializeLayers();
        this.cachePoiMarkers = [];
        this.timer           = null;
    }

    onClick_Map(event)
    {
        this.memoryMarker.setLatLng(event.latlng);
    }

    onMoveEnd_Map()
    {
        console.log(`Center: ${this.map.getCenter()} | Zoom: ${this.map.getZoom()}`);

        if (document.getElementById('zoomRefreshCheckbox').checked)
        {
            clearTimeout(this.timer);

            this.timer = setTimeout(() => this.searchPoisWithin(), 1000);
        }
    }

    initializeLayers()
    {
        // Example: https://leafletjs.com/examples/layers-control

        const markersLayer = L.layerGroup().addTo(this.map); // Turn on by default with addTo(map)

        const openStreetMap = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(this.map); // Make it dafeult with addTo(map)

        const baseLayers = {
            "OpenStreetMap": openStreetMap
        };

        const overlays = {
            "Poi": markersLayer
        };

        L.control.layers(baseLayers, overlays).addTo(this.map);

        return markersLayer;
    }

    createMemorymarker()
    {
        const divIconMarker = L.divIcon({ className: 'custom-marker' });
        const center = this.map.getCenter();

        const marker = L.marker([center.lat, center.lng], { icon: divIconMarker }).addTo(this.map);

        marker.bindPopup("<b>Hello!</b> Place me anywhere and navigate back here");

        marker.on('click', event => console.log(`${event.target.getLatLng()}`));

        return marker;
    }

    findMe()
    {
        const location = this.memoryMarker.getLatLng();

        // if (!this.map.getBounds().contains(location)) this.map.setView(location);

        this.map.setView(location);
    }

    async searchPoisDistance()
    {
        const center = this.map.getCenter();
        const bounds = this.map.getBounds();

        const distance = (bounds.getNorthWest().distanceTo(bounds.getSouthEast())) / 2;

        const apiUrl = `/api/pois/distance?lat=${center.lat}&lng=${center.lng}&distance=${distance}`;

        await this.fetchPois(apiUrl);
    }

    async searchPoisWithin()
    {
        // Center
        const center    = this.map.getCenter();
        const centerLng = center.lng;
        const centerLat = center.lat;

        // Bounds
        const bounds = this.map.getBounds();
        const nw = bounds.getNorthWest();
        const sw = bounds.getSouthWest();
        const se = bounds.getSouthEast();
        const ne = bounds.getNorthEast();

        // Api URL + Query string
        const apiUrl = `/api/pois/within?centerLng=${centerLng}&centerLat=${centerLat}&nwLng=${nw.lng}&nwLat=${nw.lat}&swLng=${sw.lng}&swLat=${sw.lat}&seLng=${se.lng}&seLat=${se.lat}&neLng=${ne.lng}&neLat=${ne.lat}`;

        await this.fetchPois(apiUrl);
    }

    async fetchPois(apiUrl)
    {
        try
        {
            const response = await fetch(apiUrl);

            if (!response.ok)
            {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const poiArray = await response.json();

            this.refreshPoiMarkersLayer(poiArray);
        }
        catch (error)
        {
            console.error('Error fetching data:', error);
        }
    }

    refreshPoiMarkersLayer(poiArray)
    {
        // console.log(poiMarkersLayer.getLayers().length)

        this.poiMarkersLayer.eachLayer(marker =>
        {
            if (marker instanceof L.Marker)
            {
                this.cachePoiMarkers.push(marker);
            }
        });

        this.poiMarkersLayer.clearLayers();

        poiArray.forEach(poi =>
        {
            const marker = this.cachePoiMarkers.pop() || L.marker([0, 0]);

            marker.setLatLng([poi.lat, poi.lng])
                  .bindPopup(`${poi.category}: <b>${poi.name}</b>`);

            marker.PoiId = poi.id;

            this.poiMarkersLayer.addLayer(marker);
        });
    }
}

// Usage
const mapWrapper = new MapWrapper();

//document.querySelector('.button.find-me').onclick = () => mapWrapper.findMe();
//document.querySelector('.button.search-distance').onclick = () => mapWrapper.searchPoisDistance();
//document.querySelector('.button.search-within').onclick = () => mapWrapper.searchPoisWithin();