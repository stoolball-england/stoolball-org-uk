import { logToConsole } from "./functions/logging";

describe("Edit friendly match", () => {
  describe("When signed out", () => {
    it("Should return 404 for a match in the past", () => {
      cy.request({
        url: "/matches/maresfield-mixed-education-12jun2014/edit/friendly",
        failOnStatusCode: false,
      }).should((response) => {
        expect(response.status).to.eq(404);
      });
    });
  });
});
