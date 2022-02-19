(function () {
  window.addEventListener("DOMContentLoaded", function () {
    // Check for consent because Google might track the user
    if (!stoolball.consent.hasMapsConsent()) {
      return;
    }

    if (!fetch) {
      return;
    }

    var mapControl = document.getElementById("map");

    // Convert data attributes into a querystring for the API
    const query = [["season", mapControl.getAttribute("data-seasonid")]]
      .map(function (x) {
        return x.join("=");
      })
      .join("&");

    // Adjust the map to the markers or leave at default?
    const fixMap = mapControl.getAttribute("data-adjust") === "true";

    // Make the placeholder big enough for a map
    mapControl.setAttribute("class", "google-map");
    const myOptions = {
      mapTypeId: google.maps.MapTypeId.ROADMAP,
      center: new google.maps.LatLng(51.8157917, -0.9166621), // Aylesbury - works for showing a map of England by default
      zoom: 6,
    };
    const map = new google.maps.Map(mapControl, myOptions);
    const groundMarkers = [],
      teamMarkers = [];
    const info = new google.maps.InfoWindow();
    let clusterer;
    let previousZoom = map.getZoom();

    // JavaScript will create two sets of markers, one for each ground, and one for each team.
    // This is so that, when clustered, we can display the number of teams by creating a marker for each(even though they're actually duplicates).
    // You can't get an infoWindow for a cluster though, so once we're zoomed in far enough switch to using the ground markers, which are unique.
    fetch("/api/locations/map?" + query)
      .then(function (response) {
        return response.json();
      })
      .then(function (data) {
        createGroundMarkers(data);
        createTeamMarkers(data);
        if (!fixMap) {
          setCentre(data);
        }
        plotMarkers(teamMarkers);
      });

    function createGroundMarkers(data) {
      for (let i = 0; i < data.length; i++) {
        if (
          data[i].latitude === null ||
          data[i].latitude === undefined ||
          data[i].longitude === null ||
          data[i].longitude === undefined
        ) {
          continue;
        }

        const content = infoWindowContent(data[i]);
        groundMarkers.push(markerScript(data[i], content));
      }
    }

    function createTeamMarkers(data) {
      for (let i = 0; i < data.length; i++) {
        if (
          data[i].latitude === null ||
          data[i].latitude === undefined ||
          data[i].longitude === null ||
          data[i].longitude === undefined
        ) {
          continue;
        }

        const content = infoWindowContent(data[i]);
        for (let j = 0; j < data[i].teams.length; j++) {
          if (data[i].teams[j].active) {
            teamMarkers.push(markerScript(data[i], content));
          }
        }
      }
    }

    function infoWindowContent(location) {
      const locationInfo = document.createElement("div");
      locationInfo.setAttribute("class", "google-map-info");
      locationInfo.setAttribute("role", "alert");

      const h2 = document.createElement("h2");
      h2.innerText = location.name;

      const p = document.createElement("p");
      p.appendChild(document.createTextNode("Home to: "));

      const links = [];
      for (let i = 0; i < location.teams.length; i++) {
        if (location.teams[i].active) {
          const a = document.createElement("a");
          a.setAttribute("href", encodeURI(location.teams[i].route));
          a.innerText = location.teams[i].name;
          links.push(a);
        }
      }

      links.map(function (a, index) {
        if (index > 0 && index !== links.length - 1) {
          p.appendChild(document.createTextNode(", "));
        } else if (index > 0 && index === links.length - 1) {
          p.appendChild(document.createTextNode(" and "));
        }
        p.appendChild(a);
      });

      locationInfo.appendChild(h2);
      locationInfo.appendChild(p);
      return locationInfo;
    }

    function markerScript(location, infoWindowContent) {
      //  And marker and click event to trigger info window.Wrap info window in function to isolate marker, otherwise the last marker
      //  is always used for the position of the info window.
      const locationMarker = new google.maps.Marker({
        position: new google.maps.LatLng(location.latitude, location.longitude),
        shadow: stoolball.maps.wicketShadow(),
        icon: stoolball.maps.wicketIcon(),
        title: location.name,
      });

      (function (marker, content) {
        google.maps.event.addListener(marker, "click", function () {
          info.setContent(content.outerHTML);
          info.open(map, marker);
        });
      })(locationMarker, infoWindowContent);
      return locationMarker;
    }

    function removeMarkers(removeMarkers) {
      for (let i = 0; i < removeMarkers.length; i++) {
        removeMarkers[i].setMap(null);
      }
    }

    function plotMarkers(addMarkers) {
      if (clusterer) clusterer.clearMarkers();
      clusterer = new MarkerClusterer(map, addMarkers, {
        gridSize: 30,
        styles: [
          {
            url: "/images/maps/map-markers.gif",
            height: 67,
            width: 31,
            textColor: "#ffffff",
            textSize: 10,
            anchorText: [28, 0],
          },
        ],
        ariaLabelFn: function (teamsInCluster) {
          return teamsInCluster + " teams";
        },
      });
    }

    function setCentre(data) {
      const minLatitude = Math.min.apply(
        null,
        data
          .filter(function (x) {
            return x.latitude !== null && x.latitude !== undefined;
          })
          .map(function (x) {
            return x.latitude;
          })
      );
      const maxLatitude = Math.max.apply(
        null,
        data
          .filter(function (x) {
            return x.latitude !== null && x.latitude !== undefined;
          })
          .map(function (x) {
            return x.latitude;
          })
      );
      const minLongitude = Math.min.apply(
        null,
        data
          .filter(function (x) {
            return x.longitude !== null && x.longitude !== undefined;
          })
          .map(function (x) {
            return x.longitude;
          })
      );
      const maxLongitude = Math.max.apply(
        null,
        data
          .filter(function (x) {
            return x.longitude !== null && x.longitude !== undefined;
          })
          .map(function (x) {
            return x.longitude;
          })
      );
      const latitudeSpan = maxLatitude - minLatitude;
      const longitudeSpan = maxLongitude - minLongitude;
      const midLatitude = minLatitude + latitudeSpan / 2;
      const midLongitude = minLongitude + longitudeSpan / 2;
      if (!isNaN(midLatitude) && !isNaN(midLongitude)) {
        map.setCenter(new google.maps.LatLng(midLatitude, midLongitude));

        if (latitudeSpan < 0.25 && longitudeSpan < 0.25) {
          map.setZoom(11);
        } else if (latitudeSpan < 0.5 && longitudeSpan < 0.5) {
          map.setZoom(10);
        } else {
          map.setZoom(6);
        }
      }
    }

    function zoomChanged() {
      var currentZoom = map.getZoom();
      if (previousZoom < 14 && currentZoom >= 14) {
        removeMarkers(teamMarkers);
        plotMarkers(groundMarkers);
      } else if (previousZoom >= 14 && currentZoom < 14) {
        removeMarkers(groundMarkers);
        plotMarkers(teamMarkers);
      }
      previousZoom = currentZoom;
    }

    google.maps.event.addListener(map, "zoom_changed", zoomChanged);
  });
})();
