import { verifyUnauthorizedWithoutToken, getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody } from '../../support/requestBodies';


describe('Authorisation Tests', () => {
  const invalidRequestBody = { username: 'ecsUiUser', password: 'adsadad' };

  it('Verify 200 response and Bearer Token Is Returned when Valid Credentials are used', () => {    
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
    });
  });

  it('Verify 401 is returned with invalid credentials', () => {
    cy.apiRequest('POST', 'api/Login', invalidRequestBody).then((response) => {
      cy.verifyApiResponseCode(response, 401)
    });
  });

  it('Verify 401 Unauthorized is returned when token is not provided for protected endpoints', () => {
    verifyUnauthorizedWithoutToken('POST', 'api/Login', invalidRequestBody);
    verifyUnauthorizedWithoutToken('POST', 'FreeSchoolMeals', invalidRequestBody);
    verifyUnauthorizedWithoutToken('POST', 'FreeSchoolMeals/Application', invalidRequestBody);
  });
});
