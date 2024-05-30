// /FreeSchoolMeals/Application/Search

import { getandVerifyBearerToken } from '../../support/apiHelpers';
import { validLoginRequestBody, validHMRCRequestBody, ValidApplicationRequestBody } from '../../support/requestBodies';
import { ApplicationData } from '../../support/interfaces';


describe('Update Application Status', () => {

    const body = {
        data: {
            "localAuthority": 896,
            "school": 111510,
            "status": "Open"
        }
    }

    const expectedApplicationsData: ApplicationData[] = [
        {
          id: "bf96e60e-2030-4682-9742-0bd97787d6e2",
          reference: "62719512",
          school: {
            id: 111510,
            name: "Hinderton School",
            localAuthority: {
              id: 896,
              name: "Cheshire West and Chester"
            }
          },
          parentFirstName: "Homer",
          parentLastName: "Simpson",
          parentNationalInsuranceNumber: "AB123456C",
          parentNationalAsylumSeekerServiceNumber: "",
          parentDateOfBirth: "01/01/1985",
          childFirstName: "Tom",
          childLastName: "sdf",
          childDateOfBirth: "01/01/2001",
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
                cy.verifyApiResponseCode(response, 200);
                cy.verifyApplicationSearchResponse(response, expectedApplicationsData)
            })
        })
    })

})