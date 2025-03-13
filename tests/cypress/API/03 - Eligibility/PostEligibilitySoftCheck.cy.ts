///FreeSchoolMeals
import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody, validHMRCRequestBody, validHomeOfficeRequestBody, invalidHMRCRequestBody, invalidDOBRequestBody, invalidLastNameRequestBody, noNIAndNASSNRequestBody} from '../../support/requestBodies';


describe('Post Eligibility Check - Valid Requests', () => {

  const validHMRCRequest = validHMRCRequestBody();
  const validHomeOfficeRequest = validHomeOfficeRequestBody();


  it('Verify 202 Accepted response is returned with valid HMRC data', () => {
    getandVerifyBearerToken('/oauth2/token', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'check/free-school-meals', validHMRCRequest, token).then((response) => {
        // Assert the status and statusText
        cy.verifyApiResponseCode(response, 202)

        // Assert the response body data
        cy.verifyPostEligibilityCheckResponse(response)
      });
    });
  });


  it('Verify 202 Accepted response is returned with valid Home Office data', () => {
    getandVerifyBearerToken('/oauth2/token', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'check/free-school-meals', validHomeOfficeRequest, token).then((response) => {
        // Assert the status and statusText
        cy.verifyApiResponseCode(response, 202)
        // Assert the response body data
        cy.verifyPostEligibilityCheckResponse(response)
      });
    });
  });
})

describe('Post Eligibility Check - Invalid Requests', () => {

  const invalidHMRCRequest = invalidHMRCRequestBody();
  const invalidDOBRequest = invalidDOBRequestBody();
  const invalidLastNameRequest = invalidLastNameRequestBody();
  const noNIAndNASSRequest = noNIAndNASSNRequestBody();

  it('Verify 400 Bad Request response is returned with invalid National Insurance number', () => {

    getandVerifyBearerToken('/oauth2/token', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'check/free-school-meals', invalidHMRCRequest, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body.errors[0]).to.have.property('title', 'Invalid National Insurance Number');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid date of birth', () => {

    getandVerifyBearerToken('/oauth2/token', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'check/free-school-meals', invalidDOBRequest, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body.errors[0]).to.have.property('title', 'Date of birth is required:- (yyyy-mm-dd)');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid last name', () => {

    getandVerifyBearerToken('/oauth2/token', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'check/free-school-meals', invalidLastNameRequest, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body.errors[0]).to.have.property('title', 'LastName is required');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid NI and Nass number', () => {

    getandVerifyBearerToken('/oauth2/token', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'check/free-school-meals', noNIAndNASSRequest, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body.errors[0]).to.have.property('title', 'National Insurance Number or National Asylum Seeker Service Number is required');
      });
    });
  });


});

