// /FreeSchoolMeals/{guid
import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody  } from '../../support/requestBodies';




describe('GET eligibility soft check by Guid', () => {
    const ValidApplicationRequestBody = {
        data: {
            school: 123456,
            parentFirstName: 'Homer',
            parentLastName: 'Simpson',
            parentNationalInsuranceNumber: 'AB123456C',
            parentNationalAsylumSeekerServiceNumber: '',
            parentDateOfBirth: '1990-01-01',
            childFirstName: 'Jane',
            childLastName: 'Smith',
            childDateOfBirth: '2005-01-01',
        }
    };
    it('Verify 200 Success response is returned with valid guid', () => {
        //Get token
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            //Make post request for eligibility check
            cy.apiRequest('POST', 'FreeSchoolMeals/Application', ValidApplicationRequestBody, token).then((response) => {
                if (response.status == 500) {
                    cy.log('Status text:', response.statusText);
                    cy.log('Response body', response.body);
                }
                cy.verifyApiResponseCode(response, 201);
                //extract Guid
                cy.extractGuid(response);

                //make get request using the guid 
                cy.get('@Guid').then((Guid) => {
                    cy.apiRequest('GET', `freeSchoolMeals/Application/${Guid}`, {}, token).then((newResponse) => {
                        // Assert the response 
                        cy.verifyApiResponseCode(newResponse, 200)
                        cy.verifyGetApplicationResponse(newResponse, ValidApplicationRequestBody)
                    })
                });
            });
        });
    })
    it('Verify 401 response is returned when bearer token is not provided', () => {
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            //Make post request for eligibility check
            cy.apiRequest('POST', 'FreeSchoolMeals/Application', ValidApplicationRequestBody, token).then((response) => {
                cy.verifyApiResponseCode(response, 201);
                //extract Guid
                cy.extractGuid(response);
                //make get request using the guid 
                cy.get('@Guid').then((Guid) => {
                    cy.apiRequest('GET', `freeSchoolMeals/Application/${Guid}`, {} ).then((newResponse) => {
                        // Assert the response 
                        cy.verifyApiResponseCode(newResponse, 401)
                    })
                });
            });
        });
    })
})