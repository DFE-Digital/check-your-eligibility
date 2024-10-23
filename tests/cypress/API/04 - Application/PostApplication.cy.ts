import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody } from '../../support/requestBodies';

describe('Verify POST application responses', () => {
    const baseApplicationRequestBody = {
        data: {
            school: 123456,
            parentFirstName: 'Homer',
            parentLastName: 'Simpson',
            parentEmail: 'homer@example.com',
            parentNationalInsuranceNumber: 'AB123456C',
            parentNationalAsylumSeekerServiceNumber: '',
            parentDateOfBirth: '1990-01-01',
            childFirstName: 'Jane',
            childLastName: 'Smith',
            childDateOfBirth: '2005-01-01',
            userID: Cypress.env('USER_ID'),
            type: 'FreeSchoolMeals'
        }
    };

    it('Verify 201 Created response is returned with valid application', () => {
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            cy.apiRequest('POST', 'Application', baseApplicationRequestBody, token).then((response) => {
                // Assert the status and statusText
                cy.verifyApiResponseCode(response, 201);

                // Assert the response body data
                cy.verifyPostApplicationResponse(response, baseApplicationRequestBody);
            });
        });
    });

    it('Verify 400 Bad request response is returned with invalid application (no parent last name)', () => {
        const invalidApplicationNoLastNameRequestBody = {
            ...baseApplicationRequestBody,
            data: {
                ...baseApplicationRequestBody.data,
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
    const baseApplicationRequestBody = {
        data: {
            school: 107126,
            parentFirstName: 'Homer',
            parentLastName: 'Simpson',
            parentEmail: 'homer@example.com',
            parentNationalInsuranceNumber: '',
            parentNationalAsylumSeekerServiceNumber: 'AB123456C',
            parentDateOfBirth: '1985-01-01',
            childFirstName: 'Jane',
            childLastName: 'Simpson',
            childDateOfBirth: '2005-01-01',
            type: 'FreeSchoolMeals'
        }
    };

    it('Verify 400 Bad request response is returned with invalid child last name', () => {
        const invalidApplicationNoChildLastNameRequestBody = {
            ...baseApplicationRequestBody,
            data: {
                ...baseApplicationRequestBody.data,
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
            ...baseApplicationRequestBody,
            data: {
                ...baseApplicationRequestBody.data,
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
