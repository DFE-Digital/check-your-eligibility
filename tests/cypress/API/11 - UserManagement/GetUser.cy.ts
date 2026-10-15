import { getAuthResponse } from "../../support/apiHelpers";
import { validSupportPortalLoginRequestBody } from "../../support/requestBodies";

describe("Get User", () => {
  let token: string;
  let userId: string;

  before(() => {
    getAuthResponse("/oauth2/token", validSupportPortalLoginRequestBody).then((body) => {
      token = body.access_token;
      userId = body.userId;
    });
  });

  it("returns the user and their roles", () => {
    cy.apiRequest("GET", `users/${userId}`, null, token).then((response) => {
      cy.verifyApiResponseCode(response, 200);
      expect(response.body.data.userID).to.equal(userId);
      expect(response.body.roles).to.be.an("array");
      // Enums serialise as strings
      const roleNames = response.body.roles.map((r: any) => r.roleName);
      expect(roleNames).to.include("Support_UserManagement");
    });
  });

  it("returns 404 for a user that does not exist", () => {
    cy.apiRequest("GET", "users/00000000-0000-0000-0000-000000000000", null, token).then((response) => {
      cy.verifyApiResponseCode(response, 404);
    });
  });
});
