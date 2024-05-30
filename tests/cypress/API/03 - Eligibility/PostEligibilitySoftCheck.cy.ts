///FreeSchoolMeals
import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody} from '../../support/requestBodies';


describe('Post Eligibility Check - Valid Requests', () => {

  const validHMRCRequestBody = {
    data: {
      nationalInsuranceNumber: 'AB123456C',
      lastName: 'Smith',
      dateOfBirth: '01/01/2000',
      nationalAsylumSeekerServiceNumber: ''
    }
  };

  const validHomeOfficeRequestBody = {
    data: {
      nationalInsuranceNumber: '',
      lastName: 'Simpson',
      dateOfBirth: '01/01/1990',
      nationalAsylumSeekerServiceNumber: 'AB123456C'
    }
  };

  it('Verify 202 Accepted response is returned with valid HMRC data', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'FreeSchoolMeals', validHMRCRequestBody, token).then((response) => {
        // Assert the status and statusText
        cy.verifyApiResponseCode(response, 202)

        // Assert the response body data
        cy.verifyPostEligibilityCheckResponse(response)
      });
    });
  });


  it('Verify 202 Accepted response is returned with valid Home Office data', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'FreeSchoolMeals', validHomeOfficeRequestBody, token).then((response) => {
        // Assert the status and statusText
        cy.verifyApiResponseCode(response, 202)
        // Assert the response body data
        cy.verifyPostEligibilityCheckResponse(response)
      });
    });
  });
})

describe('Post Eligibility Check - Invalid Requests', () => {
  it('Verify 400 Bad Request response is returned with invalid National Insurance number', () => {

    const InvalidNationalInsuranceRequestBody = {
      data: {
        nationalInsuranceNumber: 'AAG123456C',
        lastName: 'Smith',
        dateOfBirth: '01/01/2000',
        nationalAsylumSeekerServiceNumber: ''
      }
    };
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'FreeSchoolMeals', InvalidNationalInsuranceRequestBody, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body).to.have.property('data', 'Invalid National Insurance Number');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid date of birth', () => {
    const InvalidDOBRequestBody = {
      data: {
        nationalInsuranceNumber: 'AB123456C',
        lastName: 'Smith',
        dateOfBirth: '01/01/19',
        nationalAsylumSeekerServiceNumber: ''
      }
    };
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'FreeSchoolMeals', InvalidDOBRequestBody, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body).to.have.property('data', 'Date of birth is required:- (dd/mm/yyyy)');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid last name', () => {
    const InvalidLastNameRequestBody = {
      data: {
        nationalInsuranceNumber: 'AB123456C',
        lastName: '',
        dateOfBirth: '01/01/2000',
        nationalAsylumSeekerServiceNumber: ''
      }
    };
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'FreeSchoolMeals', InvalidLastNameRequestBody, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body).to.have.property('data', 'LastName is required');
      });
    });
  });

  it('Verify 400 Bad Request response is returned with invalid NI and Nass number', () => {
    const NoNIAndNASSNRequestBody = {
      data: {
        nationalInsuranceNumber: '',
        lastName: 'Smith',
        dateOfBirth: '01/01/1990',
        nationalAsylumSeekerServiceNumber: ''
      }
    };

    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('POST', 'FreeSchoolMeals', NoNIAndNASSNRequestBody, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)
        expect(response.body).to.have.property('data', 'National Insurance Number or National Asylum Seeker Service Number is required');
      });
    });
  });


});

