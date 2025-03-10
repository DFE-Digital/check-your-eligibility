import { verifyUnauthorizedWithoutToken, getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBodyWithUsernameAndPassword, validLoginRequestBodyWithClientDetails } from '../../support/requestBodies';


describe('Authorisation Tests', () => {
  const invalidRequestBody = { username: 'ecsUiUser', password: '123456' };
  const invalidClientDetails = { clientId: 'invalidClientId', clientSecret: 'invalid Secret', scope: 'invalidScope' };

  it('Verify 200 response and Bearer Token Is Returned when Valid Credentials are used', () => { 
    getandVerifyBearerToken('api/Login', validLoginRequestBodyWithUsernameAndPassword).then((token) => {
    });
  });

  it('Verify 200 response and Bearer Token Is Returned when Valid Client Details are used', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBodyWithClientDetails).then((token) => {
    });
  });

  it('Verify 401 is returned with invalid credentials', () => {
    cy.apiRequest('POST', 'api/Login', invalidRequestBody).then((response) => {
      cy.verifyApiResponseCode(response, 401)
    });
  });

  it('Verify 401 is returned with invalid client details', () => {
    cy.apiRequest('POST', 'api/Login', invalidClientDetails).then((response) => {
      cy.verifyApiResponseCode(response, 401)
    });
  });

  it('Verify 401 Unauthorized is returned when token is not provided for protected endpoints', () => {
    verifyUnauthorizedWithoutToken('POST', 'api/Login', invalidRequestBody);
    verifyUnauthorizedWithoutToken('POST', 'EligibilityCheck/FreeSchoolMeals', invalidRequestBody);
    verifyUnauthorizedWithoutToken('POST', 'Application', invalidRequestBody);
  });
});
