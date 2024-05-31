
export const validLoginRequestBody = {
    username: 'user',
    password: 'pass'
};

export const validHMRCRequestBody = {
    data: {
        nationalInsuranceNumber: 'AB123456C',
        lastName: 'Smith',
        dateOfBirth: '01/01/2000',
        nationalAsylumSeekerServiceNumber: ''
    }
};

export const validHomeOfficeRequestBody = {
    data: {
        nationalInsuranceNumber: '',
        lastName: 'Simpson',
        dateOfBirth: '01/01/1990',
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