window.addEventListener("DOMContentLoaded", function (event) {
  // Get map placeholder
  const mapContainer = document.getElementById("location-map");
  if (mapContainer) {
    // How accurate?
    let exactMessage = "";
    let zoomLevel = 14;
    switch (mapContainer.getAttribute("data-precision")) {
      case "postcode":
        exactMessage =
          "Note: This map shows the nearest postcode. The match location should be nearby.";
        break;
      case "street":
        exactMessage =
          "Note: This map shows the nearest road. The match location should be nearby.";
        break;
      case "town":
        exactMessage =
          "Note: This map shows the town or village. Contact the home team to find the match location.";
        zoomLevel = 11;
        break;
    }

    // Make the placeholder big enough for a map
    const mapControl = mapContainer.getElementsByTagName("p")[0];
    mapControl.tagName = "div";
    mapControl.style.width = "100%";
    mapControl.style.height = "500px";

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

    // Add a heading to introduce the map
    // Use DOMParser to safely HTMLDecode the attribute value
    const mapHeading = document.createElement("h2");
    let title = new DOMParser().parseFromString(
      mapContainer.getAttribute("data-title"),
      "text/html"
    );
    mapHeading.appendChild(
      document.createTextNode("Map of " + title.documentElement.textContent)
    );
    mapContainer.parentNode.insertBefore(mapHeading, mapContainer);

    if (exactMessage) {
      const exactInfo = document.createElement("p");
      exactInfo.appendChild(document.createTextNode(exactMessage));
      mapContainer.parentNode.insertBefore(exactInfo, mapContainer);
    }
  }
});
