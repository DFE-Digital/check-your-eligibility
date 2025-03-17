// TODO: have only client details in the request body

export const validLoginRequestBody = {
    client_id: Cypress.env('JWT_USERNAME'),
    client_secret: Cypress.env('JWT_PASSWORD'),
    scope: Cypress.env('JWT_SCOPE') ?? "local_authority check application admin bulk_check establishment user engine"
};

export const validLoginRequestBodyWithUsernameAndPassword = {
    username: Cypress.env('JWT_USERNAME'),
    password: Cypress.env('JWT_PASSWORD')
};

export const validLoginRequestBodyWithClientDetails = {
    client_id: Cypress.env('JWT_USERNAME'),
    client_secret: Cypress.env('JWT_PASSWORD')
};

export const validLoginRequestBodyWithClientDetailsAndScope = {
    client_id: Cypress.env('JWT_USERNAME'),
    client_secret: Cypress.env('JWT_PASSWORD'),
    scope: Cypress.env('JWT_SCOPE') ?? "local_authority check application admin bulk_check establishment user engine"
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
            nationalInsuranceNumber: '',
            lastName: 'Jacob',
            dateOfBirth: '1990-01-01',
            nationalAsylumSeekerServiceNumber: '111111111'
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

export function validApplicationSupportRequestBody() {
    return {
        data: {
            nationalInsuranceNumber: 'NN668767B',
            lastName: Cypress.env('lastName'),
            dateOfBirth: '1967-03-07',
            nationalAsylumSeekerServiceNumber: ''
        }
    }
}

export function validUserRequestBody() {
    return {
        data: {
            email: 'mar@ten.com',
            reference: 'lolz'
        }
    }
}

export function validApplicationRequestBody() {
    return {
        Data: {
            type: "FreeSchoolMeals",
            Establishment: 123456,
            ParentFirstName: "Lebb",
            ParentLastName: Cypress.env('lastName'),
            ParentNationalInsuranceNumber: "NN668767B",
            ParentNationalAsylumSeekerServiceNumber: null,
            ParentDateOfBirth: "1967-03-07",
            ChildFirstName: "Alexa",
            ChildLastName: "Crittenden",
            ChildDateOfBirth: "2007-08-14",
            UserId: "bc2b0328-9bf6-4a2f-901d-ea694c2b0838",
            ParentEmail :"PostmanTest@test.com"
        }
    }
}
