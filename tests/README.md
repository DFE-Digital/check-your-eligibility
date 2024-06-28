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

3. Open Cypress
Launch Cypress using the following command:
npx cypress open
Cypress UI will be launched. Click on "E2E Testing" to see all the tests available.

4. Configure Environment Variables
In Cypress, we use environment variables to avoid storing sensitive data in the codebase. Declare these variables when you run Cypress using the command below:
CYPRESS_API_HOST="https-path to localhost or remote"
CYPRESS_JWT_USERNAME="JWT user username"
CYPRESS_JWT_PASSWORD="JWT user password"

5. Running Tests
You can run your Cypress tests in two ways:

Interactive Mode
To run tests in interactive mode, use:
npx cypress open

Headless Mode
To run tests in headless mode, use:
npx cypress run
