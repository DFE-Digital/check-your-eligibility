**Cypress Setup User Guide**
**Introduction**
This guide will walk you through the steps required to install and run Cypress with TypeScript locally.

**Prerequisites**
Before you begin, ensure that you have the following installed:
•	Node.js (version 12 or higher)
•	npm (Node Package Manager)

**Setup Instructions**

1. Open Project in Visual Studio Code
Open your project repository in Visual Studio Code. Navigate to the test folder using the PowerShell terminal.

2. Install Cypress
Install Cypress as a development dependency by running the following command in your terminal:
```
npm install cypress@latest --save-dev
npx Cypress install
```
Certain machines may struggle to install Cypress and receive a certificate error. This can be overcome by getting the portal URL for your VPN and adding it to this command:

```
export HTTP_PROXY=https{url} npm i 
```

In windows PowerShell:
```
$env:HTTP_PROXY=https{url} npm i 
```
Try the installation commands again:
```
npm install cypress@latest --save-dev
npx Cypress install
```
This should install cypress. However, when working through a VPN the npx Cypress install command may hang. If this is the case then you can download the latest version of Cypress [here](https://docs.cypress.io/app/get-started/install-cypress)

Unzip this file to the desired install location. 

 C:\users\\(username)\AppData\Local\Cypress\Cache\\(version)\Cypress
 e.g.
 C:\Users\smith\AppData\Local\Cypress\Cache\14.2.0\Cypress

**Testing the install:**
Open Cypress to check it has installed correctly,
 ```
npx cypress open
```
You should see an instance of Cypress loading, ignore the config errors for now and close Cypress again.
 
4. Configure Environment Variables
Create a *cypress.env.json* file. **(DO NOT COMMIT THIS FILE)**
inside populate the following fields 
```
{
    "JWT_USERNAME": "",
    "JWT_PASSWORD": ""
}
```

For the values needed in the dev environment, please contact the lead developer.

When testing locally these values can be dummy values. The values should match that in the appsettings.json or the appsettings.development.json file.
```
"Jwt": {
        "Key": "your_complex_key",
        "Issuer": "dfe",
        "Clients": {
            "DUMMY_CLIENT_ID": {
                "Secret": "DUMMY_CLIENT_SECRET",
                "Scopes": "DUMMY_SCOPES"
            }
        },
        "Users": {
            "DUMMY_USERNAME": "DUMMY_PASSWORD"
        }
    }
```
For the above app settings, the corresponding `cypress.env.json` will be
```
{
    "JWT_USERNAME": "DUMMY_CLIENT_ID",
    "JWT_PASSWORD": "DUMMY_CLIENT_SECRET"
}
```

4. Open Cypress
Launch Cypress using the following command:
```
CYPRESS_BASE_URL="{Desired url}" npx cypress open
```
Alternatively, you can set the base URI by using the following:
```
$env:CYPRESS_BASE_URL="{Desired url}"
```
 then,
 ```
npx cypress open
```
Cypress UI will be launched. Click on "E2E Testing" to see all the tests available.
Select to run the tests via electron

**Running Tests**
You can run your Cypress tests in two ways:

Interactive Mode
To run tests in interactive mode, use:
```
npx cypress open
```
Headless Mode
To run tests in headless mode, use:
```
npx cypress run
```