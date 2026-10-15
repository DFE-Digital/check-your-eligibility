import { getAuthResponse } from "../../support/apiHelpers";
import { validSupportPortalLoginRequestBody } from "../../support/requestBodies";

describe("Patch User", () => {
  let token: string;
  let testUserId: string;

  before(() => {
    getAuthResponse("/oauth2/token", validSupportPortalLoginRequestBody).then((body) => {
      token = body.access_token;

      // Throwaway user, so the Cypress support user is never disabled
      const unique = Cypress._.uniqueId(`cypress-patch-${Date.now()}-`);
      const createBody = {
        data: { email: `${unique}@test.com`, reference: unique },
        metaData: { userName: unique, source: "FreeSchoolMealsParent", organisationType: "none" },
      };
      cy.apiRequest("POST", "user", createBody, token).then((response) => {
        cy.verifyApiResponseCode(response, 201);
        testUserId = response.body.data;
      });
    });
  });

  it("disables and re-enables a user", () => {
    cy.apiRequest("PATCH", `users/${testUserId}`, { data: { isEnabled: false } }, token).then((response) => {
      cy.verifyApiResponseCode(response, 204);
    });
    cy.apiRequest("GET", `users/${testUserId}`, null, token).then((response) => {
      expect(response.body.data.isEnabled).to.equal(false);
    });

    cy.apiRequest("PATCH", `users/${testUserId}`, { data: { isEnabled: true } }, token).then((response) => {
      cy.verifyApiResponseCode(response, 204);
    });
    cy.apiRequest("GET", `users/${testUserId}`, null, token).then((response) => {
      expect(response.body.data.isEnabled).to.equal(true);
    });
  });

  it("returns 404 for a user that does not exist", () => {
    cy.apiRequest("PATCH", "users/00000000-0000-0000-0000-000000000000", { data: { isEnabled: false } }, token).then(
      (response) => {
        cy.verifyApiResponseCode(response, 404);
      },
    );
  });

  it("returns 400 when isEnabled is missing", () => {
    cy.apiRequest("PATCH", `users/${testUserId}`, { data: {} }, token).then((response) => {
      cy.verifyApiResponseCode(response, 400);
    });
  });
});
