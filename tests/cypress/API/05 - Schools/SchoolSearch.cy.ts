import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody } from '../../support/requestBodies';

describe('Verify School Search', () => {

  const expectedSchoolData = {
    id: 139856,
    name: "Hinde House 2-16 Academy",
    postcode: "S5 6AG",
    street: "Shiregreen Lane",
    locality: "",
    town: "Sheffield",
    county: "South Yorkshire",
    la: "Sheffield",
    distance: 0.5416666666666666

  };
  const searchCriteria = 'hinde house';


  it('Verify 200 OK and Bearer Token Is Returned when Valid Credentials are used', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('GET', `Schools/search?query=${searchCriteria}`, {}, token).then((response) => {
        cy.verifyApiResponseCode(response, 200)
        cy.verifySchoolSearchResponse(response, expectedSchoolData);
      })
    });
  });

  it('Verify 400 response is returned for invalid search criteria', () => {
    const invalidSearchCriteria = 'ab'
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('GET', `Schools/search?query=${invalidSearchCriteria}`, {}, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)

      })
    });
  });


  it('Verify 401 response is returned when bearer token is not provided', () => {
    cy.apiRequest('GET', `Schools/search?query=${searchCriteria}`, {},).then((response) => {
      cy.verifyApiResponseCode(response, 401)
    });
  });
})  