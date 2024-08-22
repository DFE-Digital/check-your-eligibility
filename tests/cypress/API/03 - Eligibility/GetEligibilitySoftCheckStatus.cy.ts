// /FreeSchoolMeals/{guid
import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody } from '../../support/requestBodies';

const validHomeOfficeRequestBody = {
  data: {
    nationalInsuranceNumber: '',
    lastName: 'Simpson',
    dateOfBirth: '1990-01-01',
    nationalAsylumSeekerServiceNumber: '240712349'
  }
};
describe('GET eligibility soft check  Status ', () => {
  it('Verify 200 Success response is returned with valid guid', () => {
    cy.createEligibilityCheckAndGetStatus('api/Login', validLoginRequestBody, 'FreeSchoolMeals', validHomeOfficeRequestBody);
  });

  it('Verify 404 Not Found response is returned with invalid guid', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('GET', 'freeSchoolMeals/7fc12dc9-5a9d-4155-887c-d2b3d60384e/status', {}, token).then((response) => {
        cy.verifyApiResponseCode(response, 404)
      });
    });
  });

})


describe('Verify Eligibility Check Statuses', () => {
  const NotEligibleHomeOfficeRequestBody = {
    data: {
      nationalInsuranceNumber: '',
      lastName: 'Jacob',
      dateOfBirth: '1990-01-01',
      nationalAsylumSeekerServiceNumber: 'AB123456C'
    }
  };
  it('Verify Eligible status is returned', () => {
    cy.createEligibilityCheckAndGetStatus('api/Login', validLoginRequestBody, 'FreeSchoolMeals', validHomeOfficeRequestBody)
    cy.get('@status').then((status: any) => {
      expect(status).to.equal('eligible')
    })
  })

  it('Verify parentNotFound status is returned', () => {
    cy.createEligibilityCheckAndGetStatus('api/Login', validLoginRequestBody, 'FreeSchoolMeals', NotEligibleHomeOfficeRequestBody)
    cy.get('@status').then((status: any) => {
      expect(status).to.equal('parentNotFound')

    })
  })
  it('Verify queuedForProcessing status is returned', () => {
    cy.updateLastName(NotEligibleHomeOfficeRequestBody).then((updatedRequestBody) => {
      cy.createEligibilityCheckAndGetStatus('api/Login', validLoginRequestBody, 'FreeSchoolMeals', updatedRequestBody)
      cy.get('@status').then((status: any) => {
        expect(status).to.equal('queuedForProcessing')
      })

    })
  })
})