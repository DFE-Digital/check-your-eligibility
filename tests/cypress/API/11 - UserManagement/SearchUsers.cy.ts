import { getAuthResponse } from "../../support/apiHelpers";
import { validSupportPortalLoginRequestBody } from "../../support/requestBodies";

describe("Search Users", () => {
  let token: string;
  let userId: string;

  before(() => {
    getAuthResponse("/oauth2/token", validSupportPortalLoginRequestBody).then((body) => {
      token = body.access_token;
      userId = body.userId;
    });
  });

  it("returns a paginated list of users", () => {
    cy.apiRequest("GET", "users/search?pageNumber=1&pageSize=5", null, token).then((response) => {
      cy.verifyApiResponseCode(response, 200);
      expect(response.body.pageNumber).to.equal(1);
      expect(response.body.pageSize).to.equal(5);
      expect(response.body.totalNumberOfRecords).to.be.a("number");
      expect(response.body.data).to.be.an("array");
      expect(response.body.data.length).to.be.at.most(5);
      response.body.data.forEach((user: any) => expect(user).to.have.property("userID"));
    });
  });

  it("filters by user name or email", () => {
    cy.apiRequest("GET", `users/${userId}`, null, token).then((getResponse) => {
      const email: string = getResponse.body.data.email;
      expect(email, "support user email").to.be.a("string").and.not.be.empty;

      cy.apiRequest("GET", `users/search?search=${encodeURIComponent(email)}`, null, token).then((response) => {
        cy.verifyApiResponseCode(response, 200);
        expect(response.body.data.length).to.be.greaterThan(0);
        response.body.data.forEach((user: any) => {
          const term = email.toLowerCase();
          const matches =
            (user.userName ?? "").toLowerCase().includes(term) ||
            (user.email ?? "").toLowerCase().includes(term);
          expect(matches, `user ${user.userID} matches search`).to.be.true;
        });
      });
    });
  });

  it("returns an empty page when nothing matches", () => {
    cy.apiRequest("GET", `users/search?search=${Cypress._.uniqueId("nomatch-")}-zzzzzz`, null, token).then((response) => {
      cy.verifyApiResponseCode(response, 200);
      expect(response.body.data).to.have.length(0);
      expect(response.body.totalNumberOfRecords).to.equal(0);
    });
  });

  it("supports paging through results", () => {
    cy.apiRequest("GET", "users/search?pageNumber=2&pageSize=1", null, token).then((response) => {
      cy.verifyApiResponseCode(response, 200);
      expect(response.body.pageNumber).to.equal(2);
      expect(response.body.data.length).to.be.at.most(1);
    });
  });

  it("filters by user type", () => {
    cy.apiRequest("GET", "users/search?userType=EligibilityCheckingEngineSupport&pageSize=100", null, token).then((response) => {
      cy.verifyApiResponseCode(response, 200);
      expect(response.body.data.length).to.be.greaterThan(0);
      response.body.data.forEach((user: any) => {
        expect(user.userType).to.equal("EligibilityCheckingEngineSupport");
      });
    });
  });

  it("returns 400 for an invalid user type", () => {
    cy.apiRequest("GET", "users/search?userType=NotAUserType", null, token).then((response) => {
      cy.verifyApiResponseCode(response, 400);
    });
  });
});
