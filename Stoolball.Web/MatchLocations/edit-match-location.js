(function () {
  let map, marker, geocodeUsing;
  const mapContainer = document.getElementById("map");
  const latitude = document.getElementById("MatchLocation_Latitude");
  const longitude = document.getElementById("MatchLocation_Longitude");
  const precisionRadios = document.getElementById("geoprecision");
  /* const street = document.getElementById("MatchLocation_StreetDescription");
  const town = document.getElementById("MatchLocation_Town");
  const county = document.getElementById("MatchLocation_AdministrativeArea");
  const postcode = document.getElementById("MatchLocation_Postcode");
  const apiKey = document
    .querySelector("[data-apikey]")
    .getAttribute("data-apikey");*/

  /**
   * Update geolocation fields when marker is moved
   */
  function markerMoved(event) {
    if (latitude && longitude && precisionRadios && marker) {
      let latLong = marker.getPosition();
      latitude.value = latLong.lat();
      longitude.value = latLong.lng();

      // Select 'exact' precision
      let selected = precisionRadios.querySelector("input:checked");
      if (selected) {
        selected.checked = false;
      }
      let exact = precisionRadios.querySelector("input[value=Exact]");
      if (exact) {
        exact.checked = true;
      }
    }
  }

  function createMap() {
    if (mapContainer && latitude && longitude && precisionRadios) {
      // Get coordinates and title
      let lati = latitude.value;
      let longi = longitude.value;
      let selected = precisionRadios.querySelector("input:checked");
      const precision = selected ? selected.value : null;

      // How accurate?
      let zoomLevel = precision == "Exact" ? 11 : 14;

      // Special case: location not known
      if (lati.length == 0 || longi.length == 0 || precision === null) {
        lati = 51.0465744;
        longi = -0.1235961;
        zoomLevel = 9;
      }

      // Apply standard appearance for maps
      mapContainer.classList.add("google-map");

      // Create the map
      let latlng = new google.maps.LatLng(lati, longi);
      map = new google.maps.Map(mapContainer, {
        zoom: zoomLevel,
        center: latlng,
        mapTypeId: google.maps.MapTypeId.ROADMAP,
      });

      // Create marker
      marker = new google.maps.Marker({
        position: latlng,
        map: map,
        shadow: stoolball.maps.wicketShadow(),
        icon: stoolball.maps.wicketIcon(),
        draggable: true,
        zIndex: 1,
      });

      google.maps.event.addListener(marker, "dragend", markerMoved);
    }
  }

  /**
   * Change event handler for address fields
   */
  function updateMap() {
    while (mapContainer && mapContainer.firstChild)
      mapContainer.removeChild(mapContainer.firstChild);
    createMap();
  }
  /*
  function geocode() {
    if (latitude && longitude && precisionRadios) {
      // Got coordinates already?
      let lati = latitude.value;
      let longi = longitude.value;
      let precision = precisionRadios.querySelector("input:checked");
      if (!precision) {
        precision = precisionRadios.querySelector("input");
        precision.checked = true;
      }

      // If not, get best address available and geocode
      if (lati.length == 0 || longi.length == 0 || precision.value != "Exact") {
        let searchText = "";
        if (postcode && postcode.value.length > 0) {
          searchText = postcode.value;
          geocodeUsing = "Postcode";
        } else {
          if (street && street.value.length > 0) {
            searchText = street.value;
            geocodeUsing = "Street";
          }
          if (town && town.value.length > 0) {
            if (searchText.length > 0) {
              searchText += ", ";
            } else {
              geocodeUsing = "Town";
            }
            searchText += town.value;
          }
          if (county && county.value.length > 0) {
            if (searchText.length > 0) searchText += ", ";
            searchText += county.value;
          }
        }

        if (searchText.length > 0) {
          let url =
            "https://maps.googleapis.com/maps/api/geocode/json?key=" +
            encodeURIComponent(apiKey) +
            "&address=" +
            encodeURIComponent(searchText);

          const request = new XMLHttpRequest();
          request.addEventListener("load", geocodeAndUpdateMap);
          request.open("GET", url);
          request.send();
        }
      }
    }
  }

  function geocodeAndUpdateMap(requestResult) {
    const geocodeResult = JSON.parse(requestResult.srcElement.responseText);
    if (latitude && longitude && precisionRadios && geocodeResult.results[0]) {
      latitude.value = geocodeResult.results[0].geometry.location.lat;
      longitude.value = geocodeResult.results[0].geometry.location.lng;

      // Select the precision used for the geocode
      let selected = precisionRadios.querySelector("input:checked");
      if (selected) {
        selected.checked = false;
      }
      precisionRadios.querySelector(
        "input[value=" + geocodeUsing + "]"
      ).checked = true;

      updateMap();
    }
  }

  function geocodePostcode(event) {
    event.preventDefault();

    // Check there's no postcode already
    if (postcode.value.length > 0) return;

    // Reverse geocode current location using Google
    const geocoder = new google.maps.Geocoder();
    const latlng = new google.maps.LatLng(latitude.value, longitude.value);

    geocoder.geocode(
      {
        latLng: latlng,
      },
      function (results, status) {
        if (status == google.maps.GeocoderStatus.OK) {
          if (results[0] && postcode) {
            let hLen = results.length;
            for (let h = 0; h < hLen; h++) {
              // First geocode result is the most accurate.
              // Look through address components for the postcode
              let iLen = results[h].address_components.length;
              for (let i = 0; i < iLen; i++) {
                let addr = results[h].address_components[i];
                let jLen = addr.types.length;

                for (let j = 0; j < jLen; j++) {
                  if (
                    addr.types[j] == "postal_code" &&
                    addr.long_name.length > 6
                  ) {
                    // Populate the location search box
                    postcode.value = addr.long_name;
                    return;
                  }
                }
              }
            }
          }
        }
      }
    );
  }
*/
  window.addEventListener("DOMContentLoaded", function (event) {
    // Check for consent because Google might track the user
    if (!stoolball.consent.hasTrackingConsent()) {
      return;
    }

    createMap();

    latitude.addEventListener("change", updateMap);
    longitude.addEventListener("change", updateMap);

    if (precisionRadios) {
      precisionRadios.addEventListener("change", updateMap);

      /*    // Create button container
      const buttonControl = document.createElement("div");
      precisionRadios.parentNode.insertBefore(
        buttonControl,
        precisionRadios.nextSibling
      );

      // Create geocode button
      const geocodeButton = document.createElement("button");
      geocodeButton.innerText = "Geocode";
      buttonControl.appendChild(geocodeButton);
      geocodeButton.addEventListener("click", function (e) {
        e.preventDefault();
        geocode();
      });

      // Create postcode button
      const postcodeButton = document.createElement("button");
      postcodeButton.innerText = "Get postcode";
      buttonControl.appendChild(postcodeButton);
      postcodeButton.addEventListener("click", geocodePostcode);*/
    }

    /*street.addEventListener("change", geocode);
    town.addEventListener("change", geocode);
    postcode.addEventListener("change", geocode);*/
  });
})();
