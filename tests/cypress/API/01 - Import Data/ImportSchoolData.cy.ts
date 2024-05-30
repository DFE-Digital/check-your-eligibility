import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody } from '../../support/requestBodies';


describe('Import School Data', () => {

    it('Verify 200 response and Bearer Token Is Returned when Valid Credentials are used', () => {
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            cy.apiRequest('POST', 'importEstablishments',{}, token).then((response) => {
                cy.verifyApiResponseCode(response, 200)
            })
        });
    });

});
