
export const validLoginRequestBody = {
    username: Cypress.env('JWT_USERNAME'),
    password: Cypress.env('JWT_PASSWORD')
};

export const validHMRCRequestBody = {
    data: {
        nationalInsuranceNumber: 'AB123456C',
        lastName: 'SMITH',
        dateOfBirth: '2000-01-01',
        nationalAsylumSeekerServiceNumber: ''
    }
};

export const validHomeOfficeRequestBody = {
    data: {
        nationalInsuranceNumber: '',
        lastName: 'Simpson',
        dateOfBirth: '1990-01-01',
        nationalAsylumSeekerServiceNumber: 'AB123456C'
    }
};


export const ValidApplicationRequestBody = {
    data: {
        id: 'string',
        reference: 'string',
        localAuthority: 0,
        school: 0,
        parentFirstName: 'John',
        parentLastName: 'Doe',
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
};