///FreeSchoolMeals
import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody, validHMRCRequestBody, validHomeOfficeRequestBody, invalidHMRCRequestBody, invalidDOBRequestBody, invalidLastNameRequestBody, noNIAndNASSNRequestBody} from '../../support/requestBodies';


describe('Post Eligibility Check - Valid Requests', () => {

  const validHMRCRequest = validHMRCRequestBody();
  const validHomeOfficeRequest = validHomeOfficeRequestBody();


  it('Verify 202 Accepted response is returned with valid HMRC data', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', validHMRCRequest, token).then((response) => {
        // Assert the status and statusText
        cy.verifyApiResponseCode(response, 202)

        // Assert the response body data
        cy.verifyPostEligibilityCheckResponse(response)
      });
    });
  });


  it('Verify 202 Accepted response is returned with valid Home Office data', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', validHomeOfficeRequest, token).then((response) => {
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

    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', invalidHMRCRequest, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body).to.have.property('data', 'Invalid National Insurance Number');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid date of birth', () => {

    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', invalidDOBRequest, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body).to.have.property('data', 'Date of birth is required:- (yyyy-mm-dd)');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid last name', () => {

    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', invalidLastNameRequest, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body).to.have.property('data', 'LastName is required');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid NI and Nass number', () => {

    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'EligibilityCheck/FreeSchoolMeals', noNIAndNASSRequest, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body).to.have.property('data', 'National Insurance Number or National Asylum Seeker Service Number is required');
      });
    });
  });


});

