import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody, validApplicationRequestBody, validApplicationSupportRequestBody, validUserRequestBody } from '../../support/requestBodies';

describe('Verify POST application responses', () => {
    const validBaseApplicationRequest = validApplicationRequestBody();

    it('Verify 201 Created response is returned with valid application', () => {
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            const requestBody = validApplicationSupportRequestBody();
            cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', requestBody, token).then((response) => {
                cy.verifyApiResponseCode(response, 202);
                cy.apiRequest('POST', '/Users', validUserRequestBody(), token).then((response) => {
                    validBaseApplicationRequest.Data.UserId = response.Data;
                    //Make post request for eligibility check
                    cy.apiRequest('POST', 'Application', validBaseApplicationRequest, token).then((response) => {
                        // Assert the status and statusText
                        cy.verifyApiResponseCode(response, 201);

                        // Assert the response body data
                        cy.verifyPostApplicationResponse(response, validBaseApplicationRequest);
                    });
                });
            });
        });
    });

    it('Verify 400 Bad request response is returned with invalid application (no parent last name)', () => {
        const invalidApplicationNoLastNameRequestBody = {
            ...validBaseApplicationRequest,
            data: {
                ...validBaseApplicationRequest.Data,
                parentLastName: ''
            }
        };
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            cy.apiRequest('POST', 'Application', invalidApplicationNoLastNameRequestBody, token).then((response) => {
                // Assert the status and statusText
                cy.verifyApiResponseCode(response, 400);
                expect(response.body).to.have.property('data', 'LastName is required');
            });
        });
    });
});

describe('Verify invalid application request responses', () => {

    const validBaseApplicationRequest = validApplicationRequestBody();

    it('Verify 400 Bad request response is returned with invalid child last name', () => {
        const invalidApplicationNoChildLastNameRequestBody = {
            ...validBaseApplicationRequest,
            data: {
                ...validBaseApplicationRequest.Data,
                childLastName: ''
            }
        };
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            cy.apiRequest('POST', 'Application', invalidApplicationNoChildLastNameRequestBody, token).then((response) => {
                // Assert the status and statusText
                cy.verifyApiResponseCode(response, 400);
                expect(response.body).to.have.property('data', 'Child LastName is required');
            });
        });
    });

    it('Verify 400 Bad request response is returned with invalid child DOB', () => {
        const invalidApplicationInvalidChildDOBRequestBody = {
            ...validBaseApplicationRequest,
            data: {
                ...validBaseApplicationRequest.Data,
                childDateOfBirth: '01'
            }
        };

        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            cy.apiRequest('POST', 'Application', invalidApplicationInvalidChildDOBRequestBody, token).then((response) => {
                // Assert the status and statusText
                cy.verifyApiResponseCode(response, 400);
                expect(response.body).to.have.property('data', "Child Date of birth is required:- (yyyy-mm-dd)");
            });
        });
    });

    it('Verify 401 response is returned when bearer token is not provided', () => {
        cy.apiRequest('POST', 'Application', validLoginRequestBody,).then((response) => {
          cy.verifyApiResponseCode(response, 401)
        });
      });
});
