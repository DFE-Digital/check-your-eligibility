// cypress/support/commands.ts
/// <reference types="cypress" />
declare namespace Cypress {
  interface Chainable<Subject = any> {
    saveBearerToken(): Chainable<any>;
    apiRequest(method: string, url: string, requestBody: any, bearerToken?: string | null, failOnStatusCode?: boolean): Chainable<any>;
    verifyPostEligibilityCheckResponse(response: any): Chainable<any>;
    extractGuid(response: any): Chainable<string>;
    verifyGetEligibilityCheckResponseData(response: any, requestData: any): Chainable<void>;
    verifyApiResponseCode(response: any, expectedStatus: number): Chainable<void>;
    verifyGetEligibilityCheckStatusResponse(response: any): Chainable<void>;
    createEligibilityCheckAndGetStatus(loginUrl: string, loginRequestBody: any, eligibilityCheckUrl: string, eligibilityCheckRequestBody: any): Chainable<any>;
    updateLastName(requestBody: any): Chainable<any>;
    verifyPostApplicationResponse(response: any, requestData: any): Chainable<void>;
    verifyGetApplicationResponse(response: any, requestData: any): Chainable<void>;
    verifyTotalElements(totalElements: number, expectedTotalElements: number): Chainable<void>;
    verifySchoolSearchResponse(response: any, expectedData: any): Chainable<void>;
    verifyApplicationSearchResponse(response: any, expectedDataArray: any[]): Chainable<void>;
    form_request(method: string, url: string, formData: FormData, token: string, done: (response: XMLHttpRequest) => void): Chainable<void>;

  

  }
}


Cypress.Commands.add('saveBearerToken', () => {
  cy.get('@apiResponse').then((response: any) => {
    expect(response.status).to.equal(200);
    expect(response.body).to.have.property('token');
    const token = response.body.token;
    cy.wrap(token).as('bearerToken');
  });
});

Cypress.Commands.add('apiRequest', (method: string, url: string, requestBody: any, bearerToken: string | null = null, failOnStatusCode: boolean = false) => {
  const options: Partial<Cypress.RequestOptions> = {
    method: method,
    url: url,
    body: requestBody,
    failOnStatusCode: failOnStatusCode
  };

  if (bearerToken) {
    options.headers = {
      'Authorization': `Bearer ${bearerToken}`,
      'Content-Type': 'application/json'
    };
  }
  return cy.request(options);
});

Cypress.Commands.add('verifyPostEligibilityCheckResponse', (response) => {
  expect(response.body).to.have.property('data');
  expect(response.body).to.have.property('links');
  const responseData = response.body.data;
  const responseLinks = response.body.links;

  const totalElements = Object.keys(responseData).length + Object.keys(responseLinks).length;

  // Verfiy total number of elements
  cy.verifyTotalElements(totalElements, 4);

  // Verify response elements
  expect(response.body.data).to.have.property('status');
  expect(response.body.links).to.have.property('get_EligibilityCheck');
  expect(response.body.links).to.have.property('put_EligibilityCheckProcess');
  expect(response.body.links).to.have.property('get_EligibilityCheckStatus');


});

Cypress.Commands.add('extractGuid', (response) => {
  let guid;

  if (response.body.links && response.body.links.get_EligibilityCheck) {
    const getEligibilityCheck = response.body.links.get_EligibilityCheck;
    guid = getEligibilityCheck.substring(getEligibilityCheck.lastIndexOf('/') + 1);
  } else if (response.body.data && response.body.data.id) {
    guid = response.body.data.id;
  } else {
    throw new Error('No valid GUID found in response');
  }

  cy.wrap(guid).as('Guid');
});

Cypress.Commands.add('verifyGetEligibilityCheckResponseData', (response, requestData) => {
  // Verify body has data and links properties
  expect(response.body).to.have.property('data');
  expect(response.body).to.have.property('links');
  const responseData = response.body.data;
  const responseLinks = response.body.links;

  // Calculate total number of elements in data and links
  const totalElements = Object.keys(responseData).length + Object.keys(responseLinks).length;
  // Verfiy total number of elements
  cy.verifyTotalElements(totalElements, 9);

  expect(responseData).to.have.property('nationalInsuranceNumber', requestData.data.nationalInsuranceNumber);
  expect(responseData).to.have.property('lastName', requestData.data.lastName);
  expect(responseData).to.have.property('dateOfBirth', requestData.data.dateOfBirth);
  expect(responseData).to.have.property('nationalAsylumSeekerServiceNumber', requestData.data.nationalAsylumSeekerServiceNumber);
  expect(responseData).to.have.property('status');
  expect(responseData).to.have.property('created');
  

  // Verify links properties
  expect(responseLinks).to.have.property('get_EligibilityCheck');
  expect(responseLinks).to.have.property('put_EligibilityCheckProcess');
  expect(responseLinks).to.have.property('get_EligibilityCheckStatus');
});


Cypress.Commands.add('verifyApiResponseCode', (response, expectedStatus) => {
  const statusTextMap: { [key: number]: string } = {
    200: 'OK',
    201: 'Created',
    202: 'Accepted',
    400: 'Bad Request',
    401: 'Unauthorized',
    404: 'Not Found'
  };

  const expectedStatusText = statusTextMap[expectedStatus];

  expect(response.status).to.equal(expectedStatus);
  if (expectedStatusText) {
    expect(response.statusText).to.equal(expectedStatusText);
  } else {
    throw new Error(`Status text for status code ${expectedStatus} not defined in the map.`);
  }
});

Cypress.Commands.add('verifyGetEligibilityCheckStatusResponse', (response) => {

  expect(response.body).to.have.property('data');
  const responseData = response.body.data;
  expect(responseData.status).to.be.oneOf(['DwpError', 'eligible', 'parentNotFound', 'queuedForProcessing']);
})


Cypress.Commands.add('createEligibilityCheckAndGetStatus', (loginUrl: string, loginRequestBody: any, eligibilityCheckUrl: string, eligibilityCheckRequestBody: any) => {
  return cy.apiRequest('POST', loginUrl, loginRequestBody).then((response) => {
    cy.verifyApiResponseCode(response, 200);
    const token = response.body.token;

    return cy.apiRequest('POST', eligibilityCheckUrl, eligibilityCheckRequestBody, token).then((response) => {
      cy.verifyApiResponseCode(response, 202);
      cy.extractGuid(response);
      cy.wait(40000);
      return cy.get('@Guid').then((eligibilityCheckId) => {
        return cy.apiRequest('GET', `EligibilityCheck/${eligibilityCheckId}/Status`, {}, token).then((newResponse) => {
          cy.verifyApiResponseCode(newResponse, 200);
          const status = newResponse.body.data.status;
          cy.wrap(status).as('status');
        });
      });
    });
  });
});


Cypress.Commands.add('updateLastName', (requestBody) => {
  const randomLetters = Math.random().toString(36).substring(2, 7);
  requestBody.data.lastName = randomLetters;
  cy.wrap(requestBody).as('updatedRequestBody');
  return cy.wrap(requestBody)
})

Cypress.Commands.add('verifyPostApplicationResponse', (response, requestData) => {
  // Verify data properties
  expect(response).to.have.property('body');
  expect(response.body).to.have.property('data');
  expect(response.body).to.have.property('links');

  const responseData = response.body.data;
  const responseLinks = response.body.links;

  // Verfiy total number of elements
  const totalElements = Object.keys(responseData).length + Object.keys(responseLinks).length;
  cy.verifyTotalElements(totalElements, 17);

  // Assertions to verify response data matches request data
  expect(responseData).to.have.property('id');
  expect(responseData).to.have.property('reference');
  expect(responseData.establishment).to.have.property('localAuthority');
  expect(responseData).to.have.property('establishment');
  expect(responseData).to.have.property('parentFirstName', requestData.Data.ParentFirstName);
  expect(responseData).to.have.property('parentLastName', requestData.Data.ParentLastName);
  expect(responseData).to.have.property('parentEmail', requestData.Data.ParentEmail);
  expect(responseData).to.have.property('parentNationalInsuranceNumber', requestData.Data.ParentNationalInsuranceNumber);
  expect(responseData).to.have.property('parentNationalAsylumSeekerServiceNumber', requestData.Data.ParentNationalAsylumSeekerServiceNumber);
  expect(responseData).to.have.property('parentDateOfBirth', requestData.Data.ParentDateOfBirth);
  expect(responseData).to.have.property('childFirstName', requestData.Data.ChildFirstName);
  expect(responseData).to.have.property('childLastName', requestData.Data.ChildLastName);
  expect(responseData).to.have.property('childDateOfBirth', requestData.Data.ChildDateOfBirth);
  expect(responseData).to.have.property('status');
  expect(responseData).to.have.property('user');
  expect(responseData).to.have.property('created');
  expect(responseLinks).to.have.property('get_Application');
});


Cypress.Commands.add('verifyGetApplicationResponse', (response, expectedData) => {

  expect(response).to.have.property('body');
  expect(response.body).to.have.property('data');
  expect(response.body).to.have.property('links');

  const responseData = response.body.data;
  const responseLinks = response.body.links;

  // Verify total number of elements
  const totalElements = Object.keys(responseData).length +
    Object.keys(responseData.establishment).length +
    Object.keys(responseData.establishment.localAuthority).length +
    Object.keys(responseLinks).length;
  cy.verifyTotalElements(totalElements, 22);

  expect(responseData).to.have.property('id');
  expect(responseData).to.have.property('reference');
  expect(responseData).to.have.property('establishment');
  expect(responseData.establishment).to.have.property('id');
  expect(responseData.establishment).to.have.property('name');
  expect(responseData.establishment).to.have.property('localAuthority');
  expect(responseData.establishment.localAuthority).to.have.property('id');
  expect(responseData.establishment.localAuthority).to.have.property('name');
  expect(responseData).to.have.property('parentFirstName', expectedData.Data.ParentFirstName);
  expect(responseData).to.have.property('parentLastName', expectedData.Data.ParentLastName);
  expect(responseData).to.have.property('parentEmail', expectedData.Data.ParentEmail);
  expect(responseData).to.have.property('parentNationalInsuranceNumber', expectedData.Data.ParentNationalInsuranceNumber);
  expect(responseData).to.have.property('parentNationalAsylumSeekerServiceNumber', expectedData.Data.ParentNationalAsylumSeekerServiceNumber);
  expect(responseData).to.have.property('parentDateOfBirth', expectedData.Data.ParentDateOfBirth);
  expect(responseData).to.have.property('childFirstName', expectedData.Data.ChildFirstName);
  expect(responseData).to.have.property('childLastName', expectedData.Data.ChildLastName);
  expect(responseData).to.have.property('childDateOfBirth', expectedData.Data.ChildDateOfBirth);
  expect(responseData).to.have.property('status');
  expect(responseData).to.have.property('user');
  expect(responseData).to.have.property('created');
  expect(responseData).to.have.property('checkOutcome');

  // Verify the links property
  expect(response.body.links).to.have.property('get_Application');
});


Cypress.Commands.add('verifyTotalElements', (totalElements, expectedTotalElements) => {
  // Check total number of elements and log appropriate messages
  if (totalElements === expectedTotalElements) {
    // Total number of elements matches the expected number
    expect(totalElements).to.equal(expectedTotalElements);
  } else {
    // Any other number of elements (less than expected)
    throw new Error(`Total number of expected elements: ${expectedTotalElements}. Actual elements in total: ${totalElements}`);
  }
});


Cypress.Commands.add('verifySchoolSearchResponse', (response, expectedData) => {

  expect(response).to.have.property('body');
  expect(response.body).to.have.property('data');

  const responseData = response.body.data;
  // Ensure the response data is an array
  expect(responseData).to.be.an('array');
  expect(responseData).to.have.length(1);

  // Extract the first item from the array
  const school = responseData[0];

  // Assertions to verify response data matches expected data
  expect(school).to.have.property('id', expectedData.id);
  expect(school).to.have.property('name', expectedData.name);
  expect(school).to.have.property('postcode', expectedData.postcode);
  expect(school).to.have.property('street', expectedData.street);
  expect(school).to.have.property('locality', expectedData.locality);
  expect(school).to.have.property('town', expectedData.town);
  expect(school).to.have.property('county', expectedData.county);
  expect(school).to.have.property('la', expectedData.la);
  expect(school).to.have.property('distance', expectedData.distance);
});


Cypress.Commands.add('verifyApplicationSearchResponse', (response, expectedDataArray) => {
  // Verify data properties
  expect(response).to.have.property('body');
  expect(response.body).to.have.property('data');

  const responseData = response.body.data;
  // Ensure the response data is an array
  expect(responseData).to.be.an('array');

  const expectedData = expectedDataArray[0];
  expect(expectedData).to.have.property('id', expectedData.id);
  expect(expectedData).to.have.property('reference', expectedData.reference);
  expect(expectedData).to.have.property('establishment');
  expect(expectedData.establishment).to.have.property('id', expectedData.establishment.id);
  expect(expectedData.establishment).to.have.property('name', expectedData.establishment.name);
  expect(expectedData.establishment).to.have.property('localAuthority');
  expect(expectedData.establishment.localAuthority).to.have.property('id', expectedData.establishment.localAuthority.id);
  expect(expectedData.establishment.localAuthority).to.have.property('name', expectedData.establishment.localAuthority.name);
  expect(expectedData).to.have.property('parentFirstName', expectedData.parentFirstName);
  expect(expectedData).to.have.property('parentLastName', expectedData.parentLastName);
  expect(expectedData).to.have.property('parentNationalInsuranceNumber', expectedData.parentNationalInsuranceNumber);
  expect(expectedData).to.have.property('parentNationalAsylumSeekerServiceNumber', expectedData.parentNationalAsylumSeekerServiceNumber);
  expect(expectedData).to.have.property('parentDateOfBirth', expectedData.parentDateOfBirth);
  expect(expectedData).to.have.property('childFirstName', expectedData.childFirstName);
  expect(expectedData).to.have.property('childLastName', expectedData.childLastName);
  expect(expectedData).to.have.property('childDateOfBirth', expectedData.childDateOfBirth);
  expect(expectedData).to.have.property('status', expectedData.status);
  expect(expectedData).to.have.property('user', expectedData.user);

});

Cypress.Commands.add('form_request', (method: string, url: string, formData: FormData, token: string, done: (response: XMLHttpRequest) => void) => {
  const xhr = new XMLHttpRequest();
  xhr.open(method, url);

  // Set the Authorization header
  xhr.setRequestHeader('Authorization', `Bearer ${token}`);

  xhr.onload = function () {
    done(xhr);
  };

  xhr.onerror = function () {
    done(xhr);
  };

  xhr.send(formData);
});
