// /FreeSchoolMeals/Application/Search

import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody, validApplicationRequestBody, validApplicationSupportRequestBody, validUserRequestBody } from '../../support/requestBodies';


describe('Update Application Status', () => {
  const validBaseApplicationRequest = validApplicationRequestBody();
    
    it('Verify 200 Success response is returned', () => {

      getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
          const requestBody = validApplicationSupportRequestBody();
          cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', requestBody, token).then((response) => {
              cy.verifyApiResponseCode(response, 202);
              cy.apiRequest('POST', '/Users', validUserRequestBody(), token).then((response) => {
                  validBaseApplicationRequest.Data.UserId = response.Data;
                  cy.wait(60000);

                  cy.apiRequest('POST', 'Application', validBaseApplicationRequest, token).then((response) => {
                      // Assert the status and statusText
                      cy.verifyApiResponseCode(response, 201);

                      // Assert the response body data
                      cy.verifyPostApplicationResponse(response, validBaseApplicationRequest);

                      const applicationId = response.body.data.id;

                      const updatedRequestBody = {
                          "data": {
                              status: "EvidenceNeeded"
                          }
                      };

                      cy.apiRequest('PATCH', `Application/${applicationId}`, updatedRequestBody, token).then((patchResponse) => {
                          cy.verifyApiResponseCode(patchResponse, 200);

                          expect(patchResponse.body.data).to.have.property('status', 'EvidenceNeeded');
                      })
                  });
              });
          });
      });
    })


})