// /FreeSchoolMeals/Application/Search

import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody, validHMRCRequestBody, ValidApplicationRequestBody } from '../../support/requestBodies';
import { ApplicationData } from '../../support/interfaces';


describe('Update Application Status', () => {

    const body = {
        "data": {
            status: "EvidenceNeeded"
          }
    }

    const expectedApplicationsData: ApplicationData[] = [
        {
          id: "bf96e60e-2030-4682-9742-0bd97787d6e2",
          reference: "62719512",
          school: {
            id: 100020,
            name: "Primrose Hill School",
            localAuthority: {
              id: 202,
              name: "Camden"
            }
          },
          parentFirstName: "Homer",
          parentLastName: "Simpson",
          parentNationalInsuranceNumber: "AB123456C",
          parentNationalAsylumSeekerServiceNumber: "",
          parentDateOfBirth: "1985-01-01",
          childFirstName: "Tom",
          childLastName: "Bloggs",
          childDateOfBirth: "2001-01-01",
          status: "Open",
          user: null
        }
    ];
    
    it('Verify 200 Success response is returned', () => {

        //Get token
        getandVerifyBearerToken('api/Login', validLoginRequestBody).then((token) => {
            //Make post request for eligibility check
            cy.apiRequest('POST', 'FreeSchoolMeals/Application/Search', body, token).then((response) => {
                // Assert the status and statusText
                if (response.status == 400) {
                    cy.log('Status text:', response.statusText);
                    cy.log('Response body', JSON.stringify(response.body));
                }
                cy.verifyApiResponseCode(response, 200);
                cy.verifyApplicationSearchResponse(response, expectedApplicationsData)
            })
        })
    })

})