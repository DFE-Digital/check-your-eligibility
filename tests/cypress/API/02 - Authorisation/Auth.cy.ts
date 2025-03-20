import { verifyUnauthorizedWithoutToken, getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBodyWithUsernameAndPassword, validLoginRequestBodyWithClientDetails, validLoginRequestBodyWithClientDetailsAndScope } from '../../support/requestBodies';


describe('Authorisation Tests', () => {
  const invalidRequestBody = { lolzname: 'ecsUiUser', password: '123456' };
  const invalidClientDetails = { client_id: 'invalidClientId', client_secret: 'invalid Secret', scope: 'invalidScope' };

  it('Verify 200 response and Bearer Token Is Returned when Valid Credentials are used', () => { 
    getandVerifyBearerToken('/oauth2/token', validLoginRequestBodyWithUsernameAndPassword).then((token) => {
    });
  });

  it('Verify 200 response and Bearer Token Is Returned when Valid Client Details are used', () => {
    getandVerifyBearerToken('/oauth2/token', validLoginRequestBodyWithClientDetails).then((token) => {
    });
  });

  it('Verify 200 response and Bearer Token Is Returned when Valid Client Details with scope are used', () => {
    getandVerifyBearerToken('/oauth2/token', validLoginRequestBodyWithClientDetailsAndScope).then((token) => {
    });
  });

  it('Verify 401 is returned with invalid credentials', () => {
    cy.apiRequest('POST', '/oauth2/token', invalidRequestBody).then((response) => {
      cy.verifyApiResponseCode(response, 401)
    });
  });

  it('Verify 401 is returned with invalid client details', () => {
    verifyUnauthorizedWithoutToken('POST', '/oauth2/token', invalidClientDetails);
  });

  it('Verify 401 Unauthorized is returned when token is not provided for protected endpoints', () => {
    verifyUnauthorizedWithoutToken('POST', '/oauth2/token', invalidClientDetails);
    verifyUnauthorizedWithoutToken('POST', 'check/free-school-meals', invalidClientDetails);
    verifyUnauthorizedWithoutToken('POST', 'application', invalidClientDetails);
  });
});
