﻿using System.Collections.Generic;

namespace Stoolball.Routing
{
    /// <summary>
    /// Parses, validates and tidies up route values into expected formats
    /// </summary>
    public interface IRouteNormaliser
    {
        /// <summary>
        /// From a given route, removes any extraneous / characters and returns only the portion identifying an entity, ie prefix/entity-route
        /// </summary>
        /// <param name="route">prefix/entity-route</param>
        /// <param name="expectedPrefixAndRegex">Dictionary where the key is the expected prefix and the value is the valid entity route regex</param>
        /// <returns>prefix/entity-route</returns>
        string NormaliseRouteToEntity(string route, Dictionary<string, string?> expectedPrefixAndRegex);

        /// <summary>
        /// From a given route, removes any extraneous / characters and returns only the portion identifying an entity, ie prefix/entity-route
        /// </summary>
        /// <param name="route">prefix/entity-route</param>
        /// <param name="expectedPrefix">prefix</param>
        /// <param name="entityRouteRegex">(valid|alsoValid)</param>
        /// <returns>prefix/entity-route</returns>
        string NormaliseRouteToEntity(string route, string expectedPrefix, string? entityRouteRegex);

        /// <summary>
        /// From a given route, removes any extraneous / characters and returns only the portion identifying an entity, ie prefix/entity-route
        /// </summary>
        /// <param name="route">prefix/entity-route</param>
        /// <param name="expectedPrefix">prefix</param>
        /// <returns>prefix/entity-route</returns>
        string NormaliseRouteToEntity(string route, string expectedPrefix);
    }
}