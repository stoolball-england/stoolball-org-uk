// Ensure all writes to possible XSS attack vectors are protected by a trusted type, where supported.
// A default policy is the only way to implement this since jQuery and Bootstrap both use innerHTML.
// https://web.dev/trusted-types/
if (window.trustedTypes && trustedTypes.createPolicy) {
  trustedTypes.createPolicy("default", {
    createHTML: function (string, sink) {
      return DOMPurify.sanitize(string, { RETURN_TRUSTED_TYPE: true });
    },
    createScriptURL: function (url) {
      const allowedOrigins = [
        document.location.origin,
        "https://www.googletagmanager.com",
      ];
      const parsed = new URL(url, document.baseURI);
      if (allowedOrigins.indexOf(parsed.origin) > -1) {
        return url;
      }
      throw new TypeError("invalid URL");
    },
  });
}
