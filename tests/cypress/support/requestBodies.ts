
export const validLoginRequestBody = {
    username: Cypress.env('JWT_USERNAME'),
    password: Cypress.env('JWT_PASSWORD')
};

export function validHMRCRequestBody() {
    return {
        data: {
            nationalInsuranceNumber: 'NN123456C',
            lastName: Cypress.env('lastName'),
            dateOfBirth: '2000-01-01',
            nationalAsylumSeekerServiceNumber: ''
        }
    }
}

export function invalidHMRCRequestBody() {
    return {
        data: {
            nationalInsuranceNumber: 'PPG123456C',
            lastName: 'Smith',
            dateOfBirth: '2000-01-01',
            nationalAsylumSeekerServiceNumber: ''
        }
    }
}


export function validHomeOfficeRequestBody () {
    return {
        data: {
            nationalInsuranceNumber: '',
            lastName: Cypress.env('lastName'),
            dateOfBirth: '1990-01-01',
            nationalAsylumSeekerServiceNumber: 'AB123456C'
        }
    }
};

export function notEligibleHomeOfficeRequestBody () {
    return { 
        data: {
            nationalInsuranceNumber: 'PP123456C',
            lastName: 'Jacob',
            dateOfBirth: '1990-01-01',
            nationalAsylumSeekerServiceNumber: ''
        }
    }
}

export function invalidDOBRequestBody() {
    return {
        data: {
            nationalInsuranceNumber: 'AB123456C',
            lastName: 'Smith',
            dateOfBirth: '01/01/19',
            nationalAsylumSeekerServiceNumber: ''
        }
    }
}

export function invalidLastNameRequestBody() {
    return {
        data: {
            nationalInsuranceNumber: 'AB123456C',
            lastName: '',
            dateOfBirth: '2000-01-01',
            nationalAsylumSeekerServiceNumber: ''
        }
    }
}

export function noNIAndNASSNRequestBody() {
    return {
        data: {
            nationalInsuranceNumber: '',
            lastName: 'Smith',
            dateOfBirth: '1990-01-01',
            nationalAsylumSeekerServiceNumber: ''
        }
    }
}

export function validApplicationRequestBody() {
    return {
        Data: {
            type: "FreeSchoolMeals",
            Establishment: 123456,
            ParentFirstName: Cypress.env('lastName'),
            ParentLastName: "Web",
            ParentNationalInsuranceNumber: "NN668767B",
            ParentNationalAsylumSeekerServiceNumber: null,
            ParentDateOfBirth: "1967-03-07",
            ChildFirstName: "Alexa",
            ChildLastName: "Crittenden",
            ChildDateOfBirth: "2007-08-14",
            UserId: "bc2b0328-9bf6-4a2f-901d-ea694c2b0839",
            ParentEmail :"PostmanTest@test.com"
        }
    }
}
