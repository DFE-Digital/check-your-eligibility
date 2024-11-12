Cypress Setup User Guide
Introduction
This guide will walk you through the steps required to install and run Cypress with TypeScript locally.

Prerequisites
Before you begin, ensure that you have the following installed:
•	Node.js (version 12 or higher)
•	npm (Node Package Manager)

Setup Instructions

1. Open Project in Visual Studio Code
Open your project repository in Visual Studio Code and navigate to the test folder.

2. Install Cypress
Install Cypress as a development dependency by running the following command in your terminal:
npm install cypress@13.8.1 --save-dev

4. Configure Environment Variables
Create a cypress.env.json file. (DO NOT COMMIT THIS FILE).
inside populate the following fields 
{
    "JWT_USERNAME": "",
    "JWT_PASSWORD": "",
    "USER_ID": ""
}

3. Open Cypress
Launch Cypress using the following command:
CYPRESS_BASE_URL="{Desired url}" npx cypress open
Cypress UI will be launched. Click on "E2E Testing" to see all the tests available.
Select to run the tests via electron

5. Running Tests
You can run your Cypress tests in two ways:

Interactive Mode
To run tests in interactive mode, use:
npx cypress open

Headless Mode
To run tests in headless mode, use:
npx cypress run
