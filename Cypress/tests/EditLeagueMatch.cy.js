import { logToConsole } from "./functions/logging";

describe("Edit league match", () => {
  describe("When signed out", () => {
    it("Should return 404 for a match in the past", () => {
      cy.request({
        url: "/matches/maresfield-mixed-east-hoathly-and-halland-27may2014/edit/league",
        failOnStatusCode: false,
      }).should((response) => {
        expect(response.status).to.eq(404);
      });
    });
  });
});
