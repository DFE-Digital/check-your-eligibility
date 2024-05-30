// cypress/support/apiHelpers.ts

export const verifyUnauthorizedWithoutToken = (method: string, endpoint: string, requestBody: any) => {
    cy.apiRequest(method, endpoint, requestBody).then((response) => {
        cy.verifyApiResponseCode(response, 401)
    });
}


export const getandVerifyBearerToken = (url: string, requestBody: any): Cypress.Chainable<string> => {
    return cy.apiRequest('POST', url, requestBody).then((response) => {
        expect(response.status).to.equal(200);
        expect(response.body).to.have.property('token');
        return response.body.token;
    });
};