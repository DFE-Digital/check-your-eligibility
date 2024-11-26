// /FreeSchoolMeals/{guid
import { getandVerifyBearerToken } from '../../support/apiHelpers';
import {
    validLoginRequestBody,
    validApplicationRequestBody,
    validApplicationSupportRequestBody,
    validUserRequestBody
} from '../../support/requestBodies';




describe('GET eligibility soft check by Guid', () => {
    const validApplicationRequest = validApplicationRequestBody();

    it('Verify 200 Success response is returned with valid guid', () => {
        //Get token
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            const requestBody = validApplicationSupportRequestBody();
            cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', requestBody, token).then((response) => {
                cy.verifyApiResponseCode(response, 202);
                cy.apiRequest('POST', '/Users', validUserRequestBody(), token).then((response) => {
                    validApplicationRequest.Data.UserId = response.Data;
                    //Make post request for eligibility check
                    cy.apiRequest('POST', 'Application', validApplicationRequest, token).then((response) => {
                        cy.verifyApiResponseCode(response, 201);
                        //extract Guid
                        cy.extractGuid(response);

                        //make get request using the guid 
                        cy.get('@Guid').then((Guid) => {
                            cy.apiRequest('GET', `Application/${Guid}`, {}, token).then((newResponse) => {
                                // Assert the response 
                                cy.verifyApiResponseCode(newResponse, 200)
                                cy.log(JSON.stringify(validApplicationRequest))
                                cy.verifyGetApplicationResponse(newResponse, validApplicationRequest)
                            })
                        });
                    });
                });
            });
        });
    })
    it('Verify 401 response is returned when bearer token is not provided', () => {
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            //Make post request for eligibility check
            cy.apiRequest('POST', 'Application', validApplicationRequest, token).then((response) => {
                cy.verifyApiResponseCode(response, 201);
                //extract Guid
                cy.extractGuid(response);
                //make get request using the guid 
                cy.get('@Guid').then((Guid) => {
                    cy.apiRequest('GET', `Application/${Guid}`, {} ).then((newResponse) => {
                        // Assert the response 
                        cy.verifyApiResponseCode(newResponse, 401)
                    })
                });
            });
        });
    })
})