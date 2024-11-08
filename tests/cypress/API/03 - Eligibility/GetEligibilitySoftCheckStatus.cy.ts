// /FreeSchoolMeals/{guid
import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody, validHMRCRequestBody, notEligibleHomeOfficeRequestBody } from '../../support/requestBodies';

const validRequestBody = validHMRCRequestBody();
const notEligibleRequestBody = notEligibleHomeOfficeRequestBody();

describe('GET eligibility soft check  Status ', () => {
  it('Verify 200 Success response is returned with valid guid', () => {
    
    cy.createEligibilityCheckAndGetStatus('api/Login', validLoginRequestBody, 'EligibilityCheck/FreeSchoolMeals', validRequestBody);
  });

  it('Verify 404 Not Found response is returned with invalid guid', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('GET', 'EligibilityCheck/FreeSchoolMeals/7fc12dc9-5a9d-4155-887c-d2b3d60384e/Status', {}, token).then((response) => {
        cy.verifyApiResponseCode(response, 404)
      });
    });
  });

})


describe('Verify Eligibility Check Statuses', () => {

  
  it('Verify Eligible status is returned', () => {
    cy.createEligibilityCheckAndGetStatus('api/Login', validLoginRequestBody, 'EligibilityCheck/FreeSchoolMeals', validRequestBody)
    cy.get('@status').then((status: any) => {
      expect(status).to.equal('eligible')
    })
  })

  it('Verify parentNotFound status is returned', () => {
    cy.createEligibilityCheckAndGetStatus('api/Login', validLoginRequestBody, 'EligibilityCheck/FreeSchoolMeals', notEligibleRequestBody)
    cy.get('@status').then((status: any) => {
      expect(status).to.equal('parentNotFound')

    })
  })
  // it('Verify queuedForProcessing status is returned', () => {
  //   cy.updateLastName(NotEligibleHomeOfficeRequestBody).then((updatedRequestBody) => {
  //     cy.createEligibilityCheckAndGetStatus('api/Login', validLoginRequestBody, 'EligibilityCheck/FreeSchoolMeals', updatedRequestBody)
  //     cy.get('@status').then((status: any) => {
  //       expect(status).to.equal('queuedForProcessing')
  //     })

  //   })
  // })
})