const mapWrapper = (function () // IIFE
{
    var map = L.map('map').setView([48.8584, 2.2945], 15); // France

    map.on('moveend', onMoveEnd_Map);
    map.on('click',   onClick_Map);

    var memoryMarker = createMemorymarker();

    var poiMarkersLayer = initializeLayers();
    var cachePoiMarkers = [];

    var timer;

    function onClick_Map(event)
    {
        memoryMarker.setLatLng(event.latlng);
    }

    function onMoveEnd_Map()
    {
        console.log(`Center: ${map.getCenter()} | Zoom: ${map.getZoom()}`)

        if (document.getElementById('zoomRefreshCheckbox').checked)
        {
            clearTimeout(timer);

            timer = setTimeout(() => searchPoi(), 1000);
        }
    }

    function initializeLayers()
    {
        // Example: https://leafletjs.com/examples/layers-control

        const markersLayer = L.layerGroup().addTo(map); // Turn on by default with addTo(map)

        const openStreetMap = L.tileLayer('https://tile.openstreetmap.org/{z}/{x}/{y}.png', {
            maxZoom: 19,
            attribution: '&copy; <a href="http://www.openstreetmap.org/copyright">OpenStreetMap</a>'
        }).addTo(map); // Make it dafeult with addTo(map)

        const baseLayers = {
            "OpenStreetMap": openStreetMap
        };

        const overlays = {
            "Poi": markersLayer
        };

        L.control.layers(baseLayers, overlays).addTo(map);

        return markersLayer;
    }

    function createMemorymarker()
    {
        const divIconMarker = L.divIcon({ className: 'custom-marker' });

        const center = map.getCenter();

        const marker = L.marker([center.lat, center.lng], { icon: divIconMarker }).addTo(map);

        marker.bindPopup("<b>Hello!</b> Place me anywhere and navigate back here");

        marker.on('click', event => console.log(`${event.target.getLatLng()}`));

        return marker;
    }

    function findMe()
    {
        const location = memoryMarker.getLatLng();

        // if (!map.getBounds().contains(location)) map.setView(location);

        map.setView(location);
    }

    async function searchPoi()
    {
        const center = map.getCenter();
        const bounds = map.getBounds();

        const distance = (bounds.getNorthWest().distanceTo(bounds.getSouthEast())) / 2;

        let apiUrl = `/api/poi?lat=${center.lat}&lng=${center.lng}&distance=${distance}`;

        try
        {
            const response = await fetch(apiUrl);

            if (!response.ok)
            {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const poiArray = await response.json();

            refreshPoiMarkersLayer(poiArray);
        } catch (error) {
            console.error('Error fetching data:', error);
        }
    }

    function refreshPoiMarkersLayer(poiArray)
    {
        // console.log(poiMarkersLayer.getLayers().length)

        poiMarkersLayer.eachLayer(marker =>
        {
            if (marker instanceof L.Marker)
            {
                cachePoiMarkers.push(marker);
            }
        });

        poiMarkersLayer.clearLayers();

        for (const poi of poiArray)
        {
            const marker = cachePoiMarkers.pop() || L.marker([0, 0]);

            marker.setLatLng([poi.lat, poi.lng])
                .bindPopup(`${poi.category}: <b>${poi.name}</b>`);

            marker.PoiId = poi.id;

            poiMarkersLayer.addLayer(marker);
        }
    }

    // Exposed functions
    return {
        onClick_FindMe: findMe,
        onClick_SearchPoi: searchPoi
    };
})();