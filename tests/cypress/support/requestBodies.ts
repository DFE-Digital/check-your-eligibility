
export const validLoginRequestBody = {
    username: Cypress.env('JWT_USERNAME'),
    password: Cypress.env('JWT_PASSWORD')
};

export const validHMRCRequestBody = ({
    data: {
        nationalInsuranceNumber: 'NN123456C',
        lastName: Cypress.env('lastName'),
        dateOfBirth: '2000-01-01',
        nationalAsylumSeekerServiceNumber: ''
    }
});

export const validHomeOfficeRequestBody = ({
    data: {
        nationalInsuranceNumber: '',
        lastName: Cypress.env('lastName'),
        dateOfBirth: '1990-01-01',
        nationalAsylumSeekerServiceNumber: 'NN123456C'
    },
});


export const ValidApplicationRequestBody = ({
    data: {
        id: 'string',
        reference: 'string',
        localAuthority: 0,
        school: 0,
        parentFirstName: 'John',
        parentLastName: Cypress.env('lastName'),
        parentNationalInsuranceNumber: 'string',
        parentNationalAsylumSeekerServiceNumber: 'string',
        parentDateOfBirth: '1970-01-01',
        childFirstName: 'Jane',
        childLastName: 'Doe',
        childDateOfBirth: '2010-01-01'
    },
    links: {
        get_Application: 'string'
    }
});