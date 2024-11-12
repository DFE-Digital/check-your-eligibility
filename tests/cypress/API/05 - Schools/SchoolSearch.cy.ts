import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody } from '../../support/requestBodies';

describe('Verify School Search', () => {

  const expectedSchoolData = {
    "id": 100020,
    "name": "Primrose Hill School",
    "postcode": "NW1 8JL",
    "street": "Princess Road",
    "locality": "",
    "town": "London",
    "county": "",
    "la": "Camden",
    "distance": 0.0

  };
  const searchCriteria = 'Primrose Hill School';


  it('Verify 200 OK and Bearer Token Is Returned when Valid Credentials are used', () => {
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('GET', `Establishments/search?query=${searchCriteria}`, {}, token).then((response) => {
        cy.verifyApiResponseCode(response, 200)
        cy.verifySchoolSearchResponse(response, expectedSchoolData);
      })
    });
  });

  it('Verify 400 response is returned for invalid search criteria', () => {
    const invalidSearchCriteria = 'ab'
    getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
      cy.apiRequest('GET', `Establishments/search?query=${invalidSearchCriteria}`, {}, token).then((response) => {
        cy.verifyApiResponseCode(response, 400)

      })
    });
  });


  it('Verify 401 response is returned when bearer token is not provided', () => {
    cy.apiRequest('GET', `Establishments/search?query=${searchCriteria}`, {},).then((response) => {
      cy.verifyApiResponseCode(response, 401)
    });
  });
})  