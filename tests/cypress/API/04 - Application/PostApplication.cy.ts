import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody } from '../../support/requestBodies';

describe('Verify POST application responses', () => {
    const baseApplicationRequestBody = {
        data: {
            school: 107126,
            parentFirstName: 'Homer',
            parentLastName: 'Simpson',
            parentNationalInsuranceNumber: '',
            parentNationalAsylumSeekerServiceNumber: 'AB123456C',
            parentDateOfBirth: '01/01/1985',
            childFirstName: 'Jane',
            childLastName: 'Simpson',
            childDateOfBirth: '01/01/2005'
        }
    };

    it('Verify 201 Created response is returned with valid application', () => {
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            cy.apiRequest('POST', 'FreeSchoolMeals/Application', baseApplicationRequestBody, token).then((response) => {
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
            cy.apiRequest('POST', 'FreeSchoolMeals/Application', invalidApplicationNoLastNameRequestBody, token).then((response) => {
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
            parentNationalInsuranceNumber: '',
            parentNationalAsylumSeekerServiceNumber: 'AB123456C',
            parentDateOfBirth: '01/01/1985',
            childFirstName: 'Jane',
            childLastName: 'Simpson',
            childDateOfBirth: '01/01/2005'
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
            cy.apiRequest('POST', 'FreeSchoolMeals/Application', invalidApplicationNoChildLastNameRequestBody, token).then((response) => {
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
            cy.apiRequest('POST', 'FreeSchoolMeals/Application', invalidApplicationInvalidChildDOBRequestBody, token).then((response) => {
                // Assert the status and statusText
                cy.verifyApiResponseCode(response, 400);
                expect(response.body).to.have.property('data', "Child Date of birth is required:- (dd/mm/yyyy)");
            });
        });
    });

    it('Verify 401 response is returned when bearer token is not provided', () => {
        cy.apiRequest('POST', 'FreeSchoolMeals/Application', validLoginRequestBody,).then((response) => {
          cy.verifyApiResponseCode(response, 401)
        });
      });
});