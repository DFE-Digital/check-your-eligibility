import { getandVerifyBearerToken, verifyUnauthorizedWithoutToken } from "../../support/apiHelpers";
import { validLoginRequestBody } from "../../support/requestBodies";

const endpoints = [
  { method: "GET", path: "users/search", body: null },
  { method: "GET", path: "users/00000000-0000-0000-0000-000000000000", body: null },
  { method: "PATCH", path: "users/00000000-0000-0000-0000-000000000000", body: { data: { isEnabled: false } } },
];

describe("User Management endpoint security", () => {
  endpoints.forEach(({ method, path, body }) => {
    it(`${method} ${path} returns 401 without a token`, () => {
      verifyUnauthorizedWithoutToken(method, path, body, "application/json");
    });

    it(`${method} ${path} is rejected for a user without Support_UserManagement`, () => {
      getandVerifyBearerToken("/oauth2/token", validLoginRequestBody).then((token) => {
        cy.apiRequest(method, path, body, token).then((response) => {
          expect(response.status).to.be.oneOf([401, 403]);
        });
      });
    });
  });
});
