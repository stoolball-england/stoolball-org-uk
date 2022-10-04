import { logToConsole } from "./functions/logging";

describe("Edit knockout match", () => {
  describe("When signed out", () => {
    it("Should return 404 for a match in the past", () => {
      cy.request({
        url: "/matches/east-hoathly-and-halland-nutley-mixed-3jun2014/edit/knockout",
        failOnStatusCode: false,
      }).should((response) => {
        expect(response.status).to.eq(404);
      });
    });
  });
});
