// /FreeSchoolMeals/{guid
import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody, validHMRCRequestBody } from '../../support/requestBodies';


describe('GET eligibility soft check by Guid', () => {
    it('Verify 200 Success response is returned with valid guid', () => {
        //Get token
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            //Make post request for eligibility check
            cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', validHMRCRequestBody, token).then((response) => {
                cy.verifyApiResponseCode(response, 202);
                //extract Guid
                cy.extractGuid(response);

                //make get request using the guid 
                cy.get('@Guid').then((Guid) => {
                    cy.apiRequest('GET', `EligibilityCheck/${Guid}`, {}, token).then((newResponse) => {
                        // Assert the response 
                        cy.verifyApiResponseCode(newResponse, 200)
                        cy.verifyGetEligibilityCheckResponseData(newResponse, validHMRCRequestBody)
                    })
                });
            });
        });
    })
    it('Verify 404 Not Found response is returned with invalid guid', () => {
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            cy.apiRequest('GET', 'freeSchoolMeals/7fc12dc9-5a9d-4155-887c-d2b3d60384e', {}, token).then((response) => {
                cy.verifyApiResponseCode(response, 404)
            });
        });
    });

})