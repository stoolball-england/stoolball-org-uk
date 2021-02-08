window.addEventListener("DOMContentLoaded", function (event) {
  // Check for consent because Google might track the user
  if (!stoolball.consent.hasMapsConsent()) {
    return;
  }

  // Get map placeholder
  const mapContainer = document.getElementById("location-map");
  if (mapContainer) {
    // How accurate?
    let zoomLevel = 14;
    if (mapContainer.getAttribute("data-precision") === "town") {
      zoomLevel = 11;
    }

    // Apply standard appearance for maps
    const mapControl = mapContainer.getElementsByTagName("p")[0];
    mapControl.tagName = "div";
    mapControl.classList.add("google-map");
    mapControl.classList.add("print__no-urls");

    // Create the map
    const latlng = new google.maps.LatLng(
      mapContainer.getAttribute("data-latitude"),
      mapContainer.getAttribute("data-longitude")
    );
    const map = new google.maps.Map(mapControl, {
      zoom: zoomLevel,
      center: latlng,
      mapTypeId: google.maps.MapTypeId.ROADMAP,
    });

    new google.maps.Marker({
      position: latlng,
      map: map,
      shadow: stoolball.maps.wicketShadow(),
      icon: stoolball.maps.wicketIcon(),
      title: document.getElementsByTagName("h1")[0].innerText,
      zIndex: 1,
    });
  }
});
