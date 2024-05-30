import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody } from '../../support/requestBodies';

const filePath = 'cypress/fixtures/GIASDataSubset.csv';

describe('Import School Data', () => {

    it('Verify 200 response is returned when a valid file is uploaded', () => {
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            cy.fixture('GIASDataSubset.csv').then(fileContent => {
                const formData = new FormData();
                const blob = new Blob([fileContent], { type: 'text/csv' });
                formData.append('file', blob, 'GIASDataSubset.csv');

                const expectedResponseProperty = 'data';
                const expectedResponseValue = 'GIASDataSubset.csv - Establishment File Processed.';
                cy.api({
                    method: 'POST',
                    url: 'importEstablishments',
                    body: formData,
                    headers: {
                        'Content-Type': 'multipart/form-data',
                        'Authorization': `Bearer ${token}`
                    }
                    
                }).then(response => {
                    
                    expect(response.status).to.eq(200);
                    cy.log(response.body.data)
                    // Verify the response property and its value
                    expect(response.body).to.have.property(expectedResponseProperty, expectedResponseValue);
                });
            });
        })

    });

})