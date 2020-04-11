if (typeof stoolball === "undefined") {
  stoolball = {};
}
stoolball.maps = {
  /**
   * Creates a icon for a Google maps marker
   */
  wicketIcon: function () {
    return new google.maps.MarkerImage(
      "/images/maps/map-marker.gif",
      new google.maps.Size(21, 57),
      new google.maps.Point(0, 0), // origin
      new google.maps.Point(10, 53)
    ); // anchor
  },

  /**
   * Creates a shadow for a Google maps marker
   */
  wicketShadow: function () {
    return new google.maps.MarkerImage(
      "/images/maps/map-marker-shadow.png",
      new google.maps.Size(45, 57),
      new google.maps.Point(0, 0),
      new google.maps.Point(10, 53)
    );
  },
};
