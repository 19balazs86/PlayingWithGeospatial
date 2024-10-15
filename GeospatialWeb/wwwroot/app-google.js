const mapWrapper = (function () // IIFE
{
    var map, memoryMarker, poiInfoWindow;

    var directionsService, directionsRenderer;

    var poiMarkers = [];

    var timer;

    async function initializeMap()
    {
        const { Map } = await google.maps.importLibrary("maps");

        const { DirectionsService, DirectionsRenderer } = await google.maps.importLibrary("routes");

        map = new Map(document.getElementById("map"), {
            center: { lat: 48.8584, lng: 2.2945 }, // France
            zoom: 15,
            mapId: "aff8d5b3206f918f"
        });

        map.addListener('idle', onMoveEnd_Map);
        map.addListener('click', onClick_Map);

        memoryMarker = await createMemorymarker();

        directionsService  = new DirectionsService();
        directionsRenderer = new DirectionsRenderer();
        directionsRenderer.setMap(map);

        // Create a layer for markers
        //const { Data } = await google.maps.importLibrary("maps");
        //poiMarkersLayer = new Data();
        //poiMarkersLayer.addGeoJson({"type": "FeatureCollection", "features": []});
        //poiMarkersLayer.setMap(map);
    }

    function onClick_Map(event)
    {
        memoryMarker.position = event.latLng;

        if (poiInfoWindow && poiInfoWindow.isOpen)
        {
            poiInfoWindow.close();
        }
    }

    function onMoveEnd_Map()
    {
        console.log(`Center: ${map.getCenter()} | Zoom: ${map.getZoom()}`)

        if (document.getElementById('zoomRefreshCheckbox').checked)
        {
            clearTimeout(timer);

            timer = setTimeout(() => searchPoisWithin(), 1000);
        }
    }

    async function createMemorymarker()
    {
        const { InfoWindow } = await google.maps.importLibrary("maps");
        const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");

        const center = map.getCenter();

        // Reference: https://developers.google.com/maps/documentation/javascript/reference/advanced-markers
        const marker = new AdvancedMarkerElement({
            position: center,
            map: map,
            title: "Memory"
        });

        const infoWindow = new InfoWindow({ content: "<b>Hello!</b> Place me anywhere and navigate back here" });

        marker.addListener("click", target => {
            infoWindow.open(map, marker);

            console.log(target.latLng.toString());
        });

        return marker;
    }

    function findMe()
    {
        const location = memoryMarker.position;

        // if (!map.getBounds().contains(location)) map.setCenter(location);

        map.setCenter(location);
    }

    async function searchPoisDistance()
    {
        const center   = map.getCenter();
        const distance = await distanceWestEast();

        let apiUrl = `/api/pois/distance?lat=${center.lat()}&lng=${center.lng()}&distance=${distance / 2}`;

        await fetchPois(apiUrl);
    }

    async function searchPoisWithin()
    {
        // Center
        const center = map.getCenter();
        const centerLng = center.lng();
        const centerLat = center.lat();

        // Bounds
        const bounds = map.getBounds();
        const ne = bounds.getNorthEast();
        const sw = bounds.getSouthWest();
        const nw = new google.maps.LatLng(ne.lat(), sw.lng());
        const se = new google.maps.LatLng(sw.lat(), ne.lng());

        // Coordinates
        const nwLng = nw.lng();
        const nwLat = nw.lat();
        const neLng = ne.lng();
        const neLat = ne.lat();
        const swLng = sw.lng();
        const swLat = sw.lat();
        const seLng = se.lng();
        const seLat = se.lat();

        // Api URL + Query string
        let apiUrl = `/api/pois/within?centerLng=${centerLng}&centerLat=${centerLat}&nwLng=${nwLng}&nwLat=${nwLat}&swLng=${swLng}&swLat=${swLat}&seLng=${seLng}&seLat=${seLat}&neLng=${neLng}&neLat=${neLat}`;

        await fetchPois(apiUrl);
    }

    async function fetchPois(apiUrl)
    {
        try
        {
            const response = await fetch(apiUrl);

            if (!response.ok)
            {
                throw new Error(`HTTP error! Status: ${response.status}`);
            }

            const poiArray = await response.json();

            await refreshPoiMarkersLayer(poiArray);
        }
        catch (error)
        {
            console.error('Error fetching data:', error);
        }
    }

    async function refreshPoiMarkersLayer(poiArray)
    {
        const { InfoWindow } = await google.maps.importLibrary("maps");
        const { AdvancedMarkerElement } = await google.maps.importLibrary("marker");

        removeAllMarkers();

        for (const poi of poiArray)
        {
            const marker = new AdvancedMarkerElement({
                position: { lat: poi.lat, lng: poi.lng },
                map: map,
                title: poi.name
            });

            poiMarkers.push(marker);

            marker.addListener("click", () =>
            {
                if (poiInfoWindow)
                {
                    poiInfoWindow.close();
                }

                poiInfoWindow = new InfoWindow({ content: `${poi.category}: <b>${poi.name}</b>` });

                poiInfoWindow.open(map, marker);
            });
        }
    }

    function calculateAndDisplayRoute()
    {
        if (poiInfoWindow && poiInfoWindow.isOpen)
        {
            const fromPosition = memoryMarker.position;
            const toPosition   = poiInfoWindow.position;

            const routeInfo = {
                origin: fromPosition,
                destination: toPosition,
                travelMode: google.maps.TravelMode.DRIVING,
            };

            directionsService.route(routeInfo, (response, status) =>
            {
                if (status === 'OK')
                {
                    directionsRenderer.setDirections(response);
                }
                else
                {
                    window.alert('Directions request failed due to ' + status);
                }
            });
        }
    }

    function removeAllMarkers()
    {
        for (let i = 0; i < poiMarkers.length; i++)
        {
            poiMarkers[i].setMap(null);
        }

        poiMarkers = [];

        if (poiInfoWindow)
        {
            poiInfoWindow.close();
        }
    }

    async function distanceWestEast()
    {
        const geometry = await google.maps.importLibrary('geometry');

        // Get the bounds of the map
        const bounds    = map.getBounds();
        const northEast = bounds.getNorthEast();
        const southWest = bounds.getSouthWest();

        // Calculate the distance to the left and right boundaries
        const left  = new google.maps.LatLng(southWest.lat(), southWest.lng());
        const right = new google.maps.LatLng(southWest.lat(), northEast.lng());

        return geometry.spherical.computeDistanceBetween(left, right);
    }

    // Exposed functions
    return {
        initializeMap,
        findMe,
        searchPoisDistance,
        searchPoisWithin,
        calculateRoute: calculateAndDisplayRoute
    };
})();